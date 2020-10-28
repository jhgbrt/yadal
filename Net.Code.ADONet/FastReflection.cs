using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Net.Code.ADONet
{
    public class FastReflection
    {
        private FastReflection() { }
        private static Type Type = typeof(FastReflection);
        public static FastReflection Instance = new FastReflection();
        public IReadOnlyDictionary<string, Action<T, object>> GetSettersForType<T>()
        {
            var setters = _setters.GetOrAdd(
                new { Type = typeof(T) },
                d => ((Type)d.Type).GetProperties().Where(p => p.SetMethod != null).ToDictionary(p => p.Name, GetSetDelegate<T>)
                );
            return (IReadOnlyDictionary<string, Action<T, object>>)setters;
        }
        private readonly ConcurrentDictionary<dynamic, object> _setters = new ConcurrentDictionary<dynamic, object>();
        static Action<T, object> GetSetDelegate<T>(PropertyInfo p)
        {
            var method = p.GetSetMethod();
            var genericHelper = Type.GetMethod(nameof(CreateSetterDelegateHelper), BindingFlags.Static | BindingFlags.NonPublic);
            var constructedHelper = genericHelper.MakeGenericMethod(typeof(T), method.GetParameters()[0].ParameterType);
            return (Action<T, object>)constructedHelper.Invoke(null, new object[] { method });
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        // ReSharper disable once UnusedMember.Local
        static object CreateSetterDelegateHelper<TTarget, TProperty>(MethodInfo method) where TTarget : class
        {
            var action = (Action<TTarget, TProperty>)method.CreateDelegate(typeof(Action<TTarget, TProperty>));
            Action<TTarget, object> ret = (target, param) => action(target, ConvertTo<TProperty>.From(param));
            return ret;
        }

        public IReadOnlyDictionary<string, Func<T, object>> GetGettersForType<T>()
        {
            var getters = _getters.GetOrAdd(
                new { Type = typeof(T) },
                d => ((Type)d.Type).GetProperties().Where(p => p.GetMethod != null).ToDictionary(p => p.Name, GetGetDelegate<T>)
                );
            return (IReadOnlyDictionary<string, Func<T, object>>)getters;
        }
        private readonly ConcurrentDictionary<dynamic, object> _getters = new ConcurrentDictionary<dynamic, object>();
        static Func<T, object> GetGetDelegate<T>(PropertyInfo p)
        {
            var method = p.GetGetMethod();
            var genericHelper = Type.GetMethod(nameof(CreateGetterDelegateHelper), BindingFlags.Static | BindingFlags.NonPublic);
            var constructedHelper = genericHelper.MakeGenericMethod(typeof(T), method.ReturnType);
            return (Func<T, object>)constructedHelper.Invoke(null, new object[] { method });
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        // ReSharper disable once UnusedMember.Local
        static object CreateGetterDelegateHelper<TTarget, TProperty>(MethodInfo method) where TTarget : class
        {
            var func = (Func<TTarget, TProperty>)method.CreateDelegate(typeof(Func<TTarget, TProperty>));
            Func<TTarget, object> ret = target => ConvertTo<TProperty>.From(func(target));
            return ret;
        }

    }
}