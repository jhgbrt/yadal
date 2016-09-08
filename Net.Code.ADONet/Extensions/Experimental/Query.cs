using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Net.Code.ADONet.Extensions
{
    public class Query<T> : IQueryGenerator
    {
        private string _insert;
        private string _delete;
        private string _update;
        private string _selectAll;
        private string _select;
        private string _count;

        public static IQueryGenerator Create(string providerName) => Create(DbConfig.FromProviderName(providerName).MappingConvention);
        internal static IQueryGenerator Create(MappingConvention convention) => new Query<T>(convention);

        Query(MappingConvention convention)
        {
            var properties = typeof(T).GetProperties();

            var keyProperties = properties.Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(KeyAttribute))).ToArray();
            if (!keyProperties.Any())
                keyProperties = properties.Where(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (!keyProperties.Any())
                keyProperties = properties.Where(p => p.Name.Equals($"{typeof(T).Name}Id", StringComparison.OrdinalIgnoreCase)).ToArray();

            var dbGenerated = keyProperties.Where(p => p.HasCustomAttribute<DatabaseGeneratedAttribute>(a => a.DatabaseGeneratedOption != DatabaseGeneratedOption.None));

            var allPropertyNames = properties.Select(p => convention.ToDb(p.Name)).ToArray();
            var insertPropertyNames = properties.Except(dbGenerated).Select(p => p.Name).ToArray();
            var keyPropertyNames = keyProperties.Select(p => p.Name).ToArray();
            var nonKeyProperties = properties.Except(keyProperties).ToArray();
            var nonKeyPropertyNames = nonKeyProperties.Select(p => p.Name).ToArray();

            Func<string,string> assign = s => $"{convention.ToDb(s)} = {convention.Parameter(s)}";
            var insertColumns = string.Join(", ", insertPropertyNames.Select(convention.ToDb));
            var insertValues = string.Join(", ", insertPropertyNames.Select(s => $"{convention.Parameter(s)}"));
            var whereClause = string.Join(" AND ", keyPropertyNames.Select(assign));
            var updateColumns = string.Join(", ", nonKeyPropertyNames.Select(assign));
            var allColumns = string.Join(", ", allPropertyNames);
            var tableName = convention.ToDb(typeof(T).Name);

            _insert = $"INSERT INTO {tableName} ({insertColumns}) VALUES ({insertValues})";
            _delete = $"DELETE FROM {tableName} WHERE {whereClause}";
            _update = $"UPDATE {tableName} SET {updateColumns} WHERE {whereClause}";
            _select = $"SELECT {allColumns} FROM {tableName} WHERE {whereClause}";
            _selectAll = $"SELECT {allColumns} FROM {tableName}";
            _count = $"SELECT COUNT(*) FROM {tableName}";
        }

        public string Insert => _insert;
        public string Delete => _delete;
        public string Update => _update;
        public string Select => _select;
        public string SelectAll => _selectAll;
        public string Count => _count;
    }

    internal static class TypeExtensions
    {
        public static bool HasCustomAttribute<TAttribute>(this MemberInfo t, Func<TAttribute, bool> whereClause)
            => t.GetCustomAttributes(false).OfType<TAttribute>().Where(whereClause).Any();
    }
}