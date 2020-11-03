using System;
using System.Linq;
using System.Reflection;

namespace Net.Code.ADONet
{
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
    }
}