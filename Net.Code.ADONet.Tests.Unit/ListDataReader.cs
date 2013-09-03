using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Net.Code.ADONet.Tests.Unit
{
    public static class ListDataReader
    {
        public static IDataReader AsDataReader<T>(this IEnumerable<T> input)
        {
            return new ListDataReader<T>(input);
        }
        public static IDataReader AsMultiDataReader<T>(this IEnumerable<IEnumerable<T>> input)
        {
            return new ListDataReader<T>(input);
        }
    }



    /// <summary>
    /// Adapter from IEnumerable[T] to IDataReader
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ListDataReader<T> : IDataReader
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
            _listEnumerator = _lists.GetEnumerator();
            _listEnumerator.MoveNext();
            _enumerator = _listEnumerator.Current.GetEnumerator();
        }

        public void Dispose()
        {
            if (_disposed) throw new ObjectDisposedException("ListDataReader");
            _listEnumerator.Dispose();
            _enumerator.Dispose();
            _disposed = true;
        }

        public string GetName(int i)
        {
            return Properties[i].Name;
        }

        public string GetDataTypeName(int i)
        {
            return GetFieldType(i).Name;
        }

        public Type GetFieldType(int i)
        {
            return Properties[i].PropertyType;
        }

        public object GetValue(int i)
        {
            return DBNullHelper.ToDb(Properties[i].GetValue(_enumerator.Current, null));
        }

        public int GetValues(object[] values)
        {
            throw new NotSupportedException();
        }

        public int GetOrdinal(string name)
        {
            return PropertyIndexesByName.Value[name];
        }

        public bool GetBoolean(int i)
        {
            throw new NotSupportedException();
        }

        public byte GetByte(int i)
        {
            throw new NotSupportedException();
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotSupportedException();
        }

        public char GetChar(int i)
        {
            throw new NotSupportedException();
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotSupportedException();
        }

        public Guid GetGuid(int i)
        {
            throw new NotSupportedException();
        }

        public short GetInt16(int i)
        {
            throw new NotSupportedException();
        }

        public int GetInt32(int i)
        {
            throw new NotSupportedException();
        }

        public long GetInt64(int i)
        {
            throw new NotSupportedException();
        }

        public float GetFloat(int i)
        {
            throw new NotSupportedException();
        }

        public double GetDouble(int i)
        {
            throw new NotSupportedException();
        }

        public string GetString(int i)
        {
            throw new NotSupportedException();
        }

        public decimal GetDecimal(int i)
        {
            throw new NotSupportedException();
        }

        public DateTime GetDateTime(int i)
        {
            throw new NotSupportedException();
        }

        public IDataReader GetData(int i)
        {
            throw new NotSupportedException();
        }

        public bool IsDBNull(int i)
        {
            return DBNull.Value.Equals(GetValue(i));
        }

        public int FieldCount
        {
            get { return Properties.Length; }
        }

        object IDataRecord.this[int i]
        {
            get { return GetValue(i); }
        }

        object IDataRecord.this[string name]
        {
            get { return GetValue(GetOrdinal(name)); }
        }

        public void Close()
        {
            Dispose();
        }

        public DataTable GetSchemaTable()
        {
            throw new NotSupportedException();
        }

        public bool NextResult()
        {
            if (!_listEnumerator.MoveNext()) return false;
            _enumerator.Dispose();
            _enumerator = _listEnumerator.Current.GetEnumerator();
            return true; // only one list
        }

        public bool Read()
        {
            return _enumerator.MoveNext();
        }

        public int Depth
        {
            get { return 0; }
        }

        public bool IsClosed
        {
            get { return _disposed; }
        }

        public int RecordsAffected
        {
            get 
            { 
                return 0; // nothing deleted, updated or inserted
            }
        }
    }


}
