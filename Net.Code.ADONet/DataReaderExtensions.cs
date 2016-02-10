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

        public static IEnumerable<dynamic> ToExpandoList(this IEnumerable<IDataRecord> input) 
            => input.Select(item => DataRecordExtensions.ToExpando(item));

        public static IEnumerable<dynamic> ToDynamicDataRecord(this IEnumerable<IDataRecord> input) 
            => input.Select(item => Dynamic.From(item));

        public static IEnumerable<List<dynamic>> ToMultiResultSet(this IDataReader reader)
        {
            do
            {
                var list = new List<dynamic>();
                while (reader.Read()) list.Add(reader.ToExpando());
                yield return list;
            } while (reader.NextResult());
        }

        public static List<T> GetResultSet<T>(this IDataReader reader, MappingConvention convention, string provider, out bool moreResults) where T : new()
        {
            var list = new List<T>();
            while (reader.Read()) list.Add(reader.MapTo<T>(convention, provider));
            moreResults = reader.NextResult();
            return list;
        }
    }
}