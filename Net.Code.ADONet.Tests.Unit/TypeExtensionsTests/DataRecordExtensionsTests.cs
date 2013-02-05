using System.Data;
using Moq;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit.TypeExtensionsTests
{
    [TestFixture]
    public class DataRecordExtensionsTests
    {
        private const string DATARECORD_COLUMN_NAME = "x";
        private const int DATARECORD_COLUMN_INDEX = 0;
        private const int DATARECORD_VALUE = 5;

        [Test]
        public void ToExpando_WithEmptyDataRecord_ShouldNotThrow()
        {
            var dataRecord = new Mock<IDataRecord>().SetupAllProperties().Object;
            dataRecord.ToExpando();
        }

        [Test]
        public void ToExpando_WithActualData_ShouldNotThrow()
        {
            var dataRecord = CreateFakeDataRecord();
            
            var result = dataRecord.ToExpando();

            int x = result.x;

            Assert.AreEqual(DATARECORD_VALUE, x);
        }

        [Test]
        public void Get_ByName_ShouldNotThrow()
        {
            var dataRecord = CreateFakeDataRecord();

            var result = dataRecord.Get<int>(DATARECORD_COLUMN_NAME);

            Assert.AreEqual(DATARECORD_VALUE, result);
        }

        [Test]
        public void Get_ByIndex_ShouldNotThrow()
        {
            var dataRecord = CreateFakeDataRecord();

            var result = dataRecord.Get<int>(DATARECORD_COLUMN_INDEX);

            Assert.AreEqual(DATARECORD_VALUE, result);
        }

        private static IDataRecord CreateFakeDataRecord()
        {
            var dataRecord = new Mock<IDataRecord>();
            dataRecord.Setup(r => r.FieldCount).Returns(1);
            dataRecord.Setup(r => r.GetName(DATARECORD_COLUMN_INDEX)).Returns(DATARECORD_COLUMN_NAME);
            dataRecord.Setup(r => r[DATARECORD_COLUMN_INDEX]).Returns(DATARECORD_VALUE);
            return dataRecord.Object;
        }
    }
}
