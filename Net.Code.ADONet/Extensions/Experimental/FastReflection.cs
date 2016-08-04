using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Net.Code.ADONet.Extensions.Experimental
{
	public class FastReflection<T>
	{
		private FastReflection()
		{
		}

		#region Field Members
		private Lazy<Dictionary<string, Action<T, object>>> _setters =
			new Lazy<Dictionary<string, Action<T, object>>>(GetSetDelegates, LazyThreadSafetyMode.ExecutionAndPublication);

		private Lazy<Dictionary<string, Func<T, object>>> _getters =
			new Lazy<Dictionary<string, Func<T, object>>>(GetGetDelegates, LazyThreadSafetyMode.ExecutionAndPublication);
		#endregion

		#region Public Members
		public readonly static FastReflection<T> Instance = new FastReflection<T>();

		public IReadOnlyDictionary<string, Action<T, object>> GetSetters()
		{
			return _setters.Value;
		}

		public IReadOnlyDictionary<string, Func<T, object>> GetGetters()
		{
			return _getters.Value;
		}
		#endregion

		#region Private Members
		private static Dictionary<string, Action<T, object>> GetSetDelegates()
		{
			var typeInfo = typeof(T);

			if (typeInfo.IsClass && !typeInfo.IsAbstract && !typeInfo.IsInterface)
			{
				var memberInfo = typeInfo
					.GetProperties()
					.Where(x => x.CanWrite && x.SetMethod != null)
					.Select(x => (MemberInfo)x)
					.ToList();

				memberInfo.AddRange(typeInfo.GetFields().Where(x => x.IsPublic).Select(x => (MemberInfo)x));

				return memberInfo.ToDictionary(x => x.Name, GetSetDelegate);
			}

			return new Dictionary<string, Action<T, object>>();
		}

		private static Action<T, object> GetSetDelegate(MemberInfo p)
		{
			Type targetType = (p is PropertyInfo) ?
				((PropertyInfo)p).PropertyType :
				((FieldInfo)p).FieldType;

			var genericHelper = typeof(FastReflection<T>).GetMethod(nameof(CreateSetterDelegateHelper), BindingFlags.Static | BindingFlags.NonPublic);
			var constructedHelper = genericHelper.MakeGenericMethod(targetType);
			return (Action<T, object>)constructedHelper.Invoke(null, new object[] { p.Name });
		}

		private static Action<T, object> CreateSetterDelegateHelper<P>(string memberName)
		{
			var backingField = BackingFields<T>.Instance.ForMember(memberName);
			var func = ConvertTo<P>.From;
			var instance = Expression.Parameter(typeof(T), "i");
			var input = Expression.Parameter(typeof(object), "input");
			var instanceProp = backingField != null ?
				Expression.Field(instance, backingField) :
				Expression.PropertyOrField(instance, memberName);
			var convertCall = Expression.Call(null, func.Method, input);
			var assign = Expression.Assign(instanceProp, convertCall);
			return Expression.Lambda<Action<T, object>>(assign, instance, input).Compile();
		}

		private static Dictionary<string, Func<T, object>> GetGetDelegates()
		{
			var typeInfo = typeof(T);

			if (typeInfo.IsClass && !typeInfo.IsAbstract && !typeInfo.IsInterface)
			{
				var memberInfo = typeInfo
					.GetProperties()
					.Where(x => x.CanRead && x.GetMethod != null)
					.Select(x => (MemberInfo)x)
					.ToList();

				memberInfo.AddRange(typeInfo.GetFields().Where(x => x.IsPublic).Select(x => (MemberInfo)x));

				return memberInfo.ToDictionary(x => x.Name, GetGetDelegate);
			}

			return new Dictionary<string, Func<T, object>>();
		}

		private static Func<T, object> GetGetDelegate(MemberInfo p)
		{
			Type targetType = (p is PropertyInfo) ?
				((PropertyInfo)p).PropertyType :
				((FieldInfo)p).FieldType;

			var genericHelper = typeof(FastReflection<T>).GetMethod(nameof(CreateGetterDelegateHelper), BindingFlags.Static | BindingFlags.NonPublic);
			var constructedHelper = genericHelper.MakeGenericMethod(targetType);
			return (Func<T, object>)constructedHelper.Invoke(null, new object[] { p.Name });
		}

		private static Func<T, object> CreateGetterDelegateHelper<P>(string memberName)
		{
			var backingField = BackingFields<T>.Instance.ForMember(memberName);
			var instance = Expression.Parameter(typeof(T), "i");
			var instanceProp = backingField != null ?
				Expression.Field(instance, backingField) :
				Expression.PropertyOrField(instance, memberName);
			var convert = Expression.Convert(instanceProp, typeof(object));
			return Expression.Lambda<Func<T, object>>(convert, instance).Compile();
		}
		#endregion
	}

	internal class BackingFields<T>
	{
		private BackingFields()
		{
		}

		#region Field Members
		private Lazy<Dictionary<string, FieldInfo>> _backingFields =
			new Lazy<Dictionary<string, FieldInfo>>(GetBackingFields, LazyThreadSafetyMode.ExecutionAndPublication);
		#endregion

		#region Public Members
		public readonly static BackingFields<T> Instance = new BackingFields<T>();

		public FieldInfo ForMember(string name)
		{
			var key = string.Format("<{0}>k__BackingField", name);
			FieldInfo info;
			_backingFields.Value.TryGetValue(key, out info);
			return info;
		}
		#endregion

		#region Private Members
		private static Dictionary<string, FieldInfo> GetBackingFields()
		{
			return GetBackingFieldsFor(typeof(T)).ToDictionary(x => x.Name, y => y);
		}

		private static List<FieldInfo> GetBackingFieldsFor(Type t)
		{
			if (t == null)
			{
				return new List<FieldInfo>();
			}

			var fields = t
				.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(x => x.Name.IndexOf(">k__BackingField", 0, StringComparison.OrdinalIgnoreCase) > 0)
				.ToList();

			fields.AddRange(GetBackingFieldsFor(t.BaseType));

			return fields
				.GroupBy(x => x.Name)
				.Select(x => x.First())
				.ToList();
		}
		#endregion
	}
}
