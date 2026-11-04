using System;
using System.Data;
using Xunit;

namespace Net.Code.ADONet.Tests.Unit.ConvertToTests
{
    public class DateAndTimeOnlyExtensionsTests
    {
        static readonly DateOnly Date = new(2000, 1, 2);
        static readonly TimeOnly Time = new(13, 14, 15);

        static DataTableReader Reader(Type dateType, object date, Type timeType, object time)
        {
            var table = new DataTable();
            table.Columns.Add("d", dateType);
            table.Columns.Add("t", timeType);
            table.Rows.Add(date, time);
            var reader = table.CreateDataReader();
            Assert.True(reader.Read());
            return reader;
        }

        static DataTableReader NpgsqlLike() => Reader(typeof(DateOnly), Date, typeof(TimeOnly), Time);
        static DataTableReader SqlServerLike() => Reader(typeof(DateTime), new DateTime(2000, 1, 2), typeof(TimeSpan), new TimeSpan(13, 14, 15));

        [Fact]
        public void GetDateOnly_FromDateOnlyColumn() => Assert.Equal(Date, NpgsqlLike().GetDateOnly(0));

        [Fact]
        public void GetDateOnly_FromDateTimeColumn() => Assert.Equal(Date, SqlServerLike().GetDateOnly(0));

        [Fact]
        public void GetTimeOnly_FromTimeOnlyColumn() => Assert.Equal(Time, NpgsqlLike().GetTimeOnly(1));

        [Fact]
        public void GetTimeOnly_FromTimeSpanColumn() => Assert.Equal(Time, SqlServerLike().GetTimeOnly(1));

        // a record that is not a DbDataReader goes through IDataRecord.GetValue
        [Fact]
        public void PlainDataRecord_FromDateOnlyAndTimeOnlyColumns()
        {
            IDataRecord record = new PlainRecord(NpgsqlLike());
            Assert.Equal(Date, record.GetDateOnly(0));
            Assert.Equal(Time, record.GetTimeOnly(1));
        }

        [Fact]
        public void PlainDataRecord_FromDateTimeAndTimeSpanColumns()
        {
            IDataRecord record = new PlainRecord(SqlServerLike());
            Assert.Equal(Date, record.GetDateOnly(0));
            Assert.Equal(Time, record.GetTimeOnly(1));
        }

        sealed class PlainRecord(IDataRecord inner) : IDataRecord
        {
            public object this[int i] => inner[i];
            public object this[string name] => inner[name];
            public int FieldCount => inner.FieldCount;
            public bool GetBoolean(int i) => inner.GetBoolean(i);
            public byte GetByte(int i) => inner.GetByte(i);
            public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => inner.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
            public char GetChar(int i) => inner.GetChar(i);
            public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => inner.GetChars(i, fieldoffset, buffer, bufferoffset, length);
            public IDataReader GetData(int i) => inner.GetData(i);
            public string GetDataTypeName(int i) => inner.GetDataTypeName(i);
            public DateTime GetDateTime(int i) => inner.GetDateTime(i);
            public decimal GetDecimal(int i) => inner.GetDecimal(i);
            public double GetDouble(int i) => inner.GetDouble(i);
            public Type GetFieldType(int i) => inner.GetFieldType(i);
            public float GetFloat(int i) => inner.GetFloat(i);
            public Guid GetGuid(int i) => inner.GetGuid(i);
            public short GetInt16(int i) => inner.GetInt16(i);
            public int GetInt32(int i) => inner.GetInt32(i);
            public long GetInt64(int i) => inner.GetInt64(i);
            public string GetName(int i) => inner.GetName(i);
            public int GetOrdinal(string name) => inner.GetOrdinal(name);
            public string GetString(int i) => inner.GetString(i);
            public object GetValue(int i) => inner.GetValue(i);
            public int GetValues(object[] values) => inner.GetValues(values);
            public bool IsDBNull(int i) => inner.IsDBNull(i);
        }
    }
}
