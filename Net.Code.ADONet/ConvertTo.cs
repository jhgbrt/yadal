using System;
using System.Reflection;

namespace Net.Code.ADONet
{
    using static DBNullHelper;

    public static class ConvertEx
    {
        public static object To(object o, Type targetType)
        {
            MethodInfo func = typeof(ConvertTo<>).MakeGenericType(targetType).GetMethod("InvokeFrom", BindingFlags.Static|BindingFlags.Public);
            return func.Invoke(null, new[] { o });
        }
    }
    /// <summary>
    /// Class for runtime type conversion, including DBNull.Value to/from null. Supports reference types,
    /// value types and nullable value types
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class ConvertTo<T>
    {
        // ReSharper disable once StaticFieldInGenericType
        // clearly we *want* a static field for each instantiation of this generic class...
        /// <summary>
        /// The actual conversion method. Converts an object to any type using standard casting functionality,
        /// taking into account null/nullable types and avoiding DBNull issues. This method is set as a delegate
        /// at runtime (in the static constructor).
        /// </summary>
        public static readonly Func<object?, T?> From;

        public static T? InvokeFrom(object? o) => From(o);

        static ConvertTo()
        {
            // Sets the From delegate, depending on whether T is a reference type, a nullable value type or a value type.
            From = CreateConvertFunction(typeof(T));
        }

        private static Func<object?, T?> CreateConvertFunction(Type type) => type switch
        {
            Type t when !t.IsValueType => ConvertRefType,
            Type t when !t.IsNullableType() => ConvertValueType,
            Type t => CreateConvertNullableValueTypeFunc(t)
        };

        private static Func<object?, T?> CreateConvertNullableValueTypeFunc(Type type)
        {
            var delegateType = typeof(Func<object?, T?>);
            var methodInfo = typeof(ConvertTo<T>).GetMethod("ConvertNullableValueType", BindingFlags.NonPublic | BindingFlags.Static);
            var genericMethodForElement = methodInfo.MakeGenericMethod(type.GetGenericArguments()[0]);
            return (Func<object?, T?>)genericMethodForElement.CreateDelegate(delegateType);
        }

#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable RCS1213 // Remove unused member declaration.
        private static TElem? ConvertNullableValueType<TElem>(object value) where TElem : struct
            => IsNull(value) ? default(TElem?) : ConvertPrivate<TElem>(value);
#pragma warning restore RCS1213 // Remove unused member declaration.
#pragma warning restore IDE0051 // Remove unused private members

        private static T? ConvertRefType(object? value) => IsNull(value) ? default : ConvertPrivate<T>(value!);

        private static T ConvertValueType(object? value)
        {
            if (IsNull(value))
            {
                throw new NullReferenceException("Value is DbNull");
            }
            return ConvertPrivate<T>(value!);
        }

        private static TElem ConvertPrivate<TElem>(object value) => (TElem)(Convert.ChangeType(value, typeof(TElem)));
    }
}