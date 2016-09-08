using System;
using System.Data;
using System.Runtime.CompilerServices;

namespace Net.Code.ADONet.Extensions.SqlClient
{
    class DataReaderDecorator : IDataReader
    {
        readonly IDataReader _decorated;

        public DataReaderDecorator(IDataReader decorated)
        {
            _decorated = decorated;
        }

        public void Dispose()
        {
            Log();
            _decorated.Dispose();
        }

        public string GetName(int i)
        {
            Log(i);
            return _decorated.GetName(i);
        }

        public string GetDataTypeName(int i)
        {
            Log(i);
            return _decorated.GetDataTypeName(i);
        }

        public Type GetFieldType(int i)
        {
            Log(i);
            return _decorated.GetFieldType(i);
        }

        public object GetValue(int i)
        {
            Log(i);
            return _decorated.GetValue(i);
        }

        public int GetValues(object[] values)
        {
            Log();
            return _decorated.GetValues(values);
        }

        public int GetOrdinal(string name)
        {
            Log((object)name);
            return _decorated.GetOrdinal(name);
        }

        public bool GetBoolean(int i)
        {
            Log(i);
            return _decorated.GetBoolean(i);
        }

        public byte GetByte(int i)
        {
            Log(i);
            return _decorated.GetByte(i);
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            Log();
            return _decorated.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
        }

        public char GetChar(int i)
        {
            Log(i);
            return _decorated.GetChar(i);
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            Log();
            return _decorated.GetChars(i, fieldoffset, buffer, bufferoffset, length);
        }

        public Guid GetGuid(int i)
        {
            Log(i);
            return _decorated.GetGuid(i);
        }

        public short GetInt16(int i)
        {
            Log(i);
            return _decorated.GetInt16(i);
        }

        public int GetInt32(int i)
        {
            Log(i);
            return _decorated.GetInt32(i);
        }

        public long GetInt64(int i)
        {
            Log(i);
            return _decorated.GetInt64(i);
        }

        public float GetFloat(int i)
        {
            Log(i);
            return _decorated.GetFloat(i);
        }

        public double GetDouble(int i)
        {
            Log(i);
            return _decorated.GetDouble(i);
        }

        public string GetString(int i)
        {
            Log(i);
            return _decorated.GetString(i);
        }

        private void Log([CallerMemberName] string name = null)
        {
            Console.WriteLine(name);
        }
        private void Log(object arg, [CallerMemberName] string name = null)
        {
            Console.WriteLine($"{name}({arg})");
        }

        public decimal GetDecimal(int i)
        {
            Log(i);
            return _decorated.GetDecimal(i);
        }

        public DateTime GetDateTime(int i)
        {
            Log(i);
            return _decorated.GetDateTime(i);
        }

        public IDataReader GetData(int i)
        {
            Log(i);
            return _decorated.GetData(i);
        }

        public bool IsDBNull(int i)
        {
            Log(i);
            return _decorated.IsDBNull(i);
        }

        public int FieldCount
        {
            get
            {
                Log();
                return _decorated.FieldCount;
            }
        }

        object IDataRecord.this[int i]
        {
            get
            {
                Log(i);
                return _decorated[i];
            }
        }

        object IDataRecord.this[string name]
        {
            get
            {
                Log((object)name);
                return _decorated[name];
            }
        }

        public void Close()
        {
            Log();

            _decorated.Close();
        }

        public DataTable GetSchemaTable()
        {
            Log();

            return _decorated.GetSchemaTable();
        }

        public bool NextResult()
        {
            Log();

            return _decorated.NextResult();
        }

        public bool Read()
        {
            Log();

            return _decorated.Read();
        }

        public int Depth
        {
            get
            {
                Log();
                return _decorated.Depth;
            }
        }

        public bool IsClosed
        {
            get
            {
                Log();
                return _decorated.IsClosed;
            }
        }

        public int RecordsAffected
        {
            get
            {
                Log();
                return _decorated.RecordsAffected;
            }
        }
    }
}