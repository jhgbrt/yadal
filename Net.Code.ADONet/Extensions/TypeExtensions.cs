using System.ComponentModel.DataAnnotations.Schema;

namespace Net.Code.ADONet;

internal static class TypeExtensions
{
    extension(MemberInfo t)
    {
        public bool HasCustomAttribute<TAttribute>(Func<TAttribute, bool> whereClause)
            => t.GetCustomAttributes(false).OfType<TAttribute>().Any(whereClause);
    }

    extension(Type type)
    {
        public Type GetUnderlyingType()
            => Nullable.GetUnderlyingType(type) ?? type;
        public bool IsNullableType()
            => type.IsGenericType
                && !type.IsGenericTypeDefinition
                && typeof(Nullable<>) == type.GetGenericTypeDefinition();
        public string GetTableName(MappingConvention convention)
        {
            var attribute = type.GetCustomAttributes(false).OfType<TableAttribute>().FirstOrDefault();
            return convention.ToDb(attribute?.Name ?? type.Name);
        }
    }

    extension(PropertyInfo property)
    {
        public string GetColumnName(MappingConvention convention)
        {
            var attribute = property.GetCustomAttributes(true).OfType<ColumnAttribute>().FirstOrDefault();
            return convention.ToDb(attribute?.Name ?? property.Name);
        }
    }
}
