using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Net.Code.ADONet.Tests.Integration.TestSupport
{
    static class ExtensionsForLegacyComparison
    {
        private static IEnumerable<IDataRecord> AsEnumerable(this IDataReader reader)
        {
            using (reader) { while (reader.Read()) yield return reader; }
        }

        private static T MapTo<T>(this IDataRecord record, DbConfig config)
        {
            var convention = config.MappingConvention;
            var setters = FastReflection.Instance.GetSettersForType<T>();
            var result = Activator.CreateInstance<T>();
            for (var i = 0; i < record.FieldCount; i++)
            {
                Action<T, object> setter;
                var columnName = convention.FromDb(record.GetName(i));
                if (!setters.TryGetValue(columnName, out setter))
                    continue;
                var val = DBNullHelper.FromDb(record.GetValue(i));
                setter(result, val);
            }
            return result;
        }

        public static IEnumerable<T> AsEnumerableLegacy<T>(this CommandBuilder cb, DbConfig config) => cb.AsReader().AsEnumerable().Select(r => MapTo<T>(r, config));
    }
}