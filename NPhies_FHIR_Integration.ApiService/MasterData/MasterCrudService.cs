using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NPhies_FHIR_Integration.ApiService.DataExplorer;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.ApiService.MasterData;

public sealed class MasterField
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    /// <summary>string | number | decimal | boolean | date | enum | guid</summary>
    public string Type { get; init; } = "string";
    public bool Required { get; init; }
    public bool ReadOnly { get; init; }
    public bool Large { get; init; }
    public int? MaxLength { get; init; }
    public string[]? Options { get; init; }
    /// <summary>Master table whose id this column stores (render as a searchable select).</summary>
    public string? Lookup { get; init; }
    /// <summary>Extra lookup filter as "column=value" (e.g. organizationType=ins).</summary>
    public string? LookupFilter { get; init; }
    public bool Summary { get; init; }
}

public sealed class MasterTableInfo
{
    public string Table { get; init; } = string.Empty;
    public string Group { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Feeds { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
    public string KeyField { get; init; } = string.Empty;
    public string? HospitalField { get; init; }
    public string? DefaultDateField { get; init; }
    public List<string> DateFields { get; init; } = new();
    public string[] Display { get; init; } = Array.Empty<string>();
    public List<string[]> UniqueKeys { get; init; } = new();
    public List<MasterField> Fields { get; init; } = new();
    public List<ExplorerChild> Children { get; init; } = new();
    public int? Count { get; set; }
    public bool CanCreate { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
}

public sealed class MasterLookupItem
{
    public string Value { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
}

public sealed class MasterDeleteResult
{
    public bool Deleted { get; init; }
    public bool Deactivated { get; init; }
    public List<string> ReferencedBy { get; init; } = new();
}

public sealed class MasterValidationException : Exception
{
    public MasterValidationException(Dictionary<string, string> errors)
        : base(string.Join("; ", errors.Select(e => $"{e.Key}: {e.Value}"))) => Errors = errors;

    public Dictionary<string, string> Errors { get; }
}

public sealed class MasterForbiddenException : Exception
{
    public MasterForbiddenException(string message) : base(message) { }
}

/// <summary>
/// Generic create / update / delete for the tables in <see cref="MasterCatalog"/>, driven by the EF model:
/// required and length checks, coded-value checks, foreign-key existence, unique / natural keys, hospital scoping,
/// and soft deactivation of rows that are still referenced.
/// </summary>
public sealed class MasterCrudService
{
    private static readonly HashSet<string> AuditColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "createdAt", "updatedAt", "createdBy", "modifiedBy", "updatedBy", "createdDate", "modifiedDate", "lastModifiedDate", "lastUpdated"
    };
    private static readonly string[] HospitalColumns = { "organizationId", "providerId" };
    private static readonly MethodInfo AnyCoreMethod = typeof(MasterCrudService).GetMethod(nameof(AnyCore), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly ApplicationDbContext _db;
    private readonly DataExplorerService _explorer;

    public MasterCrudService(ApplicationDbContext db)
    {
        _db = db;
        _explorer = new DataExplorerService(db);
    }

    public DataExplorerRegistry Registry => _explorer.Registry;

    public ExplorerTable Table(MasterDefinition def) =>
        Registry.Find(def.Table) ?? throw new InvalidOperationException($"Master table '{def.Table}' is not in the EF model.");

    public MasterTableInfo Describe(MasterDefinition def)
    {
        var table = Table(def);
        var entity = _db.Model.FindEntityType(table.ClrType)!;
        var fields = new List<MasterField>();
        foreach (var col in table.Columns)
        {
            if (AuditColumns.Contains(col.Key)) continue;
            var p = entity.FindProperty(col.Property.Name);
            var lookup = LookupTable(def, table, entity, col);
            var u = Nullable.GetUnderlyingType(col.Property.PropertyType) ?? col.Property.PropertyType;
            fields.Add(new MasterField
            {
                Key = col.Key,
                Label = col.Label,
                Type = col.Type,
                ReadOnly = col.IsKey,
                Large = col.Large,
                MaxLength = p?.GetMaxLength(),
                Required = IsRequired(def, col, p, lookup),
                Options = def.Options.TryGetValue(col.Key, out var opts) ? opts : u.IsEnum ? Enum.GetNames(u) : null,
                Lookup = lookup,
                LookupFilter = lookup == "Organizations" ? OrganizationFilter(col.Key) : null,
                Summary = def.Summary.Contains(col.Key, StringComparer.OrdinalIgnoreCase),
            });
        }
        var ordered = fields.Where(f => f.ReadOnly)
            .Concat(def.Summary.Select(s => fields.FirstOrDefault(f => !f.ReadOnly && string.Equals(f.Key, s, StringComparison.OrdinalIgnoreCase))).Where(f => f is not null)!)
            .Concat(fields.Where(f => !f.ReadOnly && !f.Summary && f.Required))
            .Concat(fields.Where(f => !f.ReadOnly && !f.Summary && !f.Required))
            .Cast<MasterField>().Distinct().ToList();

        return new MasterTableInfo
        {
            Table = table.Name,
            Group = def.Group,
            Label = def.Label,
            Feeds = def.Feeds,
            Purpose = def.Purpose,
            KeyField = table.KeyField,
            HospitalField = HospitalField(table),
            DefaultDateField = table.DefaultDateField,
            DateFields = table.DateFields,
            Display = DisplayFields(def, table),
            UniqueKeys = UniqueKeys(def, table, entity).Select(k => k.Select(c => c.Key).ToArray()).ToList(),
            Fields = ordered,
            Children = table.Children,
        };
    }

    public Task<int> CountAsync(MasterDefinition def, string? hospitalId, CancellationToken ct) =>
        _explorer.CountAsync(Table(def), new ExplorerQuery { HospitalId = hospitalId }, ct);

    public async Task<Dictionary<string, object?>> CreateAsync(MasterDefinition def, Dictionary<string, JsonElement> values, string? forcedHospital, string? user, CancellationToken ct)
    {
        var table = Table(def);
        if (forcedHospital is not null && HospitalField(table) is null && table.ClrType.Name == "Organization")
            throw new MasterForbiddenException("Only administrators can add organizations.");

        var entity = Activator.CreateInstance(table.ClrType)!;
        var errors = Apply(def, table, entity, values, isNew: true);
        if (values.TryGetValue(table.KeyField, out var key) && key.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(key.GetString()))
            table.KeyColumn.Property.SetValue(entity, ConvertValue(key, table.KeyColumn.Property.PropertyType, nullable: false));
        ForceHospital(table, entity, forcedHospital);
        Stamp(entity, user, isNew: true);

        await ValidateAsync(def, table, entity, errors, isNew: true, ct);
        _db.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Row(table, entity);
    }

    public async Task<Dictionary<string, object?>?> UpdateAsync(MasterDefinition def, string id, Dictionary<string, JsonElement> values, string? forcedHospital, string? user, CancellationToken ct)
    {
        var table = Table(def);
        var entity = await FindAsync(table, id, ct);
        if (entity is null) return null;
        EnsureHospital(table, entity, id, forcedHospital);

        var errors = Apply(def, table, entity, values, isNew: false);
        ForceHospital(table, entity, forcedHospital);
        Stamp(entity, user, isNew: false);
        await ValidateAsync(def, table, entity, errors, isNew: false, ct);
        await _db.SaveChangesAsync(ct);
        return Row(table, entity);
    }

    public async Task<MasterDeleteResult?> DeleteAsync(MasterDefinition def, string id, string? forcedHospital, string? user, CancellationToken ct)
    {
        var table = Table(def);
        var entity = await FindAsync(table, id, ct);
        if (entity is null) return null;
        EnsureHospital(table, entity, id, forcedHospital);

        var keyValue = table.KeyColumn.Property.GetValue(entity);
        var referencedBy = new List<string>();
        foreach (var child in table.Children)
        {
            var childTable = Registry.Find(child.Table);
            var fk = childTable?.Column(child.ForeignKey);
            if (childTable is null || fk is null) continue;
            if (await AnyAsync(childTable.ClrType, new[] { (fk.Property, CoerceTo(keyValue, fk.Property.PropertyType)) }, null, null, ct))
                referencedBy.Add(childTable.Label);
        }

        if (referencedBy.Count > 0)
        {
            var active = table.ClrType.GetProperty("IsActive");
            if (active?.PropertyType != typeof(bool))
                throw new MasterValidationException(new() { [table.KeyField] = $"Cannot delete: referenced by {string.Join(", ", referencedBy)}." });
            active.SetValue(entity, false);
            var status = table.ClrType.GetProperty("Status");
            if (status?.PropertyType == typeof(string) && def.Options.TryGetValue("status", out var opts) && opts.Contains("inactive"))
                status.SetValue(entity, "inactive");
            Stamp(entity, user, isNew: false);
            await _db.SaveChangesAsync(ct);
            return new MasterDeleteResult { Deactivated = true, ReferencedBy = referencedBy };
        }

        _db.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return new MasterDeleteResult { Deleted = true };
    }

    public async Task<List<MasterLookupItem>> LookupAsync(ExplorerTable table, MasterDefinition? def, string? search, string? hospitalId,
        Dictionary<string, string> filters, int take, CancellationToken ct)
    {
        var display = DisplayFields(def, table);
        var query = new ExplorerQuery
        {
            Search = search,
            HospitalId = hospitalId,
            Page = 1,
            PageSize = Math.Clamp(take, 1, 200),
            SortBy = display.FirstOrDefault(),
            SortDesc = false,
        };
        foreach (var f in filters) query.Filters[f.Key] = f.Value;
        var page = await _explorer.QueryAsync(table, query, null, ct);
        return page.Items.Select(row =>
        {
            var value = row.GetValueOrDefault(table.KeyField)?.ToString() ?? string.Empty;
            var parts = display.Select(d => row.GetValueOrDefault(d)?.ToString()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            return new MasterLookupItem { Value = value, Label = parts.Count > 0 ? string.Join(" · ", parts) : value };
        }).ToList();
    }

    // ------------------------------------------------------------------ helpers

    public static string? HospitalField(ExplorerTable table) =>
        table.ClrType.Name == "Organization" ? null
            : HospitalColumns.Select(table.Column).FirstOrDefault(c => c is not null && c.Type == "string")?.Key;

    private string? LookupTable(MasterDefinition def, ExplorerTable table, IEntityType entity, ExplorerColumn col)
    {
        if (def.Lookups.TryGetValue(col.Key, out var mapped)) return mapped;
        var fk = entity.GetForeignKeys().FirstOrDefault(f => f.Properties.Count == 1 && f.Properties[0].Name == col.Property.Name);
        if (fk is not null)
            return Registry.Tables.FirstOrDefault(t => t.ClrType == fk.PrincipalEntityType.ClrType)?.Name;
        if (HospitalColumns.Contains(col.Key, StringComparer.OrdinalIgnoreCase) || col.Key is "insurerId" or "payerId" && table.ClrType.Name != "PayerMaster")
            return "Organizations";
        return null;
    }

    /// <summary>
    /// Non-nullable strings default to "" in the domain model, so only identifying, coded, referencing and
    /// [Required] columns (and non-null dates) must be entered.
    /// </summary>
    private static bool IsRequired(MasterDefinition def, ExplorerColumn col, IProperty? p, string? lookup)
    {
        if (col.IsKey || p is null || p.IsNullable) return false;
        var u = Nullable.GetUnderlyingType(col.Property.PropertyType) ?? col.Property.PropertyType;
        if (u == typeof(DateTime)) return true;
        if (u != typeof(string)) return false;
        return lookup is not null
               || def.NaturalKey.Contains(col.Key, StringComparer.OrdinalIgnoreCase)
               || def.Display.Take(1).Contains(col.Key, StringComparer.OrdinalIgnoreCase)
               || def.Options.ContainsKey(col.Key)
               || col.Property.GetCustomAttribute<System.ComponentModel.DataAnnotations.RequiredAttribute>() is not null;
    }

    private static string? OrganizationFilter(string column) =>
        column.Contains("insurer", StringComparison.OrdinalIgnoreCase) || column.Contains("payer", StringComparison.OrdinalIgnoreCase)
            ? "organizationType=ins"
            : HospitalColumns.Contains(column, StringComparer.OrdinalIgnoreCase) ? "organizationType=prov" : null;

    private static string[] DisplayFields(MasterDefinition? def, ExplorerTable table)
    {
        var configured = def?.Display.Where(d => table.Column(d) is not null).ToArray() ?? Array.Empty<string>();
        if (configured.Length > 0) return configured;
        var candidates = table.Columns.Where(c => c.Type == "string" && !c.IsKey && !c.Large).Select(c => c.Key).ToList();
        var code = candidates.FirstOrDefault(c => c.EndsWith("Code", StringComparison.Ordinal) && !c.StartsWith("nphies", StringComparison.Ordinal) && !c.StartsWith("currency", StringComparison.Ordinal));
        var name = candidates.FirstOrDefault(c => c.EndsWith("Name", StringComparison.Ordinal) || c.EndsWith("Description", StringComparison.Ordinal) || c == "display");
        var result = new[] { code, name }.Where(s => s is not null).Cast<string>().ToArray();
        return result.Length > 0 ? result : new[] { table.KeyField };
    }

    private static List<ExplorerColumn[]> UniqueKeys(MasterDefinition def, ExplorerTable table, IEntityType entity)
    {
        var keys = entity.GetIndexes().Where(i => i.IsUnique)
            .Select(i => i.Properties.Select(p => table.Column(p.Name)).ToArray())
            .Where(cols => cols.All(c => c is not null && !c.IsKey))
            .Select(cols => cols.Cast<ExplorerColumn>().ToArray())
            .ToList();
        var natural = def.NaturalKey.Select(table.Column).ToArray();
        if (natural.Length > 0 && natural.All(c => c is not null) &&
            !keys.Any(k => k.Select(c => c.Key).SequenceEqual(natural.Select(c => c!.Key), StringComparer.OrdinalIgnoreCase)))
            keys.Add(natural.Cast<ExplorerColumn>().ToArray());
        return keys;
    }

    private Dictionary<string, string> Apply(MasterDefinition def, ExplorerTable table, object entity, Dictionary<string, JsonElement> values, bool isNew)
    {
        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var model = _db.Model.FindEntityType(table.ClrType)!;
        foreach (var (key, element) in values)
        {
            var col = table.Column(key);
            if (col is null || col.IsKey || AuditColumns.Contains(col.Key) || !col.Property.CanWrite) continue;
            var nullable = model.FindProperty(col.Property.Name)?.IsNullable ?? true;
            try
            {
                col.Property.SetValue(entity, ConvertValue(element, col.Property.PropertyType, nullable));
            }
            catch (Exception ex) when (ex is FormatException or InvalidOperationException or ArgumentException or OverflowException)
            {
                errors[col.Key] = $"Invalid {col.Type} value.";
            }
        }
        return errors;
    }

    private async Task ValidateAsync(MasterDefinition def, ExplorerTable table, object entity, Dictionary<string, string> errors, bool isNew, CancellationToken ct)
    {
        var model = _db.Model.FindEntityType(table.ClrType)!;
        var keyProp = table.KeyColumn.Property;
        var keyValue = keyProp.GetValue(entity);
        var describe = Describe(def);
        var required = describe.Fields.Where(f => f.Required).Select(f => f.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var col in table.Columns.Where(c => !c.IsKey && !AuditColumns.Contains(c.Key)))
        {
            if (errors.ContainsKey(col.Key)) continue;
            var p = model.FindProperty(col.Property.Name);
            var value = col.Property.GetValue(entity);
            var u = Nullable.GetUnderlyingType(col.Property.PropertyType) ?? col.Property.PropertyType;
            if (u == typeof(string) && value is null && p is { IsNullable: false })
                col.Property.SetValue(entity, value = string.Empty);
            if (required.Contains(col.Key) && u == typeof(string) && string.IsNullOrWhiteSpace(value as string))
                errors[col.Key] = "Required.";
            else if (required.Contains(col.Key) && u == typeof(DateTime) && value is DateTime d && d == default)
                errors[col.Key] = "Required.";
            else if (value is string s && p?.GetMaxLength() is int max && s.Length > max)
                errors[col.Key] = $"Maximum {max} characters.";
            else if (value is string code && code.Length > 0 && def.Options.TryGetValue(col.Key, out var options) && !options.Contains(code, StringComparer.Ordinal))
                errors[col.Key] = $"Must be one of: {string.Join(", ", options)}.";
        }

        foreach (var field in describe.Fields.Where(f => f.Lookup is not null && !errors.ContainsKey(f.Key)))
        {
            var col = table.Column(field.Key)!;
            var value = col.Property.GetValue(entity);
            if (value is null || value is string { Length: 0 }) continue;
            var parent = Registry.Find(field.Lookup!);
            if (parent is null) continue;
            if (!await AnyAsync(parent.ClrType, new[] { (parent.KeyColumn.Property, CoerceTo(value, parent.KeyColumn.Property.PropertyType)) }, null, null, ct))
                errors[field.Key] = $"No {parent.Label} with id '{value}'.";
        }

        foreach (var unique in UniqueKeys(def, table, model))
        {
            if (unique.Any(c => errors.ContainsKey(c.Key))) continue;
            var pairs = unique.Select(c => (c.Property, c.Property.GetValue(entity))).ToArray();
            if (pairs.Any(p => p.Item2 is null or string { Length: 0 })) continue;
            if (await AnyAsync(table.ClrType, pairs, keyProp, isNew ? null : keyValue, ct))
                errors[unique[^1].Key] = $"Another {def.Label} row already has this {string.Join(" + ", unique.Select(c => c.Label))}.";
        }

        if (def.Rules is not null)
            foreach (var (field, message) in def.Rules(entity))
                errors.TryAdd(field, message);

        if (errors.Count > 0) throw new MasterValidationException(errors);
    }

    private static void ForceHospital(ExplorerTable table, object entity, string? forcedHospital)
    {
        if (forcedHospital is null || HospitalField(table) is not string field) return;
        table.Column(field)!.Property.SetValue(entity, forcedHospital);
    }

    private static void EnsureHospital(ExplorerTable table, object entity, string id, string? forcedHospital)
    {
        if (forcedHospital is null) return;
        if (table.ClrType.Name == "Organization")
        {
            if (!string.Equals(id, forcedHospital, StringComparison.OrdinalIgnoreCase))
                throw new MasterForbiddenException("You can only edit your own organization.");
            return;
        }
        if (HospitalField(table) is string field &&
            !string.Equals(table.Column(field)!.Property.GetValue(entity) as string, forcedHospital, StringComparison.OrdinalIgnoreCase))
            throw new MasterForbiddenException("This row belongs to another hospital.");
    }

    private static void Stamp(object entity, string? user, bool isNew)
    {
        var type = entity.GetType();
        var now = DateTime.UtcNow;
        if (isNew)
        {
            Set(type.GetProperty("CreatedAt"), now);
            Set(type.GetProperty("CreatedBy"), user);
        }
        else
        {
            Set(type.GetProperty("UpdatedAt"), now);
            Set(type.GetProperty("ModifiedBy"), user);
            Set(type.GetProperty("UpdatedBy"), user);
        }

        void Set(PropertyInfo? p, object? value)
        {
            if (p is null || !p.CanWrite || value is null) return;
            var u = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            if (u == value.GetType()) p.SetValue(entity, value);
        }
    }

    private async Task<object?> FindAsync(ExplorerTable table, string id, CancellationToken ct)
    {
        object key;
        try
        {
            key = CoerceTo(id, table.KeyColumn.Property.PropertyType)!;
        }
        catch (FormatException)
        {
            return null;
        }
        return await _db.FindAsync(table.ClrType, new[] { key }, ct);
    }

    private static Dictionary<string, object?> Row(ExplorerTable table, object entity) =>
        table.Columns.ToDictionary(c => c.Key, c => c.Property.GetValue(entity));

    private Task<bool> AnyAsync(Type clr, IReadOnlyList<(PropertyInfo Property, object? Value)> equals, PropertyInfo? keyProp, object? excludeKey, CancellationToken ct) =>
        (Task<bool>)AnyCoreMethod.MakeGenericMethod(clr).Invoke(this, new object?[] { equals, keyProp, excludeKey, ct })!;

    private Task<bool> AnyCore<T>(IReadOnlyList<(PropertyInfo Property, object? Value)> equals, PropertyInfo? keyProp, object? excludeKey, CancellationToken ct) where T : class
    {
        var e = Expression.Parameter(typeof(T), "e");
        Expression body = Expression.Constant(true);
        foreach (var (property, value) in equals)
            body = Expression.AndAlso(body, Expression.Equal(Expression.Property(e, property), Expression.Constant(value, property.PropertyType)));
        if (keyProp is not null && excludeKey is not null)
            body = Expression.AndAlso(body, Expression.NotEqual(Expression.Property(e, keyProp), Expression.Constant(excludeKey, keyProp.PropertyType)));
        return _db.Set<T>().AnyAsync(Expression.Lambda<Func<T, bool>>(body, e), ct);
    }

    private static object? CoerceTo(object? value, Type type)
    {
        if (value is null) return null;
        var u = Nullable.GetUnderlyingType(type) ?? type;
        if (u.IsInstanceOfType(value)) return value;
        if (u == typeof(Guid)) return Guid.Parse(value.ToString()!);
        return System.Convert.ChangeType(value, u, CultureInfo.InvariantCulture);
    }

    internal static object? ConvertValue(JsonElement element, Type type, bool nullable)
    {
        var u = Nullable.GetUnderlyingType(type) ?? type;
        var canBeNull = nullable && (!type.IsValueType || Nullable.GetUnderlyingType(type) is not null);
        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return canBeNull ? null : u == typeof(string) ? string.Empty : Activator.CreateInstance(u);

        var text = element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText();
        if (string.IsNullOrWhiteSpace(text) && u != typeof(string))
            return canBeNull ? null : Activator.CreateInstance(u);

        if (u == typeof(string)) return string.IsNullOrEmpty(text) && canBeNull ? null : text?.Trim();
        if (u == typeof(bool)) return element.ValueKind is JsonValueKind.True or JsonValueKind.False ? element.GetBoolean() : bool.Parse(text!);
        if (u == typeof(DateTime))
            return DateTime.Parse(text!, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        if (u == typeof(Guid)) return Guid.Parse(text!);
        if (u.IsEnum) return int.TryParse(text, out var n) ? Enum.ToObject(u, n) : Enum.Parse(u, text!, ignoreCase: true);
        return System.Convert.ChangeType(text, u, CultureInfo.InvariantCulture);
    }
}
