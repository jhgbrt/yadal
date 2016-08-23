using System;
using System.Collections.Generic;
using System.Data;

namespace Net.Code.ADONet
{
	public static class DataRecordExtensions
	{
		internal class SetterMap<T>
		{
			public int FieldIndex { get; set; }
			public Action<T, object> Setter { get; set; }
		}

		internal class FieldMap
		{
			public int FieldIndex { get; set; }
			public string FieldName { get; set; }
		}

		internal static IEnumerable<FieldMap> GetFieldMap(this IDataRecord reader)
		{
			List<FieldMap> map = new List<FieldMap>();

			for (var i = 0; i < reader.FieldCount; i++)
			{
				map.Add(new FieldMap()
				{
					FieldIndex = i,
					FieldName = reader.GetName(i)
				});
			}

			return map;
		}

		internal static IEnumerable<SetterMap<T>> GetSetterMap<T>(this IDataRecord reader, DbConfig config)
		{
			List<SetterMap<T>> map = new List<SetterMap<T>>();

			var convention = config.MappingConvention;
			var setters = FastReflection.Instance.GetSettersForType<T>();
			for (var i = 0; i < reader.FieldCount; i++)
			{
				Action<T, object> setter;
				var columnName = convention.FromDb(reader.GetName(i));

				if (setters.TryGetValue(columnName, out setter))
				{
					map.Add(new SetterMap<T>()
					{
						FieldIndex = i,
						Setter = setter
					});
				}
			}

			return map;
		}

		internal static T MapTo<T>(this IDataRecord record, IEnumerable<SetterMap<T>> setterMap)
		{
			var result = Activator.CreateInstance<T>();

			foreach (var item in setterMap)
			{
				item.Setter(result, record.GetValue(item.FieldIndex));
			}

			return result;
		}

		/// <summary>
		/// Convert a datarecord into a dynamic object, so that properties can be simply accessed
		/// using standard C# syntax.
		/// </summary>
		/// <param name="rdr">the data record</param>
		/// <returns>A dynamic object with fields corresponding to the database columns</returns>
		internal static dynamic ToExpando(this IDataRecord rdr, IEnumerable<FieldMap> fieldMap)
		{
			var d = new Dictionary<string, object>();

			foreach (var item in fieldMap)
			{
				d.Add(item.FieldName, rdr[item.FieldIndex]);
			}

			return Dynamic.From(d);
		}

		/// <summary>
		/// Get a value from an IDataRecord by column name. This method supports all types,
		/// as long as the DbType is convertible to the CLR Type passed as a generic argument.
		/// Also handles conversion from DbNull to null, including nullable types.
		/// </summary>
		public static TResult Get<TResult>(this IDataRecord reader, string name) => reader.Get<TResult>(reader.GetOrdinal(name));

		/// <summary>
		/// Get a value from an IDataRecord by index. This method supports all types,
		/// as long as the DbType is convertible to the CLR Type passed as a generic argument.
		/// Also handles conversion from DbNull to null, including nullable types.
		/// </summary>
		public static TResult Get<TResult>(this IDataRecord reader, int c) => ConvertTo<TResult>.From(reader[c]);
	}
}