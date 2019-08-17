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

        [Fact]
        public void MapTo_WhenCalled_MissingProperty_IsIgnored()
        {
            var values = new[]
            {
                ("UnmappedProperty",  (object)"SomeValue"),
            };

            var record = values.ToDataReader();
            var config = new DbConfig(c => { }, MappingConvention.Default, string.Empty);
            var map = record.GetSetterMap<SomeEntity>(config);

            var entity = record.MapTo(map);

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

            var config = new DbConfig(c => { }, MappingConvention.Default, string.Empty);
            var map = reader.GetSetterMap<MyEntity>(config);
            var entity = reader.MapTo(map);
            
            Assert.Equal("SomeValue", entity.MyProperty);
            Assert.Null(entity.MyNullableInt1);
            Assert.Equal(1, entity.MyNullableInt2);
            Assert.Equal(2, entity.MyInt1);
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
