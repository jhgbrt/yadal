using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Moq;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit
{
    [TestFixture]
    public class CommandBuilderTests
    {
        [Test]
        public void CommandBuilder_WithParameterOfTypeString_Adds_Parameter()
        {
            var commandMock = PrepareStubbedCommand();

            var b = new CommandBuilder(commandMock.Object).WithParameter("name", "value");

            var result = (IDbDataParameter)b.Command.Parameters[0];

            Assert.AreEqual("name", result.ParameterName);
            Assert.AreEqual("value", result.Value);
        }

        [Test]
        public void CommandBuilder_WithParameterOfTypeGuid_Adds_Parameter()
        {
            var commandMock = PrepareStubbedCommand();

            var newGuid = Guid.NewGuid();
            var b = new CommandBuilder(commandMock.Object).WithParameter("name", newGuid);

            var result = (IDbDataParameter)b.Command.Parameters[0];

            Assert.AreEqual("name", result.ParameterName);
            Assert.AreEqual(newGuid, result.Value);
        }

        [Test]
        public void CommandBuilder_WithParametersAnonymous_Adds_Parameters()
        {
            var commandMock = PrepareStubbedCommand();

            var b = new CommandBuilder(commandMock.Object)
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
            var command = PrepareStubbedCommand().Object;
            var b = new CommandBuilder(command);
            b.WithTimeout(TimeSpan.FromSeconds(123));
            Assert.AreEqual(123, command.CommandTimeout);
        }

        [Test]
        public void WithParameter_utd()
        {
            var command = PrepareStubbedCommand().Object;
            var b = new CommandBuilder(command)
               .WithParameter("ParamName", new[] { new { ID = 123 } }, "dbo.udtname");

            var p = (SqlParameter)b.Command.Parameters[0];
            Assert.AreEqual("ParamName", p.ParameterName);
            Assert.IsTrue(p.Value is DataTable);
            Assert.IsTrue(((DataTable)p.Value).AsEnumerable().Single().Field<int>("ID") == 123);
            Assert.AreEqual("dbo.udtname", p.TypeName);
            Assert.AreEqual(SqlDbType.Structured, p.SqlDbType);
        }
        class Person
        {
            // ReSharper disable UnusedAutoPropertyAccessor.Local
            public string FirstName { get; set; }
            // ReSharper restore UnusedAutoPropertyAccessor.Local
        }

        [Test]
        public void AsEnumerable()
        {
            var connection = new Mock<IDbConnection>();
            connection.Setup(c => c.State).Returns(ConnectionState.Closed).Verifiable();

            var command = PrepareStubbedCommand();
            command.Setup(c => c.Connection).Returns(connection.Object);
            var commandBuilder = new CommandBuilder(command.Object);

            var listDataReader = new[] { new Person { FirstName = "FirstName" } }.AsDataReader();
            command.Setup(c => c.ExecuteReader()).Returns(listDataReader).Verifiable();

            var result = commandBuilder.AsEnumerable().ToList();

            connection.VerifyAll();
            command.Verify(c => c.ExecuteReader(), Times.Once());
            Assert.AreEqual("FirstName", result[0].FirstName);
        }

        [Test]
        public void AsScalar()
        {
            var connection = new Mock<IDbConnection>();
            connection.Setup(c => c.State).Returns(ConnectionState.Closed).Verifiable();

            var command = PrepareStubbedCommand();
            command.Setup(c => c.Connection).Returns(connection.Object);
            var commandBuilder = new CommandBuilder(command.Object);

            command.Setup(c => c.ExecuteScalar()).Returns(1).Verifiable();

            var result = commandBuilder.AsScalar<int>();

            Assert.AreEqual(1, result);
            connection.VerifyAll();
            command.Verify(c => c.ExecuteScalar(), Times.Once());
        }

        [Test]
        public void AsNonQuery()
        {
            var connection = new Mock<IDbConnection>();
            connection.Setup(c => c.State).Returns(ConnectionState.Closed).Verifiable();

            var command = PrepareStubbedCommand();
            command.Setup(c => c.Connection).Returns(connection.Object);
            var commandBuilder = new CommandBuilder(command.Object);

            command.Setup(c => c.ExecuteNonQuery()).Returns(1).Verifiable();

            commandBuilder.AsNonQuery();

            connection.VerifyAll();
            command.Verify(c => c.ExecuteNonQuery(), Times.Once());
        }

        [Test]
        public void AsMultipleResultSet()
        {
            var connection = new Mock<IDbConnection>();
            connection.Setup(c => c.State).Returns(ConnectionState.Closed).Verifiable();

            var command = PrepareStubbedCommand();
            command.Setup(c => c.Connection).Returns(connection.Object);
            var commandBuilder = new CommandBuilder(command.Object);

            var listDataReader = new[]
            {
                new[] { new Person { FirstName = "FirstPersonOfFirstList" } },
                new[] { new Person { FirstName = "FirstPersonOfSecondList" },new Person { FirstName = "SecondPersonOfSecondList" } }
            }.AsMultiDataReader();

            command.Setup(c => c.ExecuteReader()).Returns(listDataReader).Verifiable();

            var result = commandBuilder.AsMultiResultSet().Select(i => i.ToArray()).ToList();

            connection.VerifyAll();
            command.Verify(c => c.ExecuteReader(), Times.Once());

            Assert.AreEqual("FirstPersonOfFirstList", result[0][0].FirstName);
            Assert.AreEqual("FirstPersonOfSecondList", result[1][0].FirstName);
            Assert.AreEqual("SecondPersonOfSecondList", result[1][1].FirstName);
        }

        private static Mock<IDbCommand> PrepareStubbedCommand()
        {
            var command = new Mock<IDbCommand>().SetupAllProperties();
            command.Setup(c => c.CreateParameter()).Returns(() => new Mock<IDbDataParameter>().SetupAllProperties().Object);
            var parameterCollection = new ParameterCollection();
            command.Setup(c => c.Parameters).Returns(parameterCollection);
            return command;
        }
    }

    class ParameterCollection : Collection<IDbDataParameter>, IDataParameterCollection
    {
        public bool Contains(string parameterName)
        {
            return this.Any(p => p.ParameterName == parameterName);
        }

        public int IndexOf(string parameterName)
        {
            var p = this.FirstOrDefault(x => x.ParameterName == parameterName);
            return p == null ? -1 : IndexOf(p);
        }

        public void RemoveAt(string parameterName)
        {
            Remove((IDbDataParameter)this[parameterName]);
        }

        public object this[string parameterName]
        {
            get { return base[IndexOf(parameterName)]; }
            set { base[IndexOf(parameterName)] = (IDbDataParameter)value; }
        }
    }
}
