using System;
using System.Data;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Code.ADONet.Tests.Unit.DataRecordExtensionTests
{
   
    [TestClass]
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

        [TestMethod]
        public void MapTo_WhenCalled_MissingProperty_IsIgnored()
        {
            var values = new[]
            {
                new {Name = "UNMAPPED_PROPERTY", Value = (object) "SomeValue"},
            };

            var record = Substitute.For<IDataReader>();
            record.FieldCount.Returns(2);
            for (int i = 0; i < values.Length; i++)
            {
                record.GetName(i).Returns(values[i].Name);
                record.GetValue(i).Returns(values[i].Value);
            }

            var config = new DbConfig(c => { }, MappingConvention.OracleStyle, string.Empty);
            var map = record.GetSetterMap<SomeEntity>(config);

            var entity = record.MapTo(map);

            Assert.IsNull(entity.MyProperty);
            Assert.IsNull(entity.MyNullableInt1);
            Assert.IsNull(entity.MyNullableInt2);
            Assert.AreEqual(default(int), entity.MyInt1);
        }

        [TestMethod]
        public void MapTo_WhenCalled_EntityIsMapped()
        {
            var reader = Substitute.For<IDataReader>();

            var values = new[]
            {
                new {Name = "MY_PROPERTY", Value = (object) "SomeValue"},
                new {Name = "MY_NULLABLE_INT1", Value = (object) DBNull.Value},
                new {Name = "MY_NULLABLE_INT2", Value = (object) 1},
                new {Name = "MY_INT1", Value = (object) 2}
            };

            reader.FieldCount.Returns(values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                reader.GetName(i).Returns(values[i].Name);
                reader.GetValue(i).Returns(values[i].Value);
            }

            var config = new DbConfig(c => { }, MappingConvention.OracleStyle, string.Empty);
            var map = reader.GetSetterMap<MyEntity>(config);
            var entity = reader.MapTo(map);
            
            Assert.AreEqual("SomeValue", entity.MyProperty);
            Assert.IsNull(entity.MyNullableInt1);
            Assert.AreEqual(1, entity.MyNullableInt2);
            Assert.AreEqual(2, entity.MyInt1);
        }

       
        [TestMethod]
        public void GivenDataReaderMock_WhenGetByNameReturnsDbNull_ResultIsNull()
        {
            var reader = Substitute.For<IDataReader>();
            reader.GetOrdinal("Id").Returns(0);
            reader[0].Returns(DBNull.Value);
            var result = reader.Get<int?>("Id");
            Assert.IsNull(result);
        }
        [TestMethod]
        public void GivenDataReaderMock_WhenGetByIndexReturnsDbNull_ResultIsNull()
        {
            var reader = Substitute.For<IDataReader>();
            reader[0].Returns(DBNull.Value);
            var result = reader.Get<int?>(0);
            Assert.IsNull(result);
        }
        [TestMethod]
        public void GivenDataReaderMock_WhenGetByNameReturnsValue_ResultIsValue()
        {
            var reader = Substitute.For<IDataReader>();
            reader.GetOrdinal("Id").Returns(0);
            reader[0].Returns(1);
            var result = reader.Get<int?>("Id");
            Assert.AreEqual(1, result);
        }
        [TestMethod]
        public void GivenDataReaderMock_WhenGetByIndexReturnsValue_ResultIsValue()
        {
            var reader = Substitute.For<IDataReader>();
            reader[0].Returns(1);
            var result = reader.Get<int?>(0);
            Assert.AreEqual(1, result);
        }
    }
}
