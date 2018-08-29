using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;

namespace Net.Code.ADONet
{
    static class DataReaderExtensions
    {
        public static IEnumerable<IDataRecord> AsEnumerable(this IDataReader reader)
        {
            using (reader)
            {
                while (reader.Read()) yield return reader;
            }
        }
        public static IEnumerable<T> AsEnumerable<T>(this IDataReader reader, DbConfig config)
        {
            var setterMap = reader.GetSetterMap<T>(config);
            using (reader)
            {
                while (reader.Read()) yield return reader.MapTo(setterMap);
            }
        }

        internal static IEnumerable<dynamic> ToExpandoList(this IDataReader reader)
        {
            using (reader)
            {
                while (reader.Read()) yield return reader.ToExpando();
            }
        }

        internal static IEnumerable<dynamic> ToDynamicDataRecord(this IDataReader reader)
        {
            using (reader)
            {
                while (reader.Read()) yield return Dynamic.From(reader);
            }
        }

        internal static IEnumerable<IReadOnlyCollection<dynamic>> ToMultiResultSet(this IDataReader reader)
        {
            do
            {
                var list = new Collection<dynamic>();
                while (reader.Read()) list.Add(reader.ToExpando());
                yield return list;
            } while (reader.NextResult());
        }

        internal static IReadOnlyCollection<T> GetResultSet<T>(this IDataReader reader, DbConfig config, out bool moreResults) 
        {
            var list = new List<T>();
            var map = reader.GetSetterMap<T>(config);
            while (reader.Read()) list.Add(reader.MapTo(map));
            moreResults = reader.NextResult();
            return list;
        }
    }
}