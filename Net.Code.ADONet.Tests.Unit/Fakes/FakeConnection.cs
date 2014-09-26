using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Net.Code.ADONet.Tests.Unit
{
    class FakeConnection : DbConnection
    {
        public Action OnDispose { get; set; }
        public FakeConnection()
        {
            Commands = new List<FakeCommand>();
            OnDispose = () => { };
        }

        private string _database;
        private ConnectionState _state;
        private string _datasource;

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return new FakeTransaction(this, isolationLevel);
        }

        public override void Close()
        {
            Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            OnDispose();
            base.Dispose(disposing);
        }

        public override void ChangeDatabase(string databaseName)
        {
            _database = databaseName;
        }

        public override void Open()
        {
            _state = ConnectionState.Open;
        }

        public override string ConnectionString { get; set; }

        public override string Database
        {
            get { return _database; }
        }

        public override ConnectionState State
        {
            get { return _state; }
        }

        public override string DataSource
        {
            get { return _datasource; }
        }

        public override string ServerVersion
        {
            get { return string.Empty; }
        }

        public List<FakeCommand> Commands { get; set; }

        protected override DbCommand CreateDbCommand()
        {
            var fakeCommand = new FakeCommand(this);
            Commands.Add(fakeCommand);
            return fakeCommand;
        }
    }
}