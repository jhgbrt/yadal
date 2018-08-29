using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Net.Code.ADONet.Extensions.SqlClient;
using NSubstitute;
using Xunit;

namespace Net.Code.ADONet.Tests.Unit
{
    public class CommandBuilderTests
    {
        [Fact]
        public void Logger_LogsCommand()
        {
            var logAction = Logger.Log;
            var sb = new StringBuilder();
            Logger.Log = s => sb.AppendLine(s);
            var command = PrepareCommand();
            
            new CommandBuilder(command, DbConfig.Default)
                .WithCommandText("commandtext")
                .WithParameter("name", "value");
            
            Logger.LogCommand(command);
            var loggedText = sb.ToString();

            Assert.Contains("commandtext", loggedText);
            Assert.Contains("name", loggedText);
            Assert.Contains("value", loggedText);
            Logger.Log = logAction;
        }

        [Fact]
        public void Logger_WhenNull_DoesNotThrow()
        {
            var logAction = Logger.Log;

            Logger.Log = null;

            var command = PrepareCommand();

            new CommandBuilder(command, DbConfig.Default)
                .WithCommandText("commandtext")
                .WithParameter("name", "value");

            Logger.LogCommand(command);

            Logger.Log = logAction;
        }

        [Fact]
        public void CommandBuilder_WithParameterOfTypeString_Adds_Parameter()
        {
            var command = PrepareCommand();

            var b = new CommandBuilder(command, DbConfig.Default).WithParameter("name", "value");

            var result = (IDbDataParameter)b.Command.Parameters[0];

            Assert.Equal("name", result.ParameterName);
            Assert.Equal("value", result.Value);
        }

        [Fact]
        public void CommandBuilder_WithParameterOfTypeString_WhenParameterExists_ChangesValue()
        {
            var command = PrepareCommand();

            var b = new CommandBuilder(command, DbConfig.Default)
                .WithParameter("name", "value1")
                .WithParameter("name", "value2");
            
            var result = (IDbDataParameter)b.Command.Parameters[0];

            Assert.Equal("name", result.ParameterName);
            Assert.Equal("value2", result.Value);
        }

        [Fact]
        public void CommandBuilder_WithParameterOfTypeGuid_Adds_Parameter()
        {
            var command = PrepareCommand();

            var newGuid = Guid.NewGuid();
            var b = new CommandBuilder(command, DbConfig.Default).WithParameter("name", newGuid);

            var result = (IDbDataParameter)b.Command.Parameters[0];

            Assert.Equal("name", result.ParameterName);
            Assert.Equal(newGuid, result.Value);
        }

        [Fact]
        public void CommandBuilder_WithDbDataParameter_Adds_Parameters()
        {
            var command = PrepareCommand();
            var parameter = Substitute.For<DbParameter>();
            parameter.ParameterName.Returns("MyParameterName");
            parameter.Value.Returns("MyParameterValue");
            var b = new CommandBuilder(command, DbConfig.Default)
                .WithParameter(parameter);

            var parameters = b.Command.Parameters;

            IDbDataParameter param1 = (IDbDataParameter) parameters[0];
            Assert.NotNull(param1);
            Assert.Equal("MyParameterValue", param1.Value);
        }

        [Fact]
        public void CommandBuilder_WithParametersAnonymous_Adds_Parameters()
        {
            var command = PrepareCommand();

            var b = new CommandBuilder(command, DbConfig.Default)
                .WithParameters(new
                                    {
                                        Param1_Int32 = 0,
                                        Param2_string = string.Empty,
                                        Param3_DateTime = DateTime.MaxValue
                                    });

            var parameters = b.Command.Parameters;

            var param1 = parameters.OfType<IDbDataParameter>().FirstOrDefault(p => p.ParameterName == "Param1_Int32");
            Assert.NotNull(param1);
            Assert.Equal(0, param1.Value);

            var param2 = parameters.OfType<IDbDataParameter>().FirstOrDefault(p => p.ParameterName == "Param2_string");
            Assert.NotNull(param2);
            Assert.Equal(string.Empty, param2.Value);

            var param3 = parameters.OfType<IDbDataParameter>().FirstOrDefault(p => p.ParameterName == "Param3_DateTime");
            Assert.NotNull(param3);
            Assert.Equal(DateTime.MaxValue, param3.Value);

        }

        [Fact]
        public void CommandBuilder_WithTimeout_SetsTimeout()
        {
            var command = PrepareCommand();
            var b = new CommandBuilder(command, DbConfig.Default);
            b.WithTimeout(TimeSpan.FromSeconds(123));
            Assert.Equal(123, command.CommandTimeout);
        }

        [Fact]
        public void WithParameter_utd()
        {
            var command = PrepareCommand();

            var b = new CommandBuilder(command, DbConfig.Default)
               .WithParameter("ParamName", new[] { new { ID = 123 } }, "dbo.udtname");

            var p = (SqlParameter)b.Command.Parameters[0];
            Assert.Equal("ParamName", p.ParameterName);
            Assert.True(p.Value is DataTable);
            Assert.True(System.Data.DataTableExtensions.AsEnumerable((DataTable)p.Value).Single().Field<int>("ID") == 123);
            Assert.Equal("dbo.udtname", p.TypeName);
            Assert.Equal(SqlDbType.Structured, p.SqlDbType);
        }

        [Fact]
        public void InTransaction_SetsTransactionOnCommand()
        {
            var command = PrepareCommand();

            var tx = Substitute.For<DbTransaction>();

            new CommandBuilder(command, DbConfig.Default).InTransaction(tx);

            Assert.Equal(tx, command.Transaction);
        }

        private static IDbCommand PrepareCommand()
        {
            var command = new FakeCommand(new FakeConnection());
            return command;
        }
    }
}
