using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.ApiService.DataExplorer;

public sealed class ExplorerQuery
{
    /// <summary>Provider organization id; ignored for tables that are not hospital scoped.</summary>
    public string? HospitalId { get; set; }
    public DateTime? From { get; set; }
    /// <summary>Inclusive (the whole day).</summary>
    public DateTime? To { get; set; }
    public string? DateField { get; set; }
    public string? Search { get; set; }
    /// <summary>Exact-match column filters (query string <c>f.{column}=value</c>).</summary>
    public Dictionary<string, string> Filters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed class ExplorerPage
{
    public List<Dictionary<string, object?>> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public sealed class ExplorerChildCount
{
    public string Table { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string ForeignKey { get; set; } = string.Empty;
    public string PrincipalKey { get; set; } = string.Empty;
    public object? Value { get; set; }
    public int Count { get; set; }
}

public sealed class ExplorerRecord
{
    public Dictionary<string, object?> Row { get; set; } = new();
    public List<ExplorerChildCount> Children { get; set; } = new();
}

/// <summary>
/// Read-only, metadata-driven query engine over every <see cref="ApplicationDbContext"/> table:
/// hospital scoping (direct or through parent tables), date range, search, column filters, sorting, paging.
/// </summary>
public sealed class DataExplorerService
{
    public const int MaxExportRows = 50_000;
    private const int MaxSearchColumns = 15;

    private static readonly MethodInfo QueryCoreMethod = typeof(DataExplorerService).GetMethod(nameof(QueryCore), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo CountCoreMethod = typeof(DataExplorerService).GetMethod(nameof(CountCore), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo GetCoreMethod = typeof(DataExplorerService).GetMethod(nameof(GetCore), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo StringContains = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;

    private readonly ApplicationDbContext _db;

    public DataExplorerService(ApplicationDbContext db) => _db = db;

    public DataExplorerRegistry Registry => DataExplorerRegistry.Get(_db);

    public Task<ExplorerPage> QueryAsync(ExplorerTable table, ExplorerQuery query, int? maxRows, CancellationToken ct)
        => (Task<ExplorerPage>)QueryCoreMethod.MakeGenericMethod(table.ClrType).Invoke(this, new object?[] { table, query, maxRows, ct })!;

    public Task<int> CountAsync(ExplorerTable table, ExplorerQuery query, CancellationToken ct)
        => (Task<int>)CountCoreMethod.MakeGenericMethod(table.ClrType).Invoke(this, new object?[] { table, query, ct })!;

    public async Task<ExplorerRecord?> GetAsync(ExplorerTable table, string id, string? hospitalId, CancellationToken ct)
    {
        var row = await (Task<Dictionary<string, object?>?>)GetCoreMethod.MakeGenericMethod(table.ClrType)
            .Invoke(this, new object?[] { table, id, hospitalId, ct })!;
        if (row is null) return null;

        var record = new ExplorerRecord { Row = row };
        foreach (var child in table.Children)
        {
            var childTable = Registry.Find(child.Table);
            if (childTable is null || !row.TryGetValue(child.PrincipalKey, out var value) || value is null) continue;
            var count = await CountAsync(childTable, new ExplorerQuery
            {
                HospitalId = hospitalId,
                Filters = new(StringComparer.OrdinalIgnoreCase) { [child.ForeignKey] = Convert.ToString(value, CultureInfo.InvariantCulture)! }
            }, ct);
            record.Children.Add(new ExplorerChildCount
            {
                Table = child.Table,
                Label = child.Label,
                ForeignKey = child.ForeignKey,
                PrincipalKey = child.PrincipalKey,
                Value = value,
                Count = count
            });
        }
        return record;
    }

    public static string ToCsv(ExplorerTable table, IEnumerable<Dictionary<string, object?>> rows)
    {
        var columns = table.Columns.Where(c => !c.Large).ToList();
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', columns.Select(c => CsvCell(c.Label))));
        foreach (var row in rows)
            sb.AppendLine(string.Join(',', columns.Select(c => CsvCell(row.GetValueOrDefault(c.Key)))));
        return sb.ToString();
    }

    private async Task<ExplorerPage> QueryCore<T>(ExplorerTable table, ExplorerQuery query, int? maxRows, CancellationToken ct) where T : class
    {
        var filtered = Filter<T>(table, query);
        var size = maxRows ?? Math.Clamp(query.PageSize, 1, 500);
        var page = maxRows.HasValue ? 1 : Math.Max(1, query.Page);
        var total = await filtered.CountAsync(ct);
        var rows = await Order(filtered, table, query).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new ExplorerPage
        {
            Items = rows.Select(r => ToRow(table, r, includeLarge: false)).ToList(),
            Total = total,
            Page = page,
            PageSize = size
        };
    }

    private Task<int> CountCore<T>(ExplorerTable table, ExplorerQuery query, CancellationToken ct) where T : class
        => Filter<T>(table, query).CountAsync(ct);

    private async Task<Dictionary<string, object?>?> GetCore<T>(ExplorerTable table, string id, string? hospitalId, CancellationToken ct) where T : class
    {
        var query = new ExplorerQuery
        {
            HospitalId = hospitalId,
            Filters = new(StringComparer.OrdinalIgnoreCase) { [table.KeyField] = id }
        };
        var entity = await Filter<T>(table, query).FirstOrDefaultAsync(ct);
        return entity is null ? null : ToRow(table, entity, includeLarge: true);
    }

    private IQueryable<T> Filter<T>(ExplorerTable table, ExplorerQuery query) where T : class
    {
        var source = ((IQueryable<T>)table.DbSetProperty.GetValue(_db)!).AsNoTracking();
        var p = Expression.Parameter(typeof(T), "e");
        var predicates = new List<Expression>();

        if (!string.IsNullOrWhiteSpace(query.HospitalId) && table.Scope is not null)
            predicates.Add(ScopePredicate(table.Scope, p, query.HospitalId!));

        var dateColumn = table.Column(query.DateField) ?? table.Column(table.DefaultDateField);
        if (dateColumn is { Type: "date" } && (query.From.HasValue || query.To.HasValue))
        {
            var member = Expression.Property(p, dateColumn.Property);
            if (query.From.HasValue)
                predicates.Add(Expression.GreaterThanOrEqual(member, Value(query.From.Value.Date, member.Type)));
            if (query.To.HasValue)
                predicates.Add(Expression.LessThan(member, Value(query.To.Value.Date.AddDays(1), member.Type)));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            Expression? any = null;
            foreach (var col in table.Columns.Where(c => c.Type == "string" && !c.Large).Take(MaxSearchColumns))
            {
                var member = Expression.Property(p, col.Property);
                var match = Expression.AndAlso(
                    Expression.NotEqual(member, Expression.Constant(null, typeof(string))),
                    Expression.Call(member, StringContains, Value(term, typeof(string))));
                any = any is null ? match : Expression.OrElse(any, match);
            }
            if (any is not null) predicates.Add(any);
        }

        foreach (var (key, raw) in query.Filters)
        {
            var col = table.Column(key) ?? throw new ArgumentException($"Unknown column '{key}' on table '{table.Name}'.");
            var member = Expression.Property(p, col.Property);
            predicates.Add(Expression.Equal(member, Value(ConvertValue(raw, col.Property.PropertyType, col.Key), member.Type)));
        }

        if (predicates.Count == 0) return source;
        var body = predicates.Aggregate(Expression.AndAlso);
        return source.Where(Expression.Lambda<Func<T, bool>>(body, p));
    }

    /// <summary>Builds <c>e.ProviderId == id</c> or <c>parentKeysOfHospital.Contains(e.ParentFk)</c>.</summary>
    private Expression ScopePredicate(ExplorerScope scope, ParameterExpression p, string hospitalId)
    {
        if (scope.Direct is not null)
            return Expression.Equal(Expression.Property(p, scope.Direct.Property), Value(hospitalId, typeof(string)));

        var parent = scope.Parent!;
        var keyType = scope.ParentKey!.Property.PropertyType;
        var parentSet = (IQueryable)parent.DbSetProperty.GetValue(_db)!;
        var pp = Expression.Parameter(parent.ClrType, "p");
        var where = Expression.Call(typeof(Queryable), nameof(Queryable.Where), new[] { parent.ClrType },
            parentSet.Expression, Expression.Quote(Expression.Lambda(ScopePredicate(parent.Scope!, pp, hospitalId), pp)));
        var select = Expression.Call(typeof(Queryable), nameof(Queryable.Select), new[] { parent.ClrType, keyType },
            where, Expression.Quote(Expression.Lambda(Expression.Property(pp, scope.ParentKey.Property), pp)));
        return Expression.Call(typeof(Queryable), nameof(Queryable.Contains), new[] { keyType },
            select, Expression.Property(p, scope.ForeignKey!.Property));
    }

    private static IQueryable<T> Order<T>(IQueryable<T> source, ExplorerTable table, ExplorerQuery query)
    {
        var key = table.KeyColumn;
        var sort = table.Column(query.SortBy) ?? table.Column(query.DateField) ?? table.Column(table.DefaultDateField) ?? key;
        var desc = query.SortBy is null || query.SortDesc;
        var ordered = Apply(source.Expression, sort, desc ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy));
        if (sort != key) ordered = Apply(ordered, key, nameof(Queryable.ThenBy));
        return source.Provider.CreateQuery<T>(ordered);

        static Expression Apply(Expression src, ExplorerColumn col, string method)
        {
            var p = Expression.Parameter(typeof(T), "o");
            var lambda = Expression.Lambda(Expression.Property(p, col.Property), p);
            return Expression.Call(typeof(Queryable), method, new[] { typeof(T), col.Property.PropertyType }, src, Expression.Quote(lambda));
        }
    }

    /// <summary>Parameterized constant (the funcletizer turns the closure access into a SQL parameter).</summary>
    private static Expression Value(object? value, Type type)
        => Expression.Convert(Expression.Property(Expression.Constant(new Box(value)), nameof(Box.Value)), type);

    private static object? ConvertValue(string raw, Type type, string column)
    {
        var u = Nullable.GetUnderlyingType(type) ?? type;
        try
        {
            if (u == typeof(string)) return raw;
            if (u == typeof(Guid)) return Guid.Parse(raw);
            if (u == typeof(bool)) return bool.Parse(raw);
            if (u == typeof(DateTime)) return DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            if (u.IsEnum) return Enum.Parse(u, raw, ignoreCase: true);
            return Convert.ChangeType(raw, u, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidCastException or OverflowException)
        {
            throw new ArgumentException($"Value '{raw}' is not valid for column '{column}'.");
        }
    }

    private static Dictionary<string, object?> ToRow(ExplorerTable table, object entity, bool includeLarge)
    {
        var row = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var col in table.Columns)
        {
            if (col.Large && !includeLarge) continue;
            var value = col.Property.GetValue(entity);
            row[col.Key] = value is Enum e ? e.ToString() : value;
        }
        return row;
    }

    private static string CsvCell(object? value)
    {
        var s = value switch
        {
            null => string.Empty,
            DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
        // Neutralise spreadsheet formula injection.
        if (s.Length > 0 && s[0] is '=' or '+' or '-' or '@' && value is string) s = "'" + s;
        return s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
    }

    private sealed class Box
    {
        public Box(object? value) => Value = value;
        public object? Value { get; }
    }
}
