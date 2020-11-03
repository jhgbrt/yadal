using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Net.Code.ADONet.Tests.Unit
{
    class Utils
    {
        public static int SizeOf<T>(T obj)
        {
            return SizeOfCache<T>.SizeOf;
        }
        public static int SizeOf<T>()
        {
            return SizeOfCache<T>.SizeOf;
        }
        public static int SizeOf(Type type)
        {
            Type t = typeof(SizeOfCache<>).MakeGenericType(type);
            return (int)t.GetField("SizeOf").GetValue(null);
        }

        private static class SizeOfCache<T>
        {
            public static readonly int SizeOf;

            static SizeOfCache()
            {
                var dm = new DynamicMethod("func", typeof(int),
                                           Type.EmptyTypes, typeof(Utils));

                ILGenerator il = dm.GetILGenerator();
                il.Emit(OpCodes.Sizeof, typeof(T));
                il.Emit(OpCodes.Ret);

                var func = (Func<int>)dm.CreateDelegate(typeof(Func<int>));
                SizeOf = func();
            }
        }
    }

    public static class ListDataReader
    {
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
        private static readonly Lazy<IDictionary<string, int>> PropertyIndexesByName
            = new Lazy<IDictionary<string, int>>(() => Properties.Select((p, i) => new { p, i }).ToDictionary(x => x.p.Name, x => x.i));
        // ReSharper restore StaticFieldInGenericType

        static ListDataReader()
        {
            var propertyInfos = typeof (T).GetProperties();

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

        public override string GetName(int i) => Properties[i].Name;

        public override string GetDataTypeName(int i) => GetFieldType(i).Name;

        public override IEnumerator GetEnumerator() => _enumerator;

        public override Type GetFieldType(int i) => Properties[i].PropertyType;

        public override object GetValue(int i) => DBNullHelper.ToDb(Properties[i].GetValue(_enumerator.Current, null));

        public override int GetValues(object[] values)
        {
            var length = Math.Min(values.Length, Properties.Length);
            for (int i = 0; i < length; i++)
            {
                values[i] = GetValue(i);
            }
            return length;
        }

        public override int GetOrdinal(string name) => PropertyIndexesByName.Value[name];

        public override bool GetBoolean(int i) => this.Get<bool>(i);

        public override byte GetByte(int i) => this.Get<byte>(i);

        public override long GetBytes(int i, long dataOffset, byte[] buffer, int bufferoffset, int length)
        {
            return Get(i, dataOffset, buffer, bufferoffset, length);
        }

        long Get<TElem>(int i, long dataOffset, TElem[] buffer, int bufferoffset, int length)
        {
            var data = this.Get<TElem[]>(i);
            var maxLength = Math.Min((long)buffer.Length - bufferoffset, length);
            maxLength = Math.Min(data.Length - dataOffset, maxLength);
            Array.Copy(data, dataOffset, buffer, bufferoffset, length);
            return maxLength;
        }

        public override char GetChar(int i) => this.Get<char>(i);

        public override long GetChars(int i, long dataOffset, char[] buffer, int bufferoffset, int length)
        {
            return Get(i, dataOffset, buffer, bufferoffset, length);
        }

        public override Guid GetGuid(int i) => this.Get<Guid>(i);

        public override short GetInt16(int i) => this.Get<short>(i);

        public override int GetInt32(int i) => this.Get<int>(i);

        public override long GetInt64(int i) => this.Get<long>(i);

        public override float GetFloat(int i) => this.Get<float>(i);

        public override double GetDouble(int i) => this.Get<double>(i);

        public override string GetString(int i) => this.Get<string>(i);

        public override decimal GetDecimal(int i) => this.Get<decimal>(i);

        public override DateTime GetDateTime(int i) => this.Get<DateTime>(i);

        public override bool IsDBNull(int i) => DBNull.Value.Equals(GetValue(i));

        public override int FieldCount => Properties.Length;

        public override bool HasRows => _lists.FirstOrDefault()?.Any() ?? false;

        public override object this[int i] => GetValue(i);

        public override object this[string name] => GetValue(GetOrdinal(name));

        public override DataTable GetSchemaTable()
        {
            Console.WriteLine($"GetSchemaTable()");
            var q = from x in Properties.Select((p, i) => new { p, i })
                    let p = x.p
                    let nullable = p.PropertyType.IsNullableType()
                    let dataType = nullable ? p.PropertyType.GetGenericArguments()[0] : p.PropertyType
                    let size = dataType.IsValueType ? Utils.SizeOf(dataType) : 4000
                    select new
                    {
                        ColumnName = p.Name,
                        AllowDBNull = nullable || !p.PropertyType.IsValueType,
                        ColumnOrdinal = x.i,
                        DataType = dataType,
                        ColumnSize = size
                    };

            var dt = q.ToDataTable();
            return dt;
        }

        public override bool NextResult()
        {
            Console.WriteLine("NextResult()");
            if (_listEnumerator == null) _listEnumerator = _lists.GetEnumerator();
            if (!_listEnumerator.MoveNext()) return false;
            _enumerator?.Dispose();
            _enumerator = _listEnumerator.Current.GetEnumerator();
            return true;
        }

        public override bool Read()
        {
            Console.WriteLine($"Read()");
            if (_disposed) throw new ObjectDisposedException("");
            if (_enumerator == null && !NextResult()) return false;
            if (_enumerator == null) return false;
            return _enumerator.MoveNext();
        }

        public override int Depth => 0;

        public override bool IsClosed => _disposed;

        public override int RecordsAffected => 0;

        protected override void Dispose(bool disposing) => _disposed = true;
    }

}
