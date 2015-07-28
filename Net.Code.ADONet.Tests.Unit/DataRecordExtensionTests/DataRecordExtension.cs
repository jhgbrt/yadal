using System;
using System.Data;
using NSubstitute;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit.DataRecordExtensionTests
{
    [TestFixture]
    public class DataRecordExtension
    {
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
