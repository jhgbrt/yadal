using System.Collections.Generic;
using System.Data;
#nullable enable

namespace Net.Code.ADONet
{
    static class DataReaderExtensions
    {
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