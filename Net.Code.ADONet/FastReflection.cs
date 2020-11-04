using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Net.Code.ADONet
{
    internal sealed class FastReflection<T>
    {
        private FastReflection() { }
        private static readonly Type Type = typeof(FastReflection<T>);
        public static FastReflection<T> Instance = new FastReflection<T>();
        public IReadOnlyDictionary<string, Action<T, object?>> GetSettersForType()
            => _setters.GetOrAdd(
                typeof(T),
                d => d.GetProperties().Where(p => p.SetMethod != null).ToDictionary(p => p.Name, GetSetDelegate)
            );
        private readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, Action<T, object?>>> _setters = new();
        private static Action<T, object?> GetSetDelegate(PropertyInfo p)
        {
            var method = p.GetSetMethod();
            Type parameterType = method.GetParameters()[0].ParameterType;
            var delegateType = typeof(Action<,>).MakeGenericType(typeof(T), parameterType);
            var action = method.CreateDelegate(delegateType);
            return (target, param) => action.DynamicInvoke(target, ConvertEx.To(param, parameterType));
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
            var delegateType = typeof(Func<,>).MakeGenericType(typeof(T), method.ReturnType);
            var func = method.CreateDelegate(delegateType);
            return target => ConvertEx.To(func.DynamicInvoke(target), method.ReturnType);
        }
    }
}