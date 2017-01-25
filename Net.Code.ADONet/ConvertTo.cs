using System;
using System.Reflection;

namespace Net.Code.ADONet
{
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
        public static readonly Func<object, T> From;

        static ConvertTo()
        {
            // Sets the From delegate, depending on whether T is a reference type, a nullable value type or a value type.
            From = CreateConvertFunction(typeof(T).GetTypeInfo());
        }

        private static Func<object, T> CreateConvertFunction(TypeInfo type)
        {
            if (!type.IsValueType)
            {
                return ConvertRefType;
            }

            if (!type.IsNullableType())
            {
                return ConvertValueType;
            }

            var delegateType = typeof(Func<object, T>);
            var methodInfo = typeof(ConvertTo<T>).GetTypeInfo().GetMethod("ConvertNullableValueType", BindingFlags.NonPublic | BindingFlags.Static);
            var genericMethodForElement = methodInfo.MakeGenericMethod(type.GetGenericArguments()[0]);
            return (Func<object, T>)genericMethodForElement.CreateDelegate(delegateType);
        }

        // ReSharper disable once UnusedMember.Local
        // (used via reflection!)
        private static TElem? ConvertNullableValueType<TElem>(object value) where TElem : struct 
            => DBNullHelper.IsNull(value) ? (TElem?)null : ConvertPrivate<TElem>(value);

        private static T ConvertRefType(object value) => DBNullHelper.IsNull(value) ? default(T) : ConvertPrivate<T>(value);

        private static T ConvertValueType(object value)
        {
            if (DBNullHelper.IsNull(value))
            {
                throw new NullReferenceException("Value is DbNull");
            }
            return ConvertPrivate<T>(value);
        }

        private static TElem ConvertPrivate<TElem>(object value) => (TElem)(Convert.ChangeType(value, typeof(TElem)));
    }
}