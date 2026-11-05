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

    // Mapping only uses public, non-static, non-indexer accessors. A public property can have a
    // non-public getter or setter (e.g. { get; private set; }); such accessors are skipped.
    internal static bool IsReadable(PropertyInfo p)
        => p.GetMethod is { IsPublic: true, IsStatic: false } && p.GetIndexParameters().Length == 0;
    internal static bool IsWritable(PropertyInfo p)
        => p.SetMethod is { IsPublic: true, IsStatic: false } && p.GetIndexParameters().Length == 0;

    extension(PropertyInfo property)
    {
        public string GetColumnName(MappingConvention convention)
        {
            var attribute = property.GetCustomAttributes(true).OfType<ColumnAttribute>().FirstOrDefault();
            return convention.ToDb(attribute?.Name ?? property.Name);
        }
    }
}
