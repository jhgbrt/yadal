using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Net.Code.ADONet.Tests.Unit
{
    enum CommandMode
    {
        NonQuery,
        Scalar,
        Reader
    }

    class FakeCommand : DbCommand
    {
        private IDataReader _dataReader;
        private object _scalarValue;
        private int _nonQueryResult;

        public FakeCommand(FakeConnection fakeConnection)
        {
            Connection = fakeConnection;
        }

        public override void Prepare()
        {
        }

        public override string CommandText { get; set; }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; }

        protected override DbParameterCollection DbParameterCollection { get; } = new FakeParameterCollection();

        protected override DbTransaction DbTransaction { get; set; }

        public override bool DesignTimeVisible { get; set; }

        public void SetResultSet<T>(IEnumerable<T> list) => _dataReader = list.AsDataReader();

        public void SetMultiResultSet<T>(IEnumerable<IEnumerable<T>> list) => _dataReader = list.AsMultiDataReader();

        public void SetScalarValue<T>(T value) => _scalarValue = value;

        public void SetNonQueryResult(int nonQueryResult) => _nonQueryResult = nonQueryResult;

        public override void Cancel() {}

        protected override DbParameter CreateDbParameter() => new FakeParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            Mode = CommandMode.Reader;
            return (DbDataReader) _dataReader;
        }

        public override int ExecuteNonQuery()
        {
            Mode = CommandMode.NonQuery;
            return _nonQueryResult;
        }

        public CommandMode Mode { get; set; }
        public string Comment { get; set; }

        public override object ExecuteScalar()
        {
            Mode = CommandMode.Scalar;
            return _scalarValue;
        }
    }
}