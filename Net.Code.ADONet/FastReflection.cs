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
        public static FastReflection Instance = new FastReflection();
        public IDictionary<string, Action<T, object>> GetSettersForType<T>()
        {
            var setters = _setters.GetOrAdd(
                new { Type = typeof(T)},
                d => ((Type)d.Type).GetProperties().ToDictionary(p => p.Name, GetSetDelegate<T>)
                );
            return (IDictionary<string, Action<T, object>>)setters;
        }
        private readonly ConcurrentDictionary<dynamic, object> _setters = new ConcurrentDictionary<dynamic, object>();
        static Action<T, object> GetSetDelegate<T>(PropertyInfo p)
        {
            var method = p.GetSetMethod();
            var genericHelper = typeof(FastReflection).GetMethod(nameof(CreateSetterDelegateHelper), BindingFlags.Static | BindingFlags.NonPublic);
            var constructedHelper = genericHelper.MakeGenericMethod(typeof(T), method.GetParameters()[0].ParameterType);
            return (Action<T, object>)constructedHelper.Invoke(null, new object[] { method });
        }
        // ReSharper disable once UnusedMethodReturnValue.Local
        // ReSharper disable once UnusedMember.Local
        static object CreateSetterDelegateHelper<TTarget, TProperty>(MethodInfo method) where TTarget : class
        {
            var action = (Action<TTarget, TProperty>)Delegate.CreateDelegate(typeof(Action<TTarget, TProperty>), method);
            Action<TTarget, object> ret = (target, param) => action(target, ConvertTo<TProperty>.From(param));
            return ret;
        }

        public IDictionary<string, Func<T, object>> GetGettersForType<T>()
        {
            var setters = _getters.GetOrAdd(
                new { Type = typeof(T) },
                d => ((Type)d.Type).GetProperties().ToDictionary(p => p.Name, GetGetDelegate<T>)
                );
            return (IDictionary<string, Func<T, object>>)setters;
        }
        private readonly ConcurrentDictionary<dynamic, object> _getters = new ConcurrentDictionary<dynamic, object>();
        static Func<T, object> GetGetDelegate<T>(PropertyInfo p)
        {
            var method = p.GetGetMethod();
            var genericHelper = typeof(FastReflection).GetMethod(nameof(CreateGetterDelegateHelper), BindingFlags.Static | BindingFlags.NonPublic);
            var constructedHelper = genericHelper.MakeGenericMethod(typeof(T), method.ReturnType);
            return (Func<T, object>)constructedHelper.Invoke(null, new object[] { method });
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        // ReSharper disable once UnusedMember.Local
        static object CreateGetterDelegateHelper<TTarget, TProperty>(MethodInfo method) where TTarget : class
        {
            var func = (Func<TTarget, TProperty>)Delegate.CreateDelegate(typeof(Func<TTarget, TProperty>), method);
            Func<TTarget, object> ret = target => ConvertTo<TProperty>.From(func(target));
            return ret;
        }

    }
}