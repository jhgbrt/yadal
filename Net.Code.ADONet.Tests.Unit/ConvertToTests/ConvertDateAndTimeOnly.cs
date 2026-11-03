using System;
using System.Data;
using System.Linq;
using Xunit;

namespace Net.Code.ADONet.Tests.Unit.ConvertToTests
{
    // Npgsql 10 returns DateOnly for 'date' and TimeOnly for 'time' columns from IDataRecord.GetValue;
    // other providers (SqlClient, SQLite, ...) return DateTime / TimeSpan. DateOnly and TimeOnly do not
    // implement IConvertible, so Convert.ChangeType cannot convert between them and the classic types.
    public class ConvertDateAndTimeOnly
    {
        static readonly DateOnly Date = new(2000, 1, 2);
        static readonly TimeOnly Time = new(13, 14, 15);

        [Fact]
        public void DateOnly_ToDateTime() => Assert.Equal(new DateTime(2000, 1, 2), ConvertTo<DateTime>.From(Date));

        [Fact]
        public void DateOnly_ToNullableDateTime() => Assert.Equal(new DateTime(2000, 1, 2), ConvertTo<DateTime?>.From(Date));

        [Fact]
        public void DateTime_ToDateOnly() => Assert.Equal(Date, ConvertTo<DateOnly>.From(new DateTime(2000, 1, 2)));

        [Fact]
        public void DateTime_ToNullableDateOnly() => Assert.Equal(Date, ConvertTo<DateOnly?>.From(new DateTime(2000, 1, 2)));

        [Fact]
        public void DateOnly_ToDateOnly() => Assert.Equal(Date, ConvertTo<DateOnly>.From(Date));

        [Fact]
        public void DbNull_ToNullableDateOnly() => Assert.Null(ConvertTo<DateOnly?>.From(DBNull.Value));

        [Fact]
        public void TimeOnly_ToTimeSpan() => Assert.Equal(new TimeSpan(13, 14, 15), ConvertTo<TimeSpan>.From(Time));

        [Fact]
        public void TimeOnly_ToNullableTimeSpan() => Assert.Equal(new TimeSpan(13, 14, 15), ConvertTo<TimeSpan?>.From(Time));

        [Fact]
        public void TimeSpan_ToTimeOnly() => Assert.Equal(Time, ConvertTo<TimeOnly>.From(new TimeSpan(13, 14, 15)));

        [Fact]
        public void TimeSpan_ToNullableTimeOnly() => Assert.Equal(Time, ConvertTo<TimeOnly?>.From(new TimeSpan(13, 14, 15)));

        [Fact]
        public void DateTimeOffset_ToDateTimeOffset()
        {
            // DateTimeOffset is not IConvertible either; a value of the exact target type must pass through.
            var value = new DateTimeOffset(2000, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));
            Assert.Equal(value, ConvertTo<DateTimeOffset>.From(value));
        }

        public class PersonWithDateTime
        {
            public int Id { get; set; }
            public DateTime BirthDate { get; set; }
            public DateTime? LastSeen { get; set; }
        }

        public record PersonRecordWithDateTime(int Id, DateTime BirthDate, DateTime? LastSeen);

        public class PersonWithDateOnly
        {
            public int Id { get; set; }
            public DateOnly BirthDate { get; set; }
            public DateOnly? LastSeen { get; set; }
        }

        static IDataReader NpgsqlLikeReader()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("BirthDate", typeof(DateOnly));
            table.Columns.Add("LastSeen", typeof(DateOnly));
            table.Rows.Add(1, Date, DBNull.Value);
            table.Rows.Add(2, Date, new DateOnly(2024, 5, 6));
            return table.CreateDataReader();
        }

        static IDataReader SqlServerLikeReader()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("BirthDate", typeof(DateTime));
            table.Columns.Add("LastSeen", typeof(DateTime));
            table.Rows.Add(1, new DateTime(2000, 1, 2), DBNull.Value);
            table.Rows.Add(2, new DateTime(2000, 1, 2), new DateTime(2024, 5, 6));
            return table.CreateDataReader();
        }

        static T[] Map<T>(IDataReader reader)
        {
            var map = reader.GetMapper<T>(DbConfig.Default);
            var result = new System.Collections.Generic.List<T>();
            while (reader.Read()) result.Add(map(reader));
            return result.ToArray();
        }

        [Fact]
        public void Class_WithDateTimeProperties_MapsFromDateOnlyColumns()
        {
            var people = Map<PersonWithDateTime>(NpgsqlLikeReader());
            Assert.Equal(new DateTime(2000, 1, 2), people[0].BirthDate);
            Assert.Null(people[0].LastSeen);
            Assert.Equal(new DateTime(2024, 5, 6), people[1].LastSeen);
        }

        [Fact]
        public void Record_WithDateTimeParameters_MapsFromDateOnlyColumns()
        {
            var people = Map<PersonRecordWithDateTime>(NpgsqlLikeReader());
            Assert.Equal(new PersonRecordWithDateTime(1, new DateTime(2000, 1, 2), null), people[0]);
            Assert.Equal(new PersonRecordWithDateTime(2, new DateTime(2000, 1, 2), new DateTime(2024, 5, 6)), people[1]);
        }

        [Fact]
        public void Class_WithDateOnlyProperties_MapsFromDateTimeColumns()
        {
            var people = Map<PersonWithDateOnly>(SqlServerLikeReader());
            Assert.Equal(Date, people[0].BirthDate);
            Assert.Null(people[0].LastSeen);
            Assert.Equal(new DateOnly(2024, 5, 6), people[1].LastSeen);
        }
    }
}
