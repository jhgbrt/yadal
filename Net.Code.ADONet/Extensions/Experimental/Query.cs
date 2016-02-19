using System;
using System.Linq;

namespace Net.Code.ADONet.Extensions.Experimental
{
    [Obsolete("This is an experimental feature, API may change or be removed in future versions", false)]
    public static class Query<T>
    {
        public static string Insert(string providerName)
        {
            var mappingConvention = DbConfig.FromProviderName(providerName).MappingConvention;
            var propertyNames = typeof(T).GetProperties().Select(p => p.Name).ToArray();
            var columnNames = mappingConvention.JoinAsColumnNames(propertyNames);
            var parameterValues = mappingConvention.JoinAsVariableNames(propertyNames);
            var tableName = mappingConvention.ToDb(typeof(T).Name);
            return $"INSERT INTO {tableName} ({columnNames}) VALUES ({parameterValues})";
        }
    }
}