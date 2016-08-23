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
		private Lazy<Dictionary<string, ISetter<T>>> _setters =
			new Lazy<Dictionary<string, ISetter<T>>>(GetSetDelegates, LazyThreadSafetyMode.ExecutionAndPublication);

		private Lazy<Dictionary<string, IGetter<T>>> _getters =
			new Lazy<Dictionary<string, IGetter<T>>>(GetGetDelegates, LazyThreadSafetyMode.ExecutionAndPublication);
		#endregion

		#region Public Members
		public readonly static FastReflection<T> Instance = new FastReflection<T>();

		public IReadOnlyDictionary<string, ISetter<T>> GetSetters()
		{
			return _setters.Value;
		}

		public IReadOnlyDictionary<string, IGetter<T>> GetGetters()
		{
			return _getters.Value;
		}

		public IGetter<T> GetGetter(string name)
		{
			IGetter<T> getter = null;

			if (_getters.Value.TryGetValue(name, out getter))
			{
				return getter;
			}

			return null;
		}

		public IGetter<T> GetGetter(Expression<Func<T, object>> expression)
		{
			return GetGetter(ExtractPropertyName(expression));
		}

		public ISetter<T> GetSetter(string name)
		{
			ISetter<T> setter = null;

			if (_setters.Value.TryGetValue(name, out setter))
			{
				return setter;
			}

			return null;
		}

		public ISetter<T> GetSetter(Expression<Func<T, object>> expression)
		{
			return GetSetter(ExtractPropertyName(expression));
		}
		#endregion

		#region Private Members
		private static Dictionary<string, ISetter<T>> GetSetDelegates()
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

			return new Dictionary<string, ISetter<T>>();
		}

		private static ISetter<T> GetSetDelegate(MemberInfo p)
		{
			Type targetType = (p is PropertyInfo) ?
				((PropertyInfo)p).PropertyType :
				((FieldInfo)p).FieldType;

			var genericHelper = typeof(FastReflection<T>).GetMethod("CreateSetterDelegateHelper", BindingFlags.Static | BindingFlags.NonPublic);
			var constructedHelper = genericHelper.MakeGenericMethod(targetType);
			return (ISetter<T>)constructedHelper.Invoke(null, new object[] { p.Name });
		}

		private static ISetter<T> CreateSetterDelegateHelper<P>(string memberName)
		{
			var backingField = BackingFields<T>.Instance.ForMember(memberName);
			var func = ConvertTo<P>.From;
			var instance = Expression.Parameter(typeof(T), "i");
			var input = Expression.Parameter(typeof(object), "input");
			var instanceProp = backingField != null ?
				Expression.Field(instance, backingField) :
				Expression.PropertyOrField(instance, memberName);
			var convertCall = Expression.Call(null, func.Method, input);
			var safeAssign = Expression.Assign(instanceProp, convertCall);

			return new Setter
			{
				Name = memberName,
				Set = Expression.Lambda<Action<T, object>>(safeAssign, instance, input).Compile()
			};
		}

		private static Dictionary<string, IGetter<T>> GetGetDelegates()
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

			return new Dictionary<string, IGetter<T>>();
		}

		private static IGetter<T> GetGetDelegate(MemberInfo p)
		{
			Type targetType = (p is PropertyInfo) ?
				((PropertyInfo)p).PropertyType :
				((FieldInfo)p).FieldType;

			var genericHelper = typeof(FastReflection<T>).GetMethod("CreateGetterDelegateHelper", BindingFlags.Static | BindingFlags.NonPublic);
			var constructedHelper = genericHelper.MakeGenericMethod(targetType);
			return (IGetter<T>)constructedHelper.Invoke(null, new object[] { p.Name });
		}

		private static IGetter<T> CreateGetterDelegateHelper<P>(string memberName)
		{
			var backingField = BackingFields<T>.Instance.ForMember(memberName);
			var instance = Expression.Parameter(typeof(T), "i");
			var instanceProp = backingField != null ?
				Expression.Field(instance, backingField) :
				Expression.PropertyOrField(instance, memberName);
			var convert = Expression.Convert(instanceProp, typeof(object));

			return new Getter
			{
				Name = memberName,
				Get = Expression.Lambda<Func<T, object>>(convert, instance).Compile()
			};
		}

		private static string ExtractPropertyName(Expression<Func<T, object>> propertyExpression)
		{
			if (propertyExpression != null)
			{
				var memberExpression = propertyExpression.Body as MemberExpression;

				if (memberExpression != null &&
					(memberExpression.Member is PropertyInfo || memberExpression.Member is FieldInfo))
				{
					return memberExpression.Member.Name;
				}
			}

			return string.Empty;
		}
		#endregion

		#region Helpers
		class Setter : ISetter<T>
		{
			public string Name { get; set; }
			public Action<T, object> Set { get; set; }
		}

		class Getter : IGetter<T>
		{
			public string Name { get; set; }
			public Func<T, object> Get { get; set; }
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

	public interface ISetter<T>
	{
		string Name { get; }
		Action<T, object> Set { get; }
	}

	public interface IGetter<T>
	{
		string Name { get; }
		Func<T, object> Get { get; }
	}
}