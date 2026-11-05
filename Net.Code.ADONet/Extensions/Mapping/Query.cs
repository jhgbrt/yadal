using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
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
    // element-wise equality, so that records containing a ValueList (DbKey) have value semantics
    public override bool Equals(object? obj)
        => obj is IEnumerable other && this.Cast<object?>().SequenceEqual(other.Cast<object?>());
    public override int GetHashCode()
    {
        int result = 19;
        foreach (var v in this)
            result = HashCode.Combine(result, v?.GetHashCode());
        return result;
    }
}
[RequiresUnreferencedCode(Trimming.ReflectionMessage)]
[RequiresDynamicCode(Trimming.DynamicCodeMessage)]
internal static class QueryFactory<T>
{
    struct MyPropertyInfo(PropertyInfo info)
    {
        public Type Type { get; } = info.PropertyType;
        public CustomAttributeData[] CustomAttributes { get; } = info.CustomAttributes.ToArray();
        public string Name { get; } = info.Name;
        public bool HasCustomAttribute<TAttribute>(Func<TAttribute, bool> predicate) => info.HasCustomAttribute<TAttribute>(predicate);
        public string GetColumnName(MappingConvention convention) => info.GetColumnName(convention);
        public object? GetValue(object obj) => info.GetValue(obj);
        public override bool Equals(object? obj) => obj is MyPropertyInfo other && ((other.Name, other.Type)).Equals((Name, Type));
        public override int GetHashCode() => (Name, Type).GetHashCode();
    }

    static readonly MyPropertyInfo[] Properties = typeof(T).GetProperties().Where(TypeExtensions.IsReadable).Select(p => new MyPropertyInfo(p)).ToArray();
    static readonly MyPropertyInfo[] KeyProperties = GetKeyProperties();
    static IEnumerable<MyPropertyInfo> DbGenerated => KeyProperties.Where(p => p.HasCustomAttribute<DatabaseGeneratedAttribute>(a => a.DatabaseGeneratedOption != DatabaseGeneratedOption.None));
    static readonly IEnumerable<MyPropertyInfo> NonKeyProperties = Properties.Except(KeyProperties);
    static readonly IEnumerable<MyPropertyInfo> InsertProperties = Properties.Except(DbGenerated);

    // the parameters each statement uses: binding parameters that are not used in the statement
    // (e.g. a database-generated key on INSERT) is rejected by some providers (Oracle with BindByName)
    static readonly string[] InsertParameterNames = InsertProperties.Select(p => p.Name).ToArray();
    static readonly string[] UpdateParameterNames = NonKeyProperties.Concat(KeyProperties).Select(p => p.Name).ToArray();
    static readonly string[] DeleteParameterNames = KeyProperties.Select(p => p.Name).ToArray();

    internal static IEnumerable<IEnumerable<(string name, object? value)>> InsertParameters(IEnumerable<T> items) => ParameterSets(items, InsertParameterNames);
    internal static IEnumerable<IEnumerable<(string name, object? value)>> UpdateParameters(IEnumerable<T> items) => ParameterSets(items, UpdateParameterNames);
    internal static IEnumerable<IEnumerable<(string name, object? value)>> DeleteParameters(IEnumerable<T> items) => ParameterSets(items, DeleteParameterNames);

    private static IEnumerable<IEnumerable<(string name, object? value)>> ParameterSets(IEnumerable<T> items, string[] names)
    {
        var getters = FastReflection<T>.Instance.GetGettersForType();
        return items.Select(item => names.Select(name => (name, getters[name](item))));
    }

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
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        var keyType = key.GetType();
        var valueProperties = keyType.GetProperties().Where(TypeExtensions.IsReadable).Select(p => new MyPropertyInfo(p)).ToArray();

        return KeyProperties switch
        {
            _ when KeyProperties.OrderBy(x => x.Name).SequenceEqual(valueProperties.OrderBy(x => x.Name))
                => new(
                    (from k in KeyProperties
                     join p in valueProperties on k.Name equals p.Name
                     select (k.Name, p.GetValue(key))
                     )),
            [{ Type: Type t, Name: string propertyName }] when IsAssignableToKey(t, keyType)
                => new([(propertyName, key)]),
            _ => throw new InvalidOperationException(
                $"Incompatible key value for {typeof(T).Name}. " +
                $"Expected: {string.Join(",", KeyProperties.Select(k => $"{k.Name} [{TypeName(k.Type)}]"))} " +
                (KeyProperties.Length == 1 ? "(the key value itself, or an object with that property) " : "(an object with exactly these properties) ") +
                $"but was: {TypeName(keyType)}" +
                (valueProperties.Length > 0 ? $" with properties {string.Join(",", valueProperties.Select(k => $"{k.Name} [{TypeName(k.Type)}]"))}" : ""))
        };
    }

    private static bool IsAssignableToKey(Type keyPropertyType, Type valueType)
        => (Nullable.GetUnderlyingType(keyPropertyType) ?? keyPropertyType).IsAssignableFrom(valueType);

    private static string TypeName(Type type)
        => Nullable.GetUnderlyingType(type) is Type underlying ? $"{underlying.Name}?" : type.Name;

    internal static string SELECTALL(MappingConvention convention) => Get(convention).SelectAll;
    internal static string SELECTONE(MappingConvention convention) => Get(convention).Select;
    internal static string INSERT(MappingConvention convention) => Get(convention).Insert;
    internal static string UPDATE(MappingConvention convention) => Get(convention).Update;
    internal static string DELETE(MappingConvention convention) => Get(convention).Delete;
    internal static string COUNT(MappingConvention convention) => Get(convention).Count;

    static readonly ConcurrentDictionary<MappingConvention, Query> cache = new();

    internal static Query Get(MappingConvention convention) => cache.GetOrAdd(convention, Create);
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
