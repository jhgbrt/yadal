using System.Data;
using System.Data.SqlClient;
using System.Linq;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Code.ADONet.Tests.Unit.DbTests
{
    [TestClass]
    public class DbConfigurationBuilderTests
    {
        private DbConfigurationBuilder _dbConfigurationBuilder;
        private DbConfig _config;

        [TestInitialize]
        public void Setup()
        {
            _config = new DbConfig();
            _dbConfigurationBuilder = new DbConfigurationBuilder(_config);
        }

        [TestMethod]
        public void Default_PrepareCommand_DoesNothing()
        {
            _dbConfigurationBuilder.FromProviderName("unknown");
            var command = Substitute.For<IDbCommand>();
            _config.PrepareCommand(command);
        }

        [TestMethod]
        public void SqlServer_PrepareCommand_DoesNothing()
        {
            _dbConfigurationBuilder.FromProviderName("System.Data.SqlClient");
            var command = new SqlCommand();
            _config.PrepareCommand(command);
        }

        [TestMethod]
        public void Oracle_PrepareCommand_SetsBindByName()
        {
            _dbConfigurationBuilder.FromProviderName("Oracle.DataAccess.Client");
            var oracleCommand = new FakeOracleDbCommand();
            _config.PrepareCommand(oracleCommand);
            Assert.IsTrue(oracleCommand.BindByName);
        }

        [TestMethod]
        public void OracleManaged_PrepareCommand_SetsBindByName()
        {
            _dbConfigurationBuilder.FromProviderName("Oracle.ManagedDataAccess.Client");
            var oracleCommand = new FakeOracleDbCommand();
            _config.PrepareCommand(oracleCommand);
            Assert.IsTrue(oracleCommand.BindByName);
        }

        [TestMethod]
        public void GivenDb_WhenNoSpecificConfigure__AndExecuteIsCalled_UsesDefaultConfig()
        {
            var fakeConnection = new FakeConnection();
            var db = new Db(fakeConnection);
            db.Configure();
            db.Execute("");
        }

        [TestMethod]
        public void GivenDb_WhenOnPrepareIsConfigured__AndExecuteIsCalled_CommandIsPrepared()
        {
            var fakeConnection = new FakeConnection();
            var db = new Db(fakeConnection);
            db.Configure().OnPrepareCommand(c => ((FakeCommand)c).Comment = "PREPARED");
            db.Execute("");
            Assert.AreEqual("PREPARED", fakeConnection.Commands.Single().Comment);
        }
    }
}