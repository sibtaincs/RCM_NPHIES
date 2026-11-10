using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NPhies_FHIR_Integration.ApiService.Security;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.ApiService.DataExplorer;

public sealed class ExplorerColumn
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    /// <summary>string, enum, number, decimal, boolean, date, guid.</summary>
    public string Type { get; init; } = "string";
    /// <summary>Large text (JSON / raw bundles) is only returned by the detail endpoint.</summary>
    public bool Large { get; init; }
    public bool IsKey { get; init; }

    internal PropertyInfo Property { get; init; } = null!;
}

public sealed class ExplorerChild
{
    public string Table { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    /// <summary>Column on the child table that points at the parent.</summary>
    public string ForeignKey { get; init; } = string.Empty;
    /// <summary>Column on the parent whose value the foreign key holds.</summary>
    public string PrincipalKey { get; init; } = string.Empty;
}

/// <summary>How rows of a table are tied to a hospital (provider organization).</summary>
public sealed class ExplorerScope
{
    /// <summary>Column holding the provider organization id directly.</summary>
    internal ExplorerColumn? Direct { get; init; }
    /// <summary>Column on this table referencing <see cref="Parent"/>.</summary>
    internal ExplorerColumn? ForeignKey { get; init; }
    internal ExplorerTable? Parent { get; init; }
    internal ExplorerColumn? ParentKey { get; init; }

