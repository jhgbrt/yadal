namespace Net.Code.ADONet;

internal static class DataRecordExtensions
{
    private record struct Setter<T>(int FieldIndex, Action<T, object?> SetValue);
    private static List<Setter<T>> GetSetters<T>(this IDataReader reader, DbConfig config)
    {
        var convention = config.MappingConvention;
        var setters = FastReflection<T>.Instance.GetSettersForType(convention);

        var list = new List<Setter<T>>(reader.FieldCount);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var columnName = reader.GetName(i);
            if (setters.TryGetValue(columnName, out var setter))
            {
                list.Add(new(i, setter));
            }
        }
        return list;
    }

    static IEnumerable<(string name, Type type)> Parameters(this ConstructorInfo constructors) => constructors.GetParameters().Select(p => (p.Name, p.ParameterType));

    internal static Func<IDataRecord, T> GetMapper<T>(this IDataReader reader, DbConfig config)
    {
        var type = typeof(T);
        var properties = type.GetProperties();

        // convention: if there is only a constructor with parameters for all properties
        // assume basic 'record-like' class
        var constructors = type.GetConstructors();
        var constructor = constructors.Length == 1 ? constructors.SingleOrDefault(c => c.Parameters().SequenceEqual(properties.Select(p => (p.Name, p.PropertyType)))) : null;

        if (constructor == null)
        {
            var setterMap = GetSetters<T>(reader, config);
            constructor = type.GetConstructor([]);
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
            // resolve column ordinals and per-parameter converters once, not for every row
            var ordinals = properties.Select(p => reader.GetOrdinal(p.GetColumnName(config.MappingConvention))).ToArray();
            var converters = constructor.GetParameters().Select(p => CreateConverter(p.ParameterType)).ToArray();
            return record =>
            {
                var values = new object?[ordinals.Length];
                for (var i = 0; i < ordinals.Length; i++)
                {
                    values[i] = converters[i](DBNullHelper.FromDb(record.GetValue(ordinals[i])));
                }
                return (T)constructor.Invoke(values);
            };
        }

    }

    private static readonly MethodInfo CreateConverterMethod = typeof(DataRecordExtensions).GetMethod(nameof(CreateConverterFor), BindingFlags.NonPublic | BindingFlags.Static);

    private static Func<object?, object?> CreateConverter(Type type)
        => (Func<object?, object?>)CreateConverterMethod.MakeGenericMethod(type).Invoke(null, null);

    // - null (DBNull) is passed through unchanged, as before: ConstructorInfo.Invoke turns it into
    //   default(T) for non-nullable value type parameters and null otherwise.
    // - values already assignable to the parameter type are passed through as-is (e.g. a Guid for
    //   an object parameter, a byte[] for an IEnumerable<byte> parameter).
    // - anything else is converted with the same semantics as the property setter path.
    private static Func<object?, object?> CreateConverterFor<TValue>() => value => value switch
    {
        null => null,
        TValue t => t,
        _ => ConvertTo<TValue>.From(value)
    };

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
