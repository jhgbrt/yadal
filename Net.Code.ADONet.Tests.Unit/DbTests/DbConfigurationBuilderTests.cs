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
        
        [TestMethod]
        public void Default_PrepareCommand_DoesNothing()
        {
            var config = DbConfig.FromProviderName("unkown");
            var command = Substitute.For<IDbCommand>();
            config.PrepareCommand(command);
        }

        [TestMethod]
        public void SqlServer_PrepareCommand_DoesNothing()
        {
            var config = DbConfig.FromProviderName("System.Data.SqlClient");
            var command = new SqlCommand();
            config.PrepareCommand(command);
        }

        [TestMethod]
        public void Oracle_PrepareCommand_SetsBindByName()
        {
            var config = DbConfig.FromProviderName("Oracle.DataAccess.Client");
            var oracleCommand = new FakeOracleDbCommand();
            config.PrepareCommand(oracleCommand);
            Assert.IsTrue(oracleCommand.BindByName);
        }

        [TestMethod]
        public void OracleManaged_PrepareCommand_SetsBindByName()
        {
            var config = DbConfig.FromProviderName("Oracle.ManagedDataAccess.Client");
            var oracleCommand = new FakeOracleDbCommand();
            config.PrepareCommand(oracleCommand);
            Assert.IsTrue(oracleCommand.BindByName);
        }

        [TestMethod]
        public void GivenDb_WhenNoSpecificConfigure__AndExecuteIsCalled_UsesDefaultConfig()
        {
            var fakeConnection = new FakeConnection();
            var db = new Db(fakeConnection, DbConfig.Default);
            db.Execute("");
        }

        [TestMethod]
        public void GivenDb_WhenOnPrepareIsConfigured__AndExecuteIsCalled_CommandIsPrepared()
        {
            var fakeConnection = new FakeConnection();
            var db = new Db(fakeConnection, new DbConfig(c => ((FakeCommand)c).Comment = "PREPARED", MappingConvention.OracleStyle, string.Empty));
            db.Execute("");
            Assert.AreEqual("PREPARED", fakeConnection.Commands.Single().Comment);
        }
    }
}