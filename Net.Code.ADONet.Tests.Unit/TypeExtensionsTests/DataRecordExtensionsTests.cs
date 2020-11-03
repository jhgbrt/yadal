using System.Data;
using NSubstitute;
using Xunit;


namespace Net.Code.ADONet.Tests.Unit.TypeExtensionsTests
{
    public class DataRecordExtensionsTests
    {
        private const string DATARECORD_COLUMN_NAME = "x";
        private const int DATARECORD_COLUMN_INDEX = 0;
        private const int DATARECORD_VALUE = 5;


        [Fact]
        public void Get_ByName_ShouldNotThrow()
        {
            var dataRecord = CreateFakeDataRecord();

            var result = dataRecord.Get<int>(DATARECORD_COLUMN_NAME);

            Assert.Equal(DATARECORD_VALUE, result);
        }

        [Fact]
        public void Get_ByIndex_ShouldNotThrow()
        {
            var dataRecord = CreateFakeDataRecord();

            var result = dataRecord.Get<int>(DATARECORD_COLUMN_INDEX);

            Assert.Equal(DATARECORD_VALUE, result);
        }

        private static IDataRecord CreateFakeDataRecord()
        {
            var dataRecord = Substitute.For<IDataRecord>();
            dataRecord.FieldCount.Returns(1);
            dataRecord.GetName(DATARECORD_COLUMN_INDEX).Returns(DATARECORD_COLUMN_NAME);
            dataRecord[DATARECORD_COLUMN_INDEX].Returns(DATARECORD_VALUE);
            return dataRecord;
        }
    }
}
