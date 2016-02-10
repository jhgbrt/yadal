using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Net.Code.ADONet
{
    public static class DataRecordExtensions
    {
        internal static T MapTo<T>(this IDataRecord record, DbConfig config)
        {
            var convention = config.MappingConvention ?? MappingConvention.Strict;
            var setters = GetSettersForType<T>(p => convention.GetName(p), config.ProviderName);
            var result = Activator.CreateInstance<T>();
            for (var i = 0; i < record.FieldCount; i++)
            {
                Action<T,object> setter;
                var columnName = convention.GetName(record, i);
                if (!setters.TryGetValue(columnName, out setter))
                    continue;
                var val = DBNullHelper.FromDb(record.GetValue(i));
                setter(result, val);
            }
            return result;
        }

        private static readonly ConcurrentDictionary<dynamic, object> Setters = new ConcurrentDictionary<dynamic, object>();
        private static IDictionary<string, Action<T, object>> GetSettersForType<T>(Func<PropertyInfo, string> getName, string provider) 
        {
            var setters = Setters.GetOrAdd(
                new {Type =  typeof (T), Provider = provider},
                d =>((Type)d.Type).GetProperties().ToDictionary(getName, GetSetDelegate<T>)
                );
            return (IDictionary<string, Action<T,object>>)setters;
        }

        static Action<T,object> GetSetDelegate<T>(this PropertyInfo p)
        {
            var method = p.GetSetMethod();
            var genericHelper = typeof(DataRecordExtensions).GetMethod(nameof(CreateSetterDelegateHelper), BindingFlags.Static | BindingFlags.NonPublic);
            var constructedHelper = genericHelper.MakeGenericMethod(typeof (T), method.GetParameters()[0].ParameterType);
            return (Action<T, object>)constructedHelper.Invoke(null, new object[] { method });
        }
        // ReSharper disable once UnusedMethodReturnValue.Local
        // ReSharper disable once UnusedMember.Local
        static object CreateSetterDelegateHelper<TTarget, TParam>(MethodInfo method) where TTarget : class
        {
            var action = (Action<TTarget, TParam>)Delegate.CreateDelegate(typeof(Action<TTarget, TParam>), method);
            Action<TTarget, object> ret = (target, param) => action(target, ConvertTo<TParam>.From(param));
            return ret;
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