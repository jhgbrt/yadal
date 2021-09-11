namespace Net.Code.ADONet;

internal static class DataRecordExtensions
{
    private record struct Setter<T>(int FieldIndex, Action<T, object?> SetValue);
    private static List<Setter<T>> GetSetters<T>(this IDataReader reader, DbConfig config)
    {
        var convention = config.MappingConvention;
        var setters = FastReflection<T>.Instance.GetSettersForType();
        var list = new List<Setter<T>>(reader.FieldCount);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var columnName = convention.FromDb(reader.GetName(i));
            if (setters.TryGetValue(columnName, out var setter))
            {
                list.Add(new(i, setter));
            }
        }
        return list;
    }

    internal static Func<IDataRecord, T> GetMapper<T>(this IDataReader reader, DbConfig config)
    {
        var type = typeof(T);
        var properties = type.GetProperties();

        // convention: if there is only a constructor with parameters for all properties
        // assume basic 'record-like' class
        var constructors = type.GetConstructors();
        var constructor = constructors.Length == 1 ? constructors
            .SingleOrDefault(c => c.GetParameters().Select(p => (p.Name, p.ParameterType))
                .SequenceEqual(properties.Select(p => (p.Name, p.PropertyType)))) : null;

        if (constructor == null)
        {
            var setterMap = GetSetters<T>(reader, config);
            constructor = type.GetConstructor(Array.Empty<Type>());
            return record =>
            {
                var item = (T)constructor.Invoke(null);
                foreach (var setter in setterMap)
                {
                    var val = DBNullHelper.FromDb(record.GetValue(setter.FieldIndex));
                    setter.SetValue(item, val);
                }
                return item;
            };
        }
        else
        {
            return record =>
            {
                var values = properties.Select(p => DBNullHelper.FromDb(record.GetValue(record.GetOrdinal(p.Name))));
                return (T)constructor.Invoke(values.ToArray());
            };
        }

    }

    /// <summary>
    /// Convert a datarecord into a dynamic object, so that properties can be simply accessed
    /// using standard C# syntax.
    /// </summary>
    /// <param name="rdr">the data record</param>
    /// <returns>A dynamic object with fields corresponding to the database columns</returns>
    internal static dynamic ToExpando(this IDataRecord rdr) => Dynamic.From(rdr.NameValues().ToDictionary(p => p.name, p => p.value));

    internal static IEnumerable<(string name, object? value)>NameValues(this IDataRecord record)
    {
        for (var i = 0; i < record.FieldCount; i++) yield return (record.GetName(i), record[i]);
    }
    /// <summary>
    /// Get a value from an IDataRecord by column name. This method supports all types,
    /// as long as the DbType is convertible to the CLR Type passed as a generic argument.
    /// Also handles conversion from DbNull to null, including nullable types.
    /// </summary>
    public static TResult? Get<TResult>(this IDataRecord record, string name) => record.Get<TResult>(record.GetOrdinal(name));

    /// <summary>
    /// Get a value from an IDataRecord by index. This method supports all types,
    /// as long as the DbType is convertible to the CLR Type passed as a generic argument.
    /// Also handles conversion from DbNull to null, including nullable types.
    /// </summary>
    public static TResult? Get<TResult>(this IDataRecord record, int c) => ConvertTo<TResult>.From(record[c]);
}
