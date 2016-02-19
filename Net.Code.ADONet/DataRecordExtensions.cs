using System;
using System.Collections.Generic;
using System.Data;

namespace Net.Code.ADONet
{
    public static class DataRecordExtensions
    {
        internal static T MapTo<T>(this IDataRecord record, DbConfig config)
        {
            var convention = config.MappingConvention ?? MappingConvention.Default;
            var setters = FastReflection.Instance.GetSettersForType<T>();
            var result = Activator.CreateInstance<T>();
            for (var i = 0; i < record.FieldCount; i++)
            {
                Action<T,object> setter;
                var columnName = convention.FromDb(record.GetName(i));
                if (!setters.TryGetValue(columnName, out setter))
                    continue;
                var val = DBNullHelper.FromDb(record.GetValue(i));
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