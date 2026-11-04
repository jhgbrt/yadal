namespace Net.Code.ADONet;

using static DBNullHelper;

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

    static ConvertTo() =>
        // Sets the From delegate, depending on whether T is a reference type, a nullable value type or a value type.
        From = CreateConvertFunction(typeof(T));

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

    private static T ConvertValueType(object? value) => IsNull(value) ? throw new NullReferenceException("Value is DbNull") : ConvertPrivate<T>(value!);

    private static TElem ConvertPrivate<TElem>(object value) => value switch
    {
        // already the requested type: no conversion needed. This also covers types that do not
        // implement IConvertible (DateTimeOffset, Guid, byte[], ...), which Convert.ChangeType rejects.
        TElem t => t,
        _ when typeof(TElem).IsEnum => (TElem)ConvertToEnum(value, typeof(TElem)),
        // DateTimeOffset is not IConvertible; same conversion as DataRecordGetterExtensions.GetDateTimeOffset
        DateTime d when typeof(TElem) == typeof(DateTimeOffset) => (TElem)(object)new DateTimeOffset(d),
#if NET6_0_OR_GREATER
        DateOnly or TimeOnly or DateTime or TimeSpan when ConvertDateOrTime(value, typeof(TElem)) is { } converted => (TElem)converted,
#endif
        _ => (TElem)Convert.ChangeType(value, typeof(TElem))
    };

#if NET6_0_OR_GREATER
    // DateOnly and TimeOnly are not IConvertible, so Convert.ChangeType cannot map them to or from
    // DateTime/TimeSpan. Npgsql (since v10) returns DateOnly for 'date' and TimeOnly for 'time' columns,
    // while most other providers return DateTime and TimeSpan.
    private static object? ConvertDateOrTime(object value, Type targetType) => value switch
    {
        DateOnly d when targetType == typeof(DateTime) => d.ToDateTime(TimeOnly.MinValue),
        DateTime d when targetType == typeof(DateOnly) => DateOnly.FromDateTime(d),
        TimeOnly t when targetType == typeof(TimeSpan) => t.ToTimeSpan(),
        TimeSpan t when targetType == typeof(TimeOnly) => TimeOnly.FromTimeSpan(t),
        _ => null
    };
#endif

    // Enums: strings are parsed by name (case-insensitive), numeric values are converted
    // to the underlying integral type first (e.g. Int64 from SQLite into an int-based enum).
    private static object ConvertToEnum(object value, Type enumType) => value switch
    {
        string s => Enum.Parse(enumType, s, ignoreCase: true),
        _ => Enum.ToObject(enumType, Convert.ChangeType(value, Enum.GetUnderlyingType(enumType)))
    };
}
