using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace Net.Code.ADONet
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Adapter from IEnumerable[T] to IDataReader
        /// </summary>
        public static IDataReader AsDataReader<T>(this IEnumerable<T> input) => new EnumerableDataReaderImpl<T>(input);

        private class EnumerableDataReaderImpl<T> : DbDataReader
        {
            private readonly IEnumerable<T> _list;
            private IEnumerator<T> _enumerator;
            private bool _disposed;

            // ReSharper disable StaticFieldInGenericType
            private static readonly PropertyInfo[] Properties;
            private static readonly IReadOnlyDictionary<string, int> PropertyIndexesByName;
            private static readonly IReadOnlyDictionary<string, Func<T, object>> Getters;
            // ReSharper restore StaticFieldInGenericType

            static EnumerableDataReaderImpl()
            {
                var propertyInfos = typeof (T).GetProperties();
                Properties = propertyInfos.ToArray();
                Getters = FastReflection.Instance.GetGettersForType<T>();
                PropertyIndexesByName = Properties.Select((p, i) => new {p, i}).ToDictionary(x => x.p.Name, x => x.i);
            }


            public EnumerableDataReaderImpl(IEnumerable<T> list)
            {
                _list = list;
                _enumerator = _list.GetEnumerator();
            }
            
            public override string GetName(int i) => Properties[i].Name;

            public override string GetDataTypeName(int i) => Properties[i].PropertyType.Name;

            public override IEnumerator GetEnumerator() => _enumerator;

            public override Type GetFieldType(int i) => Properties[i].PropertyType;

            public override object GetValue(int i)
                => DBNullHelper.ToDb(Getters[Properties[i].Name](_enumerator.Current));

            public override int GetValues(object[] values)
            {
                var length = Math.Min(values.Length, Properties.Length);
                for (int i = 0; i < length; i++)
                {
                    values[i] = GetValue(i);
                }
                return length;
            }

            public override int GetOrdinal(string name) => PropertyIndexesByName[name];

            public override bool GetBoolean(int i) => this.Get<bool>(i);
            public override byte GetByte(int i) => this.Get<byte>(i);
            public override long GetBytes(int i, long dataOffset, byte[] buffer, int bufferoffset, int length) 
                => Get(i, dataOffset, buffer, bufferoffset, length);
            public override char GetChar(int i) => this.Get<char>(i);
            public override long GetChars(int i, long dataOffset, char[] buffer, int bufferoffset, int length) 
                => Get(i, dataOffset, buffer, bufferoffset, length);
            public override Guid GetGuid(int i) => this.Get<Guid>(i);
            public override short GetInt16(int i) => this.Get<short>(i);
            public override int GetInt32(int i) => this.Get<int>(i);
            public override long GetInt64(int i) => this.Get<long>(i);
            public override float GetFloat(int i) => this.Get<float>(i);
            public override double GetDouble(int i) => this.Get<double>(i);
            public override string GetString(int i) => this.Get<string>(i);
            public override decimal GetDecimal(int i) => this.Get<decimal>(i);
            public override DateTime GetDateTime(int i) => this.Get<DateTime>(i);

            long Get<TElem>(int i, long dataOffset, TElem[] buffer, int bufferoffset, int length)
            {
                var data = this.Get<TElem[]>(i);
                var maxLength = Math.Min((long) buffer.Length - bufferoffset, length);
                maxLength = Math.Min(data.Length - dataOffset, maxLength);
                Array.Copy(data, (int)dataOffset, buffer, bufferoffset, length);
                return maxLength;
            }

            public override bool IsDBNull(int i) => DBNull.Value.Equals(GetValue(i));

            public override int FieldCount => Properties.Length;

            public override bool HasRows => _list.Any();

            public override object this[int i] => GetValue(i);

            public override object this[string name] => GetValue(GetOrdinal(name));

            public override void Close() => Dispose();
            public override DataTable GetSchemaTable()
            {
                var q = from x in Properties.Select((p, i) => new {p, i})
                    let p = x.p
                    let nullable = p.PropertyType.IsNullableType()
                    let dataType = p.PropertyType.GetUnderlyingType()
                    select new
                    {
                        ColumnName = p.Name,
                        ColumnOrdinal = x.i,
                        ColumnSize = int.MaxValue, // must be filled in and large enough for ToDataTable
                        AllowDBNull = nullable || !p.PropertyType.IsValueType, // assumes string nullable
                        DataType = dataType,
                    };

                var dt = q.ToDataTable();
                return dt;
            }

            public override bool NextResult()
            {
                _enumerator?.Dispose();
                _enumerator = null;
                return false;
            }

            public override bool Read() => _enumerator.MoveNext();

            public override int Depth => 0;

            public override bool IsClosed => _disposed;

            public override int RecordsAffected => 0;

            protected override void Dispose(bool disposing) => _disposed = true;
        }
    }
}