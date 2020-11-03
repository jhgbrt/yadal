using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace System.Runtime.CompilerServices { internal class IsExternalInit { } }
namespace Net.Code.ADONet
{
    internal static class DataRecordExtensions
    {
        internal record Setter<T>(int FieldIndex, Action<T, object?> SetValue);

        internal class SetterMap<T> : List<Setter<T>> { }

        internal static SetterMap<T> GetSetterMap<T>(this IDataReader reader, DbConfig config)
        {
            var map = new SetterMap<T>();
            var convention = config.MappingConvention;
            var setters = FastReflection<T>.Instance.GetSettersForType();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var columnName = convention.FromDb(reader.GetName(i));
                if (setters.TryGetValue(columnName, out var setter))
                {
                    map.Add(new Setter<T>(i, setter));
                }
            }
            return map;
        }

        internal static T MapTo<T>(this IDataRecord record, SetterMap<T> setterMap)
        {
            var result = Activator.CreateInstance<T>();
            foreach (var setter in setterMap)
            {
                var val = DBNullHelper.FromDb(record.GetValue(setter.FieldIndex));
                setter.SetValue(result, val);
            }
            return result;
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
}