using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Net.Code.ADONet.Tests.Unit
{
    class FakeConnection : DbConnection
    {
        public Action OnDispose { get; }
        public FakeConnection()
        {
            Commands = new List<FakeCommand>();
            OnDispose = () => { };
        }

        private string _database;
        private ConnectionState _state;

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new FakeTransaction(this, isolationLevel);

        public override void Close() => Dispose();

        protected override void Dispose(bool disposing)
        {
            OnDispose();
            base.Dispose(disposing);
        }

        public override void ChangeDatabase(string databaseName) => _database = databaseName;

        public override void Open() => _state = ConnectionState.Open;

        public override string ConnectionString { get; set; }

        public override string Database => _database;

        public override ConnectionState State => _state;

        public override string DataSource { get; }

        public override string ServerVersion => string.Empty;

        public List<FakeCommand> Commands { get; }

        protected override DbCommand CreateDbCommand()
        {
            var fakeCommand = new FakeCommand(this);
            Commands.Add(fakeCommand);
            return fakeCommand;
        }
    }
}