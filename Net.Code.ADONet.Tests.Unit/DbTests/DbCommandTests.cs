using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using Net.Code.ADONet.Extensions.SqlClient;
using Xunit;

namespace Net.Code.ADONet.Tests.Unit.DbTests
{

    public class DbCommandTests
    {
        private IDb _db;
        private FakeConnection _fakeConnection;

        public DbCommandTests()
        {
            _fakeConnection = new FakeConnection();
            _db = new Db(_fakeConnection, DbConfig.Default);
        }

        [Fact]
        public void Sql_CommandBuilder_BuildsCommandWithCorrectText()
        {
            var result = _db.Sql("COMMANDTEXT").Command;
            Assert.Equal("COMMANDTEXT", result.CommandText);
        }

        [Fact]
        public void Sql_CommandBuilder_BuildsCommandOfCorrectType()
        {
            var result = _db.Sql("").Command;
            Assert.Equal(CommandType.Text, result.CommandType);
        }

        [Fact]
        public void StoredProcedure_CommandBuilder_BuildsCommandWithCorrectText()
        {
            var result = _db.StoredProcedure("COMMANDTEXT").Command;
            Assert.Equal("COMMANDTEXT", result.CommandText);
        }

        [Fact]
        public void StoredProcedure_CommandBuilder_BuildsCommandOfCorrectType()
        {
            var result = _db.StoredProcedure("").Command;
            Assert.Equal(CommandType.StoredProcedure, result.CommandType);
        }

        [Fact]
        public void Execute_ExecutesCommand()
        {
            _db.Execute("COMMANDTEXT");
            var fakeCommand = _fakeConnection.Commands.Single();
            Assert.Equal("COMMANDTEXT", fakeCommand.CommandText);
            Assert.Equal(CommandMode.NonQuery, fakeCommand.Mode);
        }
    }


    public class DbCommandBuilderTests
    {
        private SqlCommand _sqlCommand;
        private CommandBuilder _builder;
        private StringBuilder _logging;

        public DbCommandBuilderTests()
        {
            _logging = new StringBuilder();
            Logger.Log = s => _logging.AppendLine(s);
            _sqlCommand = new SqlCommand();
            _builder = new CommandBuilder(_sqlCommand, DbConfig.Default);
        }

        [Fact]
        public void WithParameter_AddsParameterWithCorrectNameAndValue()
        {
            _builder.WithParameter("myparam", "myvalue");
            Assert.Equal(1, _sqlCommand.Parameters.Count);
            Assert.Equal("myparam", _sqlCommand.Parameters[0].ParameterName);
            Assert.Equal("myvalue", _sqlCommand.Parameters[0].Value);
        }

        [Fact]
        public void WithParameters_AnonymousObject_AddsParameterWithCorrectNameAndValue()
        {
            _builder.WithParameters(new
                                    {
                                        myparam1 = "myvalue", 
                                        myparam2 = 999
                                    });
            Assert.Equal(2, _sqlCommand.Parameters.Count);
            Assert.Equal("myparam1", _sqlCommand.Parameters[0].ParameterName);
            Assert.Equal("myvalue", _sqlCommand.Parameters[0].Value);
            Assert.Equal("myparam2", _sqlCommand.Parameters[1].ParameterName);
            Assert.Equal(999, _sqlCommand.Parameters[1].Value);
        }

        [Fact]
        public void WithTimeout_SetsCommandTimeOut()
        {
            var timeout = TimeSpan.FromMinutes(42);
            _builder.WithTimeout(timeout);
            Assert.Equal(timeout, TimeSpan.FromSeconds(_sqlCommand.CommandTimeout));
        }

        [Fact]
        public void WithParameter_TableValued_SetsParameter()
        {
            var input = new[] {1, 2, 3};
            _builder.WithParameter("tableValuedParameter", input, "arrayOfInt");
            Assert.Equal(1, _sqlCommand.Parameters.Count);
            Assert.Equal("tableValuedParameter", _sqlCommand.Parameters[0].ParameterName);
            Assert.Equal(SqlDbType.Structured, _sqlCommand.Parameters[0].SqlDbType);
            Assert.Equal("arrayOfInt", _sqlCommand.Parameters[0].TypeName);
        }

        [Fact]
        public void OfType_SetsCommandType()
        {
            _builder.OfType(CommandType.StoredProcedure);
            Assert.Equal(CommandType.StoredProcedure, _sqlCommand.CommandType);
        }

        [Fact]
        public void WithCommandText_SetsCommandText()
        {
            var text = "SELECT 42 FROM DUAL";
            _builder.WithCommandText(text);
            Assert.Equal(text, _sqlCommand.CommandText);
        }
    }

}
