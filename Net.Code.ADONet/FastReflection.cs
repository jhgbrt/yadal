namespace Net.Code.ADONet;

/// <summary>
/// Non-generic counterpart of <see cref="FastReflection{T}"/>, for when the type is only known at runtime.
/// Getters are compiled once per type and cached.
/// </summary>
internal static class FastReflection
{
    private static readonly ConcurrentDictionary<Type, (string name, Func<object, object?> getter)[]> _getters = new();

    /// <summary>
    /// Returns the (readable, non-indexer) public properties of the given type, in declaration order,
    /// with a fast getter for each.
    /// </summary>
    public static IReadOnlyList<(string name, Func<object, object?> getter)> GetGetters(Type type)
        => _getters.GetOrAdd(
            type,
            t => t.GetProperties()
                .Where(p => p.GetMethod is { IsStatic: false } && p.GetIndexParameters().Length == 0)
                .Select(p => (p.Name, GetGetDelegate(p)))
                .ToArray()
        );

    private static Func<object, object?> GetGetDelegate(PropertyInfo p)
    {
        // open instance delegates over value type getters are not supported; fall back to reflection
        if (p.DeclaringType!.IsValueType)
            return target => p.GetValue(target);
        var method = p.GetGetMethod();
        var genericHelper = typeof(FastReflection).GetMethod(nameof(CreateGetterDelegateHelper), BindingFlags.Static | BindingFlags.NonPublic);
        var constructedHelper = genericHelper.MakeGenericMethod(p.DeclaringType, method.ReturnType);
        return (Func<object, object?>)constructedHelper.Invoke(null, new object[] { method });
    }

    private static Func<object, object?> CreateGetterDelegateHelper<TTarget, TProperty>(MethodInfo method)
    {
        var func = (Func<TTarget, TProperty>)method.CreateDelegate(typeof(Func<TTarget, TProperty>));
        return target => ConvertTo<TProperty>.From(func((TTarget)target));
    }
}

internal sealed class FastReflection<T>
{
    private FastReflection() { }
    private static readonly Type Type = typeof(FastReflection<T>);
    public static FastReflection<T> Instance = new();
    public IReadOnlyDictionary<string, Action<T, object?>> GetSettersForType(MappingConvention convention)
        => _setters.GetOrAdd(
            (typeof(T), convention),
            d => d.type.GetProperties().Where(p => p.SetMethod != null)
            .ToDictionary(p => p.GetColumnName(convention), GetSetDelegate)
        );
    private readonly ConcurrentDictionary<(Type type, MappingConvention convention), IReadOnlyDictionary<string, Action<T, object?>>> _setters = new();
    private static Action<T, object?> GetSetDelegate(PropertyInfo p)
    {
        var method = p.GetSetMethod();
        var genericHelper = Type.GetMethod(nameof(CreateSetterDelegateHelper), BindingFlags.Static | BindingFlags.NonPublic);
        var constructedHelper = genericHelper.MakeGenericMethod(typeof(T), method.GetParameters()[0].ParameterType);
        return (Action<T, object?>)constructedHelper.Invoke(null, new object[] { method });
    }

    private static Action<TTarget, object> CreateSetterDelegateHelper<TTarget, TProperty>(MethodInfo method)
    {
        var action = (Action<TTarget, TProperty?>)method.CreateDelegate(typeof(Action<TTarget, TProperty>));
        return (target, param) => action(target, ConvertTo<TProperty>.From(param));
    }

    public IReadOnlyDictionary<string, Func<T, object?>> GetGettersForType()
        => _getters.GetOrAdd(
            typeof(T),
            t => t.GetProperties().Where(p => p.GetMethod != null).ToDictionary(p => p.Name, GetGetDelegate)
        );
    private readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, Func<T, object?>>> _getters = new();
    private static Func<T, object?> GetGetDelegate(PropertyInfo p)
    {
        var method = p.GetGetMethod();
        var genericHelper = Type.GetMethod(nameof(CreateGetterDelegateHelper), BindingFlags.Static | BindingFlags.NonPublic);
        var constructedHelper = genericHelper.MakeGenericMethod(typeof(T), method.ReturnType);
        return (Func<T, object?>)constructedHelper.Invoke(null, new object[] { method });
    }

    private static Func<TTarget, object?> CreateGetterDelegateHelper<TTarget, TProperty>(MethodInfo method)
    {
        var func = (Func<TTarget, TProperty>)method.CreateDelegate(typeof(Func<TTarget, TProperty>));
        return target => ConvertTo<TProperty>.From(func(target));
    }
}
