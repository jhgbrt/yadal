using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit.DataRecordExtensionTests
{
    [TestFixture]
    public class DataRecordExtension
    {
        [Test]
        public void GivenDataReaderMock_WhenGetByNameReturnsDbNull_ResultIsNull()
        {
            var reader = new Mock<IDataReader>();
            reader.Setup(r => r.GetOrdinal("Id")).Returns(0);
            reader.Setup(r => r[0]).Returns(DBNull.Value);
            var result = reader.Object.Get<int?>("Id");
            Assert.IsNull(result);
        }
        [Test]
        public void GivenDataReaderMock_WhenGetByIndexReturnsDbNull_ResultIsNull()
        {
            var reader = new Mock<IDataReader>();
            reader.Setup(r => r[0]).Returns(DBNull.Value);
            var result = reader.Object.Get<int?>(0);
            Assert.IsNull(result);
        }
        [Test]
        public void GivenDataReaderMock_WhenGetByNameReturnsValue_ResultIsValue()
        {
            var reader = new Mock<IDataReader>();
            reader.Setup(r => r.GetOrdinal("Id")).Returns(0);
            reader.Setup(r => r[0]).Returns(1);
            var result = reader.Object.Get<int?>("Id");
            Assert.AreEqual(1, result);
        }
        [Test]
        public void GivenDataReaderMock_WhenGetByIndexReturnsValue_ResultIsValue()
        {
            var reader = new Mock<IDataReader>();
            reader.Setup(r => r[0]).Returns(1);
            var result = reader.Object.Get<int?>(0);
            Assert.AreEqual(1, result);
        }
    }
}