    public string Describe() => Direct is not null
        ? Direct.Key
        : $"{ForeignKey!.Key} → {Parent!.Name}.{Parent.Scope!.Describe()}";
}

public sealed class ExplorerTable
{
    public string Name { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Permission { get; init; } = string.Empty;
    public string KeyField { get; init; } = string.Empty;
    public List<ExplorerColumn> Columns { get; init; } = new();
    public List<string> DateFields { get; init; } = new();
    public string? DefaultDateField { get; init; }
    public bool HospitalScoped => Scope is not null;
    public string? HospitalPath => Scope?.Describe();
    public List<ExplorerChild> Children { get; } = new();

    internal Type ClrType { get; init; } = null!;
    internal PropertyInfo DbSetProperty { get; init; } = null!;
    internal ExplorerScope? Scope { get; set; }
    internal ExplorerColumn KeyColumn => Columns.First(c => c.IsKey);

    internal ExplorerColumn? Column(string? key) => string.IsNullOrWhiteSpace(key)
        ? null
        : Columns.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Metadata for every table exposed by the generic data explorer, built once from the EF model.
/// Tables holding credentials are excluded and secret-looking columns are never exposed.
/// </summary>
public sealed class DataExplorerRegistry
{
    private static readonly HashSet<string> ExcludedEntities = new(StringComparer.Ordinal) { "User", "RefreshToken" };
    private static readonly Regex SecretColumn = new("(password|salt|secret|privatekey|apikey|pfx|refreshtoken|accesstoken)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LargeColumn = new("(json|raw|bundle|payloads?$|base64|contentdata|filedata|^data$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly string[] DirectScopeColumns = { "ProviderId", "RequestorId", "OrganizationId" };
    private static readonly string[] PreferredDateFields =
    {
        "ServiceDate", "ServicedDate", "SubmittedAt", "SubmissionDate", "CreatedDate", "PaymentDate", "ReceivedAt",
        "SentAt", "DenialDate", "RequestDate", "AttemptedAt", "Timestamp", "CreatedAt"
    };

    private static readonly object Gate = new();
    private static DataExplorerRegistry? _instance;

    public IReadOnlyList<ExplorerTable> Tables { get; }
    private readonly Dictionary<string, ExplorerTable> _byName;

    private DataExplorerRegistry(List<ExplorerTable> tables)
    {
        Tables = tables;
        _byName = tables.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
    }

    public ExplorerTable? Find(string name) => _byName.GetValueOrDefault(name);

    public static DataExplorerRegistry Get(ApplicationDbContext db)
    {
        if (_instance is not null) return _instance;
        lock (Gate)
        {
            return _instance ??= Build(db.Model);
        }
    }

    private static DataExplorerRegistry Build(IModel model)
    {
        var tables = new List<ExplorerTable>();
        var entityTypes = new Dictionary<Type, IEntityType>();

        foreach (var setProp in typeof(ApplicationDbContext).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!setProp.PropertyType.IsGenericType || setProp.PropertyType.GetGenericTypeDefinition() != typeof(DbSet<>)) continue;
            var clr = setProp.PropertyType.GetGenericArguments()[0];
            if (ExcludedEntities.Contains(clr.Name) || entityTypes.ContainsKey(clr)) continue;
            var entityType = model.FindEntityType(clr);
            var pk = entityType?.FindPrimaryKey();
            if (entityType is null || entityType.IsOwned() || pk is null || pk.Properties.Count != 1 || pk.Properties[0].PropertyInfo is null) continue;

            var columns = new List<ExplorerColumn>();
            foreach (var p in entityType.GetProperties())
            {
                if (p.PropertyInfo is null || p.IsShadowProperty()) continue;
                if (SecretColumn.IsMatch(p.Name)) continue;
                var type = ColumnType(p.ClrType);
                if (type is null) continue;
                columns.Add(new ExplorerColumn
                {
                    Key = Camel(p.Name),
                    Label = Humanize(p.Name),
                    Type = type,
                    IsKey = p == pk.Properties[0],
                    Large = type == "string" && LargeColumn.IsMatch(p.Name) && p.GetMaxLength() is null or > 1000,
                    Property = p.PropertyInfo
                });
            }
            if (!columns.Any(c => c.IsKey)) continue;

            var dateFields = columns.Where(c => c.Type == "date").Select(c => c.Key).ToList();
            var defaultDate = PreferredDateFields.Select(Camel).FirstOrDefault(d => dateFields.Contains(d)) ?? dateFields.FirstOrDefault();
            var (category, permission) = Categorize(clr.Name);

            tables.Add(new ExplorerTable
            {
                Name = setProp.Name,
                Label = Humanize(setProp.Name),
                Category = category,
                Permission = permission,
                KeyField = Camel(pk.Properties[0].Name),
                Columns = columns,
                DateFields = dateFields,
                DefaultDateField = defaultDate,
                ClrType = clr,
                DbSetProperty = setProp
            });
            entityTypes[clr] = entityType;
        }

        var byClr = tables.ToDictionary(t => t.ClrType);

        // Relations: EF foreign keys first, then the {Entity}Id naming convention.
        var relations = new List<(ExplorerTable Child, ExplorerColumn Fk, ExplorerTable Parent, ExplorerColumn PrincipalKey)>();
        foreach (var child in tables)
        {
            var et = entityTypes[child.ClrType];
            foreach (var fk in et.GetForeignKeys())
            {
                if (fk.Properties.Count != 1 || fk.PrincipalKey.Properties.Count != 1) continue;
                if (!byClr.TryGetValue(fk.PrincipalEntityType.ClrType, out var parent) || parent == child) continue;
                var fkCol = child.Column(fk.Properties[0].Name);
                var pkCol = parent.Column(fk.PrincipalKey.Properties[0].Name);
                if (fkCol is null || pkCol is null) continue;
                relations.Add((child, fkCol, parent, pkCol));
            }

            foreach (var col in child.Columns.Where(c => c.Key.EndsWith("Id", StringComparison.Ordinal) && !c.IsKey))
            {
                if (relations.Any(r => r.Child == child && r.Fk == col)) continue;
                var parentName = col.Property.Name[..^2];
                var parent = tables.FirstOrDefault(t => t != child && t.ClrType.Name == parentName);
                if (parent is null) continue;
                var pkCol = parent.KeyColumn;
                if (pkCol.Property.PropertyType != col.Property.PropertyType) continue;
                relations.Add((child, col, parent, pkCol));
            }
        }

        foreach (var r in relations)
        {
            if (r.Parent.Children.Any(c => c.Table == r.Child.Name && c.ForeignKey == r.Fk.Key)) continue;
            r.Parent.Children.Add(new ExplorerChild
            {
                Table = r.Child.Name,
                Label = r.Child.Label,
                ForeignKey = r.Fk.Key,
                PrincipalKey = r.PrincipalKey.Key
            });
        }

        // Hospital scope: a direct provider column, else inherited from a scoped parent (resolved iteratively).
        foreach (var t in tables)
        {
            if (t.ClrType.Name == "Organization") continue;
            var direct = DirectScopeColumns.Select(t.Column).FirstOrDefault(c => c is not null && c.Type == "string");
            if (direct is not null) t.Scope = new ExplorerScope { Direct = direct };
        }

        for (var pass = 0; pass < 4; pass++)
        {
            foreach (var t in tables.Where(t => t.Scope is null && t.ClrType.Name != "Organization"))
            {
                var r = relations.FirstOrDefault(r => r.Child == t && r.Parent.Scope is not null && r.Parent != t
                                                      && r.Fk.Property.PropertyType == r.PrincipalKey.Property.PropertyType);
                if (r.Child is null) continue;
                t.Scope = new ExplorerScope { ForeignKey = r.Fk, Parent = r.Parent, ParentKey = r.PrincipalKey };
            }
        }

        return new DataExplorerRegistry(tables.OrderBy(t => t.Category).ThenBy(t => t.Label).ToList());
    }

    private static (string Category, string Permission) Categorize(string entity)
    {
        if (entity.StartsWith("PreAuthorization")) return ("Prior Authorization", Permissions.PreAuthRead);
        if (entity.StartsWith("CoverageEligibility") || entity.StartsWith("Eligibility") || entity is "BenefitBalance" or "Benefit")
            return ("Eligibility", Permissions.EligibilityRead);
        if (entity.StartsWith("PaymentReconciliation") || entity == "PaymentNotice") return ("Payments", Permissions.PaymentsRead);
        if (entity.StartsWith("Appeal") || entity is "ClaimDenial" or "ClaimError") return ("Denials & Appeals", Permissions.ClaimsRead);
        if (entity.StartsWith("Communication")) return ("Communications", Permissions.ClaimsRead);
        if (entity.StartsWith("ClaimResponse")) return ("Claim Responses", Permissions.ClaimsRead);
        if ((entity.StartsWith("Claim") && entity != "ClaimSubmissionRules") || entity is "VisionPrescription" or "OralDetail" || entity.StartsWith("Cancellation"))
            return ("Claims", Permissions.ClaimsRead);
        if (entity is "Patient" or "Coverage" or "Organization" or "Location" or "Practitioner" or "Encounter")
            return ("Patients & Organizations", Permissions.PatientsRead);
        if (entity.EndsWith("Master") || entity is "ClaimSubmissionRules" or "PolicyBenefitCoverage" or "NphiesCodeMapping" or "DoctorQualification")
            return ("Master Data", Permissions.MasterDataRead);
        if (entity.EndsWith("Entity")) return ("Terminology & Validation", Permissions.MasterDataRead);
        return ("Messaging & Audit", Permissions.AuditRead);
    }

    private static string? ColumnType(Type t)
    {
        var u = Nullable.GetUnderlyingType(t) ?? t;
        if (u == typeof(string)) return "string";
        if (u == typeof(bool)) return "boolean";
        if (u == typeof(DateTime)) return "date";
        if (u == typeof(decimal) || u == typeof(double) || u == typeof(float)) return "decimal";
        if (u == typeof(int) || u == typeof(long) || u == typeof(short) || u == typeof(byte)) return "number";
        if (u == typeof(Guid)) return "guid";
        if (u.IsEnum) return "enum";
        return null;
    }

    internal static string Camel(string name) => string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];

    internal static string Humanize(string name)
    {
        var s = Regex.Replace(name, "(Entity|Entities)$", "");
        s = Regex.Replace(s, "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
        return s.Trim();
    }
}
