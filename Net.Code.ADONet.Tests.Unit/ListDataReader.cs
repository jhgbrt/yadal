using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace Net.Code.ADONet.Tests.Unit
{
    public static class ListDataReader
    {
        public static DbDataReader AsDataReader<T>(this IEnumerable<T> input)
        {
            return new ListDataReader<T>(input);
        }
        public static DbDataReader AsMultiDataReader<T>(this IEnumerable<IEnumerable<T>> input)
        {
            return new ListDataReader<T>(input);
        }
    }


    /// <summary>
    /// Adapter from IEnumerable[T] to IDataReader
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ListDataReader<T> : DbDataReader
    {
        private readonly IEnumerable<IEnumerable<T>> _lists;
        private IEnumerator<IEnumerable<T>> _listEnumerator;
        private IEnumerator<T> _enumerator;
        private bool _disposed;

        // ReSharper disable StaticFieldInGenericType
        private static readonly PropertyInfo[] Properties;
        private static readonly Lazy<IDictionary<string,int>> PropertyIndexesByName 
            = new Lazy<IDictionary<string, int>>(() => Properties.Select((p,i) => new {p,i}).ToDictionary(x=>x.p.Name, x => x.i));
        // ReSharper restore StaticFieldInGenericType

        static ListDataReader()
        {
            var propertyInfos = from property in typeof(T).GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public)
                                where property.PropertyType.IsPrimitive || property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(string)
                                select property;

            Properties = propertyInfos.ToArray();
        }

        public ListDataReader(IEnumerable<T> list)
            : this(new[] { list })
        {
        }

        public ListDataReader(IEnumerable<IEnumerable<T>> listOfLists)
        {
            _lists = listOfLists;
        }

        public override string GetName(int i)
        {
            return Properties[i].Name;
        }

        public override string GetDataTypeName(int i)
        {
            return GetFieldType(i).Name;
        }

        public override IEnumerator GetEnumerator()
        {
            return _enumerator;
        }

        public override Type GetFieldType(int i)
        {
            return Properties[i].PropertyType;
        }

        public override object GetValue(int i)
        {
            return DBNullHelper.ToDb(Properties[i].GetValue(_enumerator.Current, null));
        }

        public override int GetValues(object[] values)
        {
            var length = Math.Min(values.Length, Properties.Length);
            for (int i = 0; i < length; i++)
            {
                values[i] = GetValue(i);
            }
            return length;
        }

        public override int GetOrdinal(string name)
        {
            return PropertyIndexesByName.Value[name];
        }

        public override bool GetBoolean(int i)
        {
            return (bool) GetValue(i);
        }

        public override byte GetByte(int i)
        {
            throw new NotSupportedException();
        }

        public override long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotSupportedException();
        }

        public override char GetChar(int i)
        {
            throw new NotSupportedException();
        }

        public override long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotSupportedException();
        }

        public override Guid GetGuid(int i)
        {
            throw new NotSupportedException();
        }

        public override short GetInt16(int i)
        {
            throw new NotSupportedException();
        }

        public override int GetInt32(int i)
        {
            throw new NotSupportedException();
        }

        public override long GetInt64(int i)
        {
            throw new NotSupportedException();
        }

        public override float GetFloat(int i)
        {
            throw new NotSupportedException();
        }

        public override double GetDouble(int i)
        {
            throw new NotSupportedException();
        }

        public override string GetString(int i)
        {
            throw new NotSupportedException();
        }

        public override decimal GetDecimal(int i)
        {
            throw new NotSupportedException();
        }

        public override DateTime GetDateTime(int i)
        {
            throw new NotSupportedException();
        }

        public override bool IsDBNull(int i)
        {
            return DBNull.Value.Equals(GetValue(i));
        }

        public override int FieldCount
        {
            get { return Properties.Length; }
        }


        public override bool HasRows
        {
            get { return _lists.FirstOrDefault()?.Any() ?? false; }
        }

        public override object this[int i]
        {
            get { return GetValue(i); }
        }

        public override object this[string name]
        {
            get { return GetValue(GetOrdinal(name)); }
        }


        public override DataTable GetSchemaTable()
        {
            var q = from p in Properties
                let nullable = p.PropertyType.IsNullableType()
                let colType = nullable ? p.PropertyType.GetGenericArguments()[0] : p.PropertyType
                select new DataColumn(p.Name, colType) { AllowDBNull = nullable };
            var dt = new DataTable();
            dt.Columns.AddRange(q.ToArray());
            dt.Columns.Add(new DataColumn("ColumnOrdinal", typeof (int)));
            return dt;
        }

        public override bool NextResult()
        {
            if (!_listEnumerator.MoveNext()) return false;
            _enumerator.Dispose();
            _enumerator = _listEnumerator.Current.GetEnumerator();
            return true; 
        }

        public override bool Read()
        {
            if (_disposed) throw new ObjectDisposedException("");
            return _enumerator.MoveNext();
        }

        public override int Depth
        {
            get { return 0; }
        }

        public override bool IsClosed
        {
            get { return _disposed; }
        }

        public override int RecordsAffected
        {
            get 
            { 
                return 0; // nothing deleted, updated or inserted
            }
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
        }
    }


}
