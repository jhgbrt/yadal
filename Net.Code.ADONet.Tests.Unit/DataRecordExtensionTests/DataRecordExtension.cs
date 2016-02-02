using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using NSubstitute;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit.DataRecordExtensionTests
{
   
    [TestFixture]
    public class DataRecordExtension
    {
        public class MyEntity
        {
            public string MyProperty { get; set; }
            public int? MyNullableInt1 { get; set; }
            public int? MyNullableInt2 { get; set; }
            public int MyInt1 { get; set; }
            public int MyInt2 { get; set; }
        }

        [Test]
        public void MapTo_WhenCalled_EntityIsMapped()
        {
            var record = Substitute.For<IDataRecord>();
            record.FieldCount.Returns(3);
            record.GetName(0).Returns("MY_PROPERTY");
            record.GetValue(0).Returns("SomeValue");
            record.GetName(1).Returns("MY_NULLABLE_INT1");
            record.GetValue(1).Returns(DBNull.Value);
            record.GetName(2).Returns("MY_NULLABLE_INT2");
            record.GetValue(2).Returns(2);
            record.GetName(3).Returns("MYINT1");
            record.GetValue(3).Returns(DBNull.Value);
            record.GetName(2).Returns("MyInt2");
            record.GetValue(2).Returns(3);
            var entity = record.MapTo<MyEntity>();
            Assert.AreEqual("SomeValue", entity.MyProperty);
            Assert.IsNull(entity.MyNullableInt1);
            Assert.AreEqual(2, entity.MyNullableInt2);
            Assert.IsNull(entity.MyInt1);
            Assert.AreEqual(3, entity.MyInt2);
        }

       
        [Test]
        public void GivenDataReaderMock_WhenGetByNameReturnsDbNull_ResultIsNull()
        {
            var reader = Substitute.For<IDataReader>();
            reader.GetOrdinal("Id").Returns(0);
            reader[0].Returns(DBNull.Value);
            var result = reader.Get<int?>("Id");
            Assert.IsNull(result);
        }
        [Test]
        public void GivenDataReaderMock_WhenGetByIndexReturnsDbNull_ResultIsNull()
        {
            var reader = Substitute.For<IDataReader>();
            reader[0].Returns(DBNull.Value);
            var result = reader.Get<int?>(0);
            Assert.IsNull(result);
        }
        [Test]
        public void GivenDataReaderMock_WhenGetByNameReturnsValue_ResultIsValue()
        {
            var reader = Substitute.For<IDataReader>();
            reader.GetOrdinal("Id").Returns(0);
            reader[0].Returns(1);
            var result = reader.Get<int?>("Id");
            Assert.AreEqual(1, result);
        }
        [Test]
        public void GivenDataReaderMock_WhenGetByIndexReturnsValue_ResultIsValue()
        {
            var reader = Substitute.For<IDataReader>();
            reader[0].Returns(1);
            var result = reader.Get<int?>(0);
            Assert.AreEqual(1, result);
        }
    }
}
