using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Net.Code.ADONet;

public record Query(string Insert, string Update, string Delete, string Select, string SelectAll, string Count);
internal record DbKey(ValueList<(string name, object value)> values)
{
    public DbKey(IEnumerable<(string name, object value)> values) : this([.. values]) { }
}
internal class ValueList<T> : List<T>
{
    public ValueList() { }
    public ValueList(IEnumerable<T> values)
    {
        base.AddRange(values);
    }
    public override bool Equals(object obj)
    {
        dynamic d = obj;
        return IsSequenceEqual(d);
    }
    private bool IsSequenceEqual(IEnumerable<T> other) => this.SequenceEqual(other);
    private bool IsSequenceEqual<TOther>(IEnumerable<TOther> other)
        => this.Zip(other, (a, b) => (a, b)).All(p =>
        (p is (null, null)) || (p.a?.Equals(p.b) ?? false)
        );
    private bool IsSequenceEqual(object other) => false;
    public override int GetHashCode()
    {
        int result = 19;
        foreach (var v in this)
            result = HashCode.Combine(result, v?.GetHashCode());
        return result;
    }
}
internal static class QueryFactory<T> 
{
    struct MyPropertyInfo(PropertyInfo info)
    {
        public Type Type { get; } = info.PropertyType;
        public CustomAttributeData[] CustomAttributes { get; } = info.CustomAttributes.ToArray();
        public string Name { get; } = info.Name;
        public bool HasCustomAttribute<TAttribute>(Func<TAttribute, bool> predicate) => info.HasCustomAttribute<TAttribute>(predicate);
        public string GetColumnName(MappingConvention convention) => info.GetColumnName(convention);
        public object GetValue(object obj) => info.GetValue(obj);
        public override bool Equals(object obj) => obj is MyPropertyInfo other && ((other.Name, other.Type)).Equals((Name, Type));
        public override int GetHashCode() => (Name, Type).GetHashCode();
    }

    static readonly MyPropertyInfo[] Properties = typeof(T).GetProperties().Select(p => new MyPropertyInfo(p)).ToArray();
    static readonly MyPropertyInfo[] KeyProperties = GetKeyProperties();
    static IEnumerable<MyPropertyInfo> DbGenerated => KeyProperties.Where(p => p.HasCustomAttribute<DatabaseGeneratedAttribute>(a => a.DatabaseGeneratedOption != DatabaseGeneratedOption.None));
    static readonly IEnumerable<MyPropertyInfo> NonKeyProperties = Properties.Except(KeyProperties);
    static readonly IEnumerable<MyPropertyInfo> InsertProperties = Properties.Except(DbGenerated);

    static MyPropertyInfo[] GetKeyProperties()
    {
        var keyProperties = Properties.Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(KeyAttribute)));
        if (!keyProperties.Any())
            keyProperties = Properties.Where(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
        if (!keyProperties.Any())
            keyProperties = Properties.Where(p => p.Name.Equals($"{typeof(T).Name}Id", StringComparison.OrdinalIgnoreCase)).ToArray();
        return keyProperties.ToArray();
    }

    public static DbKey ToKey(object key)
    {
        var valueProperties = key.GetType().GetProperties().Select(p => new MyPropertyInfo(p));

        return KeyProperties switch
        {
            [{ Type: { IsPrimitive: true } t, Name: string propertyName }] when key.GetType() == t
                => new([(propertyName, key)]),
            _ when KeyProperties.OrderBy(x => x.Name).SequenceEqual(valueProperties.OrderBy(x => x.Name))
                => new(
                    (from k in KeyProperties
                     join p in valueProperties on k.Name equals p.Name
                     select (k.Name, p.GetValue(key))
                     )),
            _ => throw new InvalidOperationException($"Incompatible key value. Expected: {string.Join(",", KeyProperties.Select(k => $"{k.Name} [{k.Type.Name}]"))} but was: {string.Join(",", key.GetType().GetProperties().Select(k => $"{k.Name} [{k.PropertyType.Name}]"))}")
        };
    }

    internal static string SELECTALL(MappingConvention convention) => Get(convention).SelectAll;
    internal static string SELECTONE(MappingConvention convention) => Get(convention).Select;
    internal static string INSERT(MappingConvention convention) => Get(convention).Insert;
    internal static string UPDATE(MappingConvention convention) => Get(convention).Update;
    internal static string DELETE(MappingConvention convention) => Get(convention).Delete;
    internal static string COUNT(MappingConvention convention) => Get(convention).Count;

    static Dictionary<MappingConvention, Query> cache = [];

    internal static Query Get(MappingConvention convention)
    {
        if (!cache.TryGetValue(convention, out var query)) 
        { 
            query = Create(convention);
            cache[convention] = query;
        }
        return query;
    }
    private static Query Create(MappingConvention convention)
    {
        var insertColumns = string.Join(", ", InsertProperties.Select(p => p.GetColumnName(convention)));
        var insertValues = string.Join(", ", InsertProperties.Select(p => $"{convention.Parameter(p.Name)}"));
        var whereClause = string.Join(" AND ", KeyProperties.Select(p => $"{p.GetColumnName(convention)} = {convention.Parameter(p.Name)}"));
        var updateColumns = string.Join(", ", NonKeyProperties.Select(p => $"{p.GetColumnName(convention)} = {convention.Parameter(p.Name)}"));
        var allColumns = string.Join(", ", Properties.Select(p => p.GetColumnName(convention)));
        var tableName = typeof(T).GetTableName(convention);

        return new Query(
            $"INSERT INTO {tableName} ({insertColumns}) VALUES ({insertValues})", 
            $"UPDATE {tableName} SET {updateColumns} WHERE {whereClause}", 
            $"DELETE FROM {tableName} WHERE {whereClause}", 
            $"SELECT {allColumns} FROM {tableName} WHERE {whereClause}", 
            $"SELECT {allColumns} FROM {tableName}", 
            $"SELECT COUNT(*) FROM {tableName}");
    }

}
