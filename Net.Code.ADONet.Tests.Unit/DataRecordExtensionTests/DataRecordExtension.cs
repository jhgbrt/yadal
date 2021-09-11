using System;
using System.Data;
using NSubstitute;
using Xunit;

namespace Net.Code.ADONet.Tests.Unit.DataRecordExtensionTests
{
    public static class DataReaderHelper
    {
        public static IDataReader ToDataReader(this (string name, object value)[] values)
        {
            var reader = Substitute.For<IDataReader>();
            reader.FieldCount.Returns(values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                reader.GetOrdinal(values[i].name).Returns(i);
                reader.GetName(i).Returns(values[i].name);
                reader.GetValue(i).Returns(values[i].value);
            }
            return reader;
        }
    }

    public class DataRecordExtension
    {
        public class MyEntity
        {
            public string MyProperty { get; set; }
            public int? MyNullableInt1 { get; set; }
            public int? MyNullableInt2 { get; set; }
            public int MyInt1 { get; set; }
        }

        public class SomeEntity
        {
            public string MyProperty { get; set; }
            public int? MyNullableInt1 { get; set; }
            public int? MyNullableInt2 { get; set; }
            public int MyInt1 { get; set; }
        }

        public record SomeRecord(int Id, string MyString, int? MyNullableInt, double MyDouble);
        public record struct SomeRecordStruct(int Id, string MyString, int? MyNullableInt, double MyDouble);

        [Fact]
        public void MapTo_WhenCalled_MissingProperty_IsIgnored()
        {
            var values = new[]
            {
                ("UnmappedProperty",  (object)"SomeValue"),
            };

            var record = values.ToDataReader();
            var config = new DbConfig(c => { }, MappingConvention.Default);
            var map = record.GetMapper<SomeEntity>(config);

            var entity = map(record);

            Assert.Null(entity.MyProperty);
            Assert.Null(entity.MyNullableInt1);
            Assert.Null(entity.MyNullableInt2);
            Assert.Equal(default(int), entity.MyInt1);
        }

        [Fact]
        public void MapTo_WhenCalled_EntityIsMapped()
        {
            var values = new[]
            {
                ("MyProperty",  "SomeValue"),
                ("MyNullableInt1", DBNull.Value),
                ("MyNullableInt2", 1),
                ("MyInt1", (object) 2)
            };

            var reader = values.ToDataReader();

            var config = new DbConfig(c => { }, MappingConvention.Default);
            var map = reader.GetMapper<MyEntity>(config);
            var entity = map(reader);

            Assert.Equal("SomeValue", entity.MyProperty);
            Assert.Null(entity.MyNullableInt1);
            Assert.Equal(1, entity.MyNullableInt2);
            Assert.Equal(2, entity.MyInt1);
        }

        [Fact]
        public void MapTo_WhenCalled_RecordClassIsMapped()
        {
            var values = new[]
            {
                (nameof(SomeRecord.Id),  (object)1),
                (nameof(SomeRecord.MyString), "Some String"),
                (nameof(SomeRecord.MyNullableInt), DBNull.Value),
                (nameof(SomeRecord.MyDouble), (object) 2.0)
            };

            var reader = values.ToDataReader();

            var config = new DbConfig(c => { }, MappingConvention.Default);
            var map = reader.GetMapper<SomeRecord>(config);
            var entity = map(reader);

            Assert.Equal(1, entity.Id);
            Assert.Equal("Some String", entity.MyString);
            Assert.Null(entity.MyNullableInt);
            Assert.Equal(2.0, entity.MyDouble);
        }

        [Fact]
        public void MapTo_WhenCalled_RecordStructIsMapped()
        {
            var values = new[]
            {
                (nameof(SomeRecord.Id),  (object)1),
                (nameof(SomeRecord.MyString), "Some String"),
                (nameof(SomeRecord.MyNullableInt), DBNull.Value),
                (nameof(SomeRecord.MyDouble), (object) 2.0)
            };

            var reader = values.ToDataReader();

            var config = new DbConfig(c => { }, MappingConvention.Default);
            var map = reader.GetMapper<SomeRecordStruct>(config);
            var entity = map(reader);

            Assert.Equal(1, entity.Id);
            Assert.Equal("Some String", entity.MyString);
            Assert.Null(entity.MyNullableInt);
            Assert.Equal(2.0, entity.MyDouble);
        }


        [Fact]
        public void GivenDataReaderMock_WhenGetByNameReturnsDbNull_ResultIsNull()
        {
            var reader = Substitute.For<IDataReader>();
            reader.GetOrdinal("Id").Returns(0);
            reader[0].Returns(DBNull.Value);
            var result = reader.Get<int?>("Id");
            Assert.Null(result);
        }
        [Fact]
        public void GivenDataReaderMock_WhenGetByIndexReturnsDbNull_ResultIsNull()
        {
            var reader = Substitute.For<IDataReader>();
            reader[0].Returns(DBNull.Value);
            var result = reader.Get<int?>(0);
            Assert.Null(result);
        }
        [Fact]
        public void GivenDataReaderMock_WhenGetByNameReturnsValue_ResultIsValue()
        {
            var reader = Substitute.For<IDataReader>();
            reader.GetOrdinal("Id").Returns(0);
            reader[0].Returns(1);
            var result = reader.Get<int?>("Id");
            Assert.Equal(1, result);
        }
        [Fact]
        public void GivenDataReaderMock_WhenGetByIndexReturnsValue_ResultIsValue()
        {
            var reader = Substitute.For<IDataReader>();
            reader[0].Returns(1);
            var result = reader.Get<int?>(0);
            Assert.Equal(1, result);
        }
    }
}
