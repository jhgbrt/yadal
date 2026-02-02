using System.ComponentModel.DataAnnotations.Schema;

namespace Net.Code.ADONet;

internal static class TypeExtensions
{
    public static bool HasCustomAttribute<TAttribute>(this MemberInfo t, Func<TAttribute, bool> whereClause)
        => t.GetCustomAttributes(false).OfType<TAttribute>().Any(whereClause);
    public static Type GetUnderlyingType(this Type type)
        => type.IsNullableType() ? Nullable.GetUnderlyingType(type) : type;
    public static bool IsNullableType(this Type type)
        => type.IsGenericType
            && !type.IsGenericTypeDefinition
            && typeof(Nullable<>) == type.GetGenericTypeDefinition();
    public static string GetTableName(this Type type, MappingConvention convention)
    {
        var attribute = type.GetCustomAttributes(false).OfType<TableAttribute>().FirstOrDefault();
        return convention.ToDb(attribute?.Name ?? type.Name);
    }
    public static string GetColumnName(this PropertyInfo property, MappingConvention convention)
    {
        var attribute = property.GetCustomAttributes(true).OfType<ColumnAttribute>().FirstOrDefault();
        return convention.ToDb(attribute?.Name ?? property.Name);
    }
}
