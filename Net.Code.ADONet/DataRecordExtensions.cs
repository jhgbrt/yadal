using System;
using System.Collections.Generic;
using System.Data;

namespace Net.Code.ADONet
{
    public static class DataRecordExtensions
    {
        internal class Setter<T>
        {
            public Setter(int fieldIndex, Action<T, object> action)
            {
                FieldIndex = fieldIndex;
                Action = action;
            }

            public int FieldIndex { get; private set; }
            public Action<T,object> Action { get; private set; }
        }

        internal class SetterMap<T> : List<Setter<T>> { }

        internal static SetterMap<T> GetSetterMap<T>(this IDataReader reader, DbConfig config)
        {
            var map = new SetterMap<T>();
            var convention = config.MappingConvention;
            var setters = FastReflection.Instance.GetSettersForType<T>();
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
            foreach (var item in setterMap)
            {
                var val = DBNullHelper.FromDb(record.GetValue(item.FieldIndex));
                var setter = item.Action;
                setter(result, val);
            }
            return result;
        }

        /// <summary>
        /// Convert a datarecord into a dynamic object, so that properties can be simply accessed
        /// using standard C# syntax.
        /// </summary>
        /// <param name="rdr">the data record</param>
        /// <returns>A dynamic object with fields corresponding to the database columns</returns>
        internal static dynamic ToExpando(this IDataRecord rdr)
        {
            var d = new Dictionary<string, object>();
            for (var i = 0; i < rdr.FieldCount; i++)
            {
                var name = rdr.GetName(i);
                var value = rdr[i];
                d.Add(name, value);
            }
            return Dynamic.From(d);
        }
        /// <summary>
        /// Get a value from an IDataRecord by column name. This method supports all types,
        /// as long as the DbType is convertible to the CLR Type passed as a generic argument.
        /// Also handles conversion from DbNull to null, including nullable types.
        /// </summary>
        public static TResult Get<TResult>(this IDataRecord reader, string name) => reader.Get<TResult>(reader.GetOrdinal(name));

        /// <summary>
        /// Get a value from an IDataRecord by index. This method supports all types,
        /// as long as the DbType is convertible to the CLR Type passed as a generic argument.
        /// Also handles conversion from DbNull to null, including nullable types.
        /// </summary>
        public static TResult Get<TResult>(this IDataRecord reader, int c) => ConvertTo<TResult>.From(reader[c]);
    }
}