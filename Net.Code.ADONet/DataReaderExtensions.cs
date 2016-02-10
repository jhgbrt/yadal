using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Net.Code.ADONet
{
    static class DataReaderExtensions
    {
        public static IEnumerable<IDataRecord> AsEnumerable(this IDataReader reader)
        {
            using (reader) { while (reader.Read()) yield return reader; }
        }

        internal static IEnumerable<dynamic> ToExpandoList(this IEnumerable<IDataRecord> input) 
            => input.Select(item => item.ToExpando());

        internal static IEnumerable<dynamic> ToDynamicDataRecord(this IEnumerable<IDataRecord> input) 
            => input.Select(item => Dynamic.From(item));

        internal static IEnumerable<List<dynamic>> ToMultiResultSet(this IDataReader reader)
        {
            do
            {
                var list = new List<dynamic>();
                while (reader.Read()) list.Add(reader.ToExpando());
                yield return list;
            } while (reader.NextResult());
        }

        internal static List<T> GetResultSet<T>(this IDataReader reader, DbConfig config, out bool moreResults) where T : new()
        {
            var list = new List<T>();
            while (reader.Read()) list.Add(reader.MapTo<T>(config));
            moreResults = reader.NextResult();
            return list;
        }
    }
}