using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using NSubstitute;
using Xunit;

namespace Net.Code.ADONet.Tests.Unit.DbTests
{
    public class DbConfigurationBuilderTests
    {
        [Fact]
        public void Default_PrepareCommand_DoesNothing()
        {
            var config = DbConfig.FromProviderName("unkown");
            var command = Substitute.For<IDbCommand>();
            config.PrepareCommand(command);
        }

        [Fact]
        public void SqlServer_PrepareCommand_DoesNothing()
        {
            var config = DbConfig.FromProviderName("System.Data.SqlClient");
            var command = new SqlCommand();
            config.PrepareCommand(command);
        }

        [Fact]
        public void Oracle_PrepareCommand_SetsBindByName()
        {
            var config = DbConfig.FromProviderName("Oracle.DataAccess.Client");
            var oracleCommand = new FakeOracleDbCommand();
            config.PrepareCommand(oracleCommand);
            Assert.True(oracleCommand.BindByName);
        }

        [Fact]
        public void OracleManaged_PrepareCommand_SetsBindByName()
        {
            var config = DbConfig.FromProviderName("Oracle.ManagedDataAccess.Client");
            var oracleCommand = new FakeOracleDbCommand();
            config.PrepareCommand(oracleCommand);
            Assert.True(oracleCommand.BindByName);
        }

        [Fact]
        public void GivenDb_WhenNoSpecificConfigure__AndExecuteIsCalled_UsesDefaultConfig()
        {
            var fakeConnection = new FakeConnection();
            var db = new Db(fakeConnection, DbConfig.Default);
            db.Execute("");
        }

        [Fact]
        public void GivenDb_WhenOnPrepareIsConfigured__AndExecuteIsCalled_CommandIsPrepared()
        {
            var fakeConnection = new FakeConnection();
            var db = new Db(fakeConnection, new DbConfig(
                c => ((FakeCommand)c).Comment = "PREPARED",
                MappingConvention.Default)
                );
            db.Execute("");
            Assert.Equal("PREPARED", fakeConnection.Commands.Single().Comment);
        }
    }
}