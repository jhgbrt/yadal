using System;
using System.ComponentModel;
using System.Reflection;

namespace Net.Code.ADONet
{
    public static class DBNullHelper
    {
        public static Type GetUnderlyingType(this Type type)
            => type.GetTypeInfo().IsNullableType() ? Nullable.GetUnderlyingType(type) : type;

        public static bool IsNullableType(this Type type) => type.GetTypeInfo().IsNullableType();

        public static bool IsNullableType(this TypeInfo type) 
            => (type.IsGenericType && !type.IsGenericTypeDefinition) &&
               (typeof(Nullable<>) == type.GetGenericTypeDefinition());
        public static bool IsNull(object o) => o == null || DBNull.Value.Equals(o);
        public static object FromDb(object o) => IsNull(o) ? null : o;
        public static object ToDb(object o) => IsNull(o) ? DBNull.Value : o;
    }
}