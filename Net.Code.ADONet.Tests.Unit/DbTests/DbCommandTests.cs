using System.Data;
using Moq;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit.DbTests
{

    [TestFixture]
    public class DbCommandTests
    {
        private IDb _db;

        [TestFixtureSetUp]
        public void Setup()
        {
            var commandStub = new Mock<IDbCommand>().SetupAllProperties().Object;

            var connection = new Mock<IDbConnection>();
            connection.Setup(c => c.CreateCommand()).Returns(commandStub);
            _db = new Db(connection.Object);
   
        }

        [Test]
        public void Sql_CommandBuilder_BuildsCommandWithCorrectText()
        {
            var result = _db.Sql("COMMANDTEXT").Command;
            Assert.AreEqual("COMMANDTEXT", result.CommandText);
        }

        [Test]
        public void Sql_CommandBuilder_BuildsCommandOfCorrectType()
        {
            var result = _db.Sql("").Command;
            Assert.AreEqual(CommandType.Text, result.CommandType);
        }

        [Test]
        public void StoredProcedure_CommandBuilder_BuildsCommandWithCorrectText()
        {
            var result = _db.StoredProcedure("COMMANDTEXT").Command;
            Assert.AreEqual("COMMANDTEXT", result.CommandText);
        }

        [Test]
        public void StoredProcedure_CommandBuilder_BuildsCommandOfCorrectType()
        {
            var result = _db.StoredProcedure("").Command;
            Assert.AreEqual(CommandType.StoredProcedure, result.CommandType);
        }
    }
}
