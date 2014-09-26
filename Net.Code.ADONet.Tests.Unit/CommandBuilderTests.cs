using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit
{
    [TestFixture]
    public class CommandBuilderTests
    {
        [Test]
        public void Logger_LogsCommand()
        {
            var logAction = Logger.Log;
            var sb = new StringBuilder();
            Logger.Log = s => sb.AppendLine(s);
            var command = PrepareCommand();

            new CommandBuilder(command)
                .WithCommandText("commandtext")
                .WithParameter("name", "value");
            
            Logger.LogCommand(command);
            var loggedText = sb.ToString();

            Assert.IsTrue(loggedText.Contains("commandtext"));
            Assert.IsTrue(loggedText.Contains("name"));
            Assert.IsTrue(loggedText.Contains("value"));
            Logger.Log = logAction;
        }
        [Test]
        public void CommandBuilder_WithParameterOfTypeString_Adds_Parameter()
        {
            var command = PrepareCommand();

            var b = new CommandBuilder(command).WithParameter("name", "value");

            var result = (IDbDataParameter)b.Command.Parameters[0];

            Assert.AreEqual("name", result.ParameterName);
            Assert.AreEqual("value", result.Value);
        }

        [Test]
        public void CommandBuilder_WithParameterOfTypeString_WhenParameterExists_ChangesValue()
        {
            var command = PrepareCommand();

            var b = new CommandBuilder(command)
                .WithParameter("name", "value1")
                .WithParameter("name", "value2");
            
            var result = (IDbDataParameter)b.Command.Parameters[0];

            Assert.AreEqual("name", result.ParameterName);
            Assert.AreEqual("value2", result.Value);
        }

        [Test]
        public void CommandBuilder_WithParameterOfTypeGuid_Adds_Parameter()
        {
            var command = PrepareCommand();

            var newGuid = Guid.NewGuid();
            var b = new CommandBuilder(command).WithParameter("name", newGuid);

            var result = (IDbDataParameter)b.Command.Parameters[0];

            Assert.AreEqual("name", result.ParameterName);
            Assert.AreEqual(newGuid, result.Value);
        }

        [Test]
        public void CommandBuilder_WithParametersAnonymous_Adds_Parameters()
        {
            var command = PrepareCommand();

            var b = new CommandBuilder(command)
                .WithParameters(new
                                    {
                                        Param1_Int32 = 0,
                                        Param2_string = string.Empty,
                                        Param3_DateTime = DateTime.MaxValue
                                    });

            var parameters = b.Command.Parameters;

            var param1 = parameters.OfType<IDbDataParameter>().FirstOrDefault(p => p.ParameterName == "Param1_Int32");
            Assert.IsNotNull(param1);
            Assert.AreEqual(0, param1.Value);

            var param2 = parameters.OfType<IDbDataParameter>().FirstOrDefault(p => p.ParameterName == "Param2_string");
            Assert.IsNotNull(param2);
            Assert.AreEqual(string.Empty, param2.Value);

            var param3 = parameters.OfType<IDbDataParameter>().FirstOrDefault(p => p.ParameterName == "Param3_DateTime");
            Assert.IsNotNull(param3);
            Assert.AreEqual(DateTime.MaxValue, param3.Value);

        }

        [Test]
        public void CommandBuilder_WithTimeout_SetsTimeout()
        {
            var command = PrepareCommand();
            var b = new CommandBuilder(command);
            b.WithTimeout(TimeSpan.FromSeconds(123));
            Assert.AreEqual(123, command.CommandTimeout);
        }

        [Test]
        public void WithParameter_utd()
        {
            var command = PrepareCommand();

            var b = new CommandBuilder(command)
               .WithParameter("ParamName", new[] { new { ID = 123 } }, "dbo.udtname");

            var p = (SqlParameter)b.Command.Parameters[0];
            Assert.AreEqual("ParamName", p.ParameterName);
            Assert.IsTrue(p.Value is DataTable);
            Assert.IsTrue(((DataTable)p.Value).AsEnumerable().Single().Field<int>("ID") == 123);
            Assert.AreEqual("dbo.udtname", p.TypeName);
            Assert.AreEqual(SqlDbType.Structured, p.SqlDbType);
        }
        private static IDbCommand PrepareCommand()
        {
            var command = new FakeCommand(new FakeConnection());
            return command;
        }
    }
}
