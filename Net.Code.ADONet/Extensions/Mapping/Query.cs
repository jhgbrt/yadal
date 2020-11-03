using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Net.Code.ADONet.Extensions.Mapping
{
    internal sealed class Query<T> : IQuery
    {
        // ReSharper disable StaticMemberInGenericType
        private static readonly PropertyInfo[] Properties;
        private static readonly PropertyInfo[] KeyProperties;
        private static readonly PropertyInfo[] DbGenerated;
        // ReSharper restore StaticMemberInGenericType

        internal static IQuery Create(IMappingConvention convention) => new Query<T>(convention);

        static Query()
        {
            Properties = typeof(T).GetProperties();

            KeyProperties = Properties.Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(KeyAttribute))).ToArray();
            if (KeyProperties.Length == 0)
                KeyProperties = Properties.Where(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (KeyProperties.Length == 0)
                KeyProperties = Properties.Where(p => p.Name.Equals($"{typeof(T).Name}Id", StringComparison.OrdinalIgnoreCase)).ToArray();

            DbGenerated = KeyProperties.Where(p => p.HasCustomAttribute<DatabaseGeneratedAttribute>(a => a.DatabaseGeneratedOption != DatabaseGeneratedOption.None)).ToArray();
        }

        private Query(IMappingConvention convention)
        {
            var allPropertyNames = Properties.Select(p => convention.ToDb(p.Name)).ToArray();
            var insertPropertyNames = Properties.Except(DbGenerated).Select(p => p.Name).ToArray();
            var keyPropertyNames = KeyProperties.Select(p => p.Name).ToArray();
            var nonKeyProperties = Properties.Except(KeyProperties).ToArray();
            var nonKeyPropertyNames = nonKeyProperties.Select(p => p.Name).ToArray();

            string assign(string s) => $"{convention.ToDb(s)} = {convention.Parameter(s)}";
            var insertColumns = string.Join(", ", insertPropertyNames.Select(convention.ToDb));
            var insertValues = string.Join(", ", insertPropertyNames.Select(s => $"{convention.Parameter(s)}"));
            var whereClause = string.Join(" AND ", keyPropertyNames.Select(assign));
            var updateColumns = string.Join(", ", nonKeyPropertyNames.Select(assign));
            var allColumns = string.Join(", ", allPropertyNames);
            var tableName = convention.ToDb(typeof(T).Name);

            Insert = $"INSERT INTO {tableName} ({insertColumns}) VALUES ({insertValues})";
            Delete = $"DELETE FROM {tableName} WHERE {whereClause}";
            Update = $"UPDATE {tableName} SET {updateColumns} WHERE {whereClause}";
            Select = $"SELECT {allColumns} FROM {tableName} WHERE {whereClause}";
            SelectAll = $"SELECT {allColumns} FROM {tableName}";
            Count = $"SELECT COUNT(*) FROM {tableName}";
        }

        public string Insert { get; }

        public string Delete { get; }

        public string Update { get; }

        public string Select { get; }

        public string SelectAll { get; }

        public string Count { get; }
    }
}