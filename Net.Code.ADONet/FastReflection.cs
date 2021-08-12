namespace Net.Code.ADONet;

internal sealed class FastReflection<T>
{
    private FastReflection() { }
    private static readonly Type Type = typeof(FastReflection<T>);
    public static FastReflection<T> Instance = new();
    public IReadOnlyDictionary<string, Action<T, object?>> GetSettersForType()
        => _setters.GetOrAdd(
            typeof(T),
            d => d.GetProperties().Where(p => p.SetMethod != null).ToDictionary(p => p.Name, GetSetDelegate)
        );
    private readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, Action<T, object?>>> _setters = new();
    private static Action<T, object?> GetSetDelegate(PropertyInfo p)
    {
        var method = p.GetSetMethod();
        var genericHelper = Type.GetMethod(nameof(CreateSetterDelegateHelper), BindingFlags.Static | BindingFlags.NonPublic);
        var constructedHelper = genericHelper.MakeGenericMethod(typeof(T), method.GetParameters()[0].ParameterType);
        return (Action<T, object?>)constructedHelper.Invoke(null, new object[] { method });
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    // ReSharper disable once UnusedMember.Local
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

    // ReSharper disable once UnusedMethodReturnValue.Local
    // ReSharper disable once UnusedMember.Local
    private static Func<TTarget, object?> CreateGetterDelegateHelper<TTarget, TProperty>(MethodInfo method)
    {
        var func = (Func<TTarget, TProperty>)method.CreateDelegate(typeof(Func<TTarget, TProperty>));
        return target => ConvertTo<TProperty>.From(func(target));
    }
}
