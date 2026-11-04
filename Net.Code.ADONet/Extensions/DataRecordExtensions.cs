namespace Net.Code.ADONet;

internal static class DataRecordExtensions
{
    private record struct Setter<T>(int FieldIndex, Action<T, object?> SetValue);

    extension(IDataReader reader)
    {
        private List<Setter<T>> GetSetters<T>(DbConfig config)
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

        internal Func<IDataRecord, T> GetMapper<T>(DbConfig config)
        {
            var type = typeof(T);
            var properties = type.GetProperties();

            // convention: if there is only a constructor with parameters for all properties
            // assume basic 'record-like' class
            var constructors = type.GetConstructors();
            var constructor = constructors.Length == 1 ? constructors.SingleOrDefault(c => c.Parameters().SequenceEqual(properties.Select(p => (p.Name, p.PropertyType)))) : null;

            if (constructor == null)
            {
                var setterMap = reader.GetSetters<T>(config);
                var defaultConstructor = type.GetConstructor([]);
                return record =>
                {
                    var item = (T)(defaultConstructor ?? throw new InvalidOperationException(
                        $"Cannot map to {type}: it needs a public parameterless constructor, or a single constructor with a parameter for each property")).Invoke(null);
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
    }

    extension(ConstructorInfo constructor)
    {
        private IEnumerable<(string name, Type type)> Parameters() => constructor.GetParameters().Select(p => (p.Name ?? string.Empty, p.ParameterType));
    }

    private static readonly MethodInfo CreateConverterMethod = typeof(DataRecordExtensions).GetMethod(nameof(CreateConverterFor), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static Func<object?, object?> CreateConverter(Type type)
        => (Func<object?, object?>)CreateConverterMethod.MakeGenericMethod(type).Invoke(null, null)!;

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

    extension(IDataRecord record)
    {
        /// <summary>
        /// Convert a datarecord into a dynamic object, so that properties can be simply accessed
        /// using standard C# syntax.
        /// </summary>
        /// <returns>A dynamic object with fields corresponding to the database columns</returns>
        internal dynamic ToExpando() => Dynamic.From(record.NameValues().ToDictionary(p => p.name, p => p.value));

        internal IEnumerable<(string name, object? value)> NameValues()
        {
            for (var i = 0; i < record.FieldCount; i++) yield return (record.GetName(i), record[i]);
        }

        /// <summary>
        /// Get a value from an IDataRecord by column name. This method supports all types,
        /// as long as the DbType is convertible to the CLR Type passed as a generic argument.
        /// Also handles conversion from DbNull to null, including nullable types.
        /// </summary>
        public TResult? Get<TResult>(string name) => record.Get<TResult>(record.GetOrdinal(name));

        /// <summary>
        /// Get a value from an IDataRecord by index. This method supports all types,
        /// as long as the DbType is convertible to the CLR Type passed as a generic argument.
        /// Also handles conversion from DbNull to null, including nullable types.
        /// </summary>
        public TResult? Get<TResult>(int c) => ConvertTo<TResult>.From(record[c]);
    }
}
