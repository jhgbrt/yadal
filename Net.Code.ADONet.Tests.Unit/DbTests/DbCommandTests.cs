using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net.Code.ADONet.Extensions.SqlClient;

namespace Net.Code.ADONet.Tests.Unit.DbTests
{

    [TestClass]
    public class DbCommandTests
    {
        private IDb _db;
        private FakeConnection _fakeConnection;

        [TestInitialize]
        public void Setup()
        {
            _fakeConnection = new FakeConnection();
            _db = new Db(_fakeConnection);
        }

        [TestMethod]
        public void Sql_CommandBuilder_BuildsCommandWithCorrectText()
        {
            var result = _db.Sql("COMMANDTEXT").Command;
            Assert.AreEqual("COMMANDTEXT", result.CommandText);
        }

        [TestMethod]
        public void Sql_CommandBuilder_BuildsCommandOfCorrectType()
        {
            var result = _db.Sql("").Command;
            Assert.AreEqual(CommandType.Text, result.CommandType);
        }

        [TestMethod]
        public void StoredProcedure_CommandBuilder_BuildsCommandWithCorrectText()
        {
            var result = _db.StoredProcedure("COMMANDTEXT").Command;
            Assert.AreEqual("COMMANDTEXT", result.CommandText);
        }

        [TestMethod]
        public void StoredProcedure_CommandBuilder_BuildsCommandOfCorrectType()
        {
            var result = _db.StoredProcedure("").Command;
            Assert.AreEqual(CommandType.StoredProcedure, result.CommandType);
        }

        [TestMethod]
        public void Execute_ExecutesCommand()
        {
            _db.Execute("COMMANDTEXT");
            var fakeCommand = _fakeConnection.Commands.Single();
            Assert.AreEqual("COMMANDTEXT", fakeCommand.CommandText);
            Assert.AreEqual(CommandMode.NonQuery, fakeCommand.Mode);
        }
    }

    [TestClass]
    public class DbCommandBuilderTests
    {
        private SqlCommand _sqlCommand;
        private CommandBuilder _builder;
        private StringBuilder _logging;

        [TestInitialize]
        public void Setup()
        {
            _logging = new StringBuilder();
            Logger.Log = s => _logging.AppendLine(s);
            _sqlCommand = new SqlCommand();
            _builder = new CommandBuilder(_sqlCommand);
        }

        [TestMethod]
        public void WithParameter_AddsParameterWithCorrectNameAndValue()
        {
            _builder.WithParameter("myparam", "myvalue");
            Assert.AreEqual(1, _sqlCommand.Parameters.Count, "expected exactly one parameter");
            Assert.AreEqual("myparam", _sqlCommand.Parameters[0].ParameterName);
            Assert.AreEqual("myvalue", _sqlCommand.Parameters[0].Value);
        }

        [TestMethod]
        public void WithParameters_AnonymousObject_AddsParameterWithCorrectNameAndValue()
        {
            _builder.WithParameters(new
                                    {
                                        myparam1 = "myvalue", 
                                        myparam2 = 999
                                    });
            Assert.AreEqual(2, _sqlCommand.Parameters.Count, "expected exactly two parameters");
            Assert.AreEqual("myparam1", _sqlCommand.Parameters[0].ParameterName);
            Assert.AreEqual("myvalue", _sqlCommand.Parameters[0].Value);
            Assert.AreEqual("myparam2", _sqlCommand.Parameters[1].ParameterName);
            Assert.AreEqual(999, _sqlCommand.Parameters[1].Value);
        }

        [TestMethod]
        public void WithTimeout_SetsCommandTimeOut()
        {
            var timeout = TimeSpan.FromMinutes(42);
            _builder.WithTimeout(timeout);
            Assert.AreEqual(timeout, TimeSpan.FromSeconds(_sqlCommand.CommandTimeout));
        }

        [TestMethod]
        public void WithParameter_TableValued_SetsParameter()
        {
            var input = new[] {1, 2, 3};
            _builder.WithParameter("tableValuedParameter", input, "arrayOfInt");
            Assert.AreEqual(1, _sqlCommand.Parameters.Count, "expected exactly one parameter");
            Assert.AreEqual("tableValuedParameter", _sqlCommand.Parameters[0].ParameterName);
            Assert.AreEqual(SqlDbType.Structured, _sqlCommand.Parameters[0].SqlDbType);
            Assert.AreEqual("arrayOfInt", _sqlCommand.Parameters[0].TypeName);
        }

        [TestMethod]
        public void OfType_SetsCommandType()
        {
            _builder.OfType(CommandType.StoredProcedure);
            Assert.AreEqual(CommandType.StoredProcedure, _sqlCommand.CommandType);
        }

        [TestMethod]
        public void WithCommandText_SetsCommandText()
        {
            var text = "SELECT 42 FROM DUAL";
            _builder.WithCommandText(text);
            Assert.AreEqual(text, _sqlCommand.CommandText);
        }
    }

}
