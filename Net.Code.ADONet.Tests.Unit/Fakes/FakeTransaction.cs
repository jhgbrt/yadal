using System.Data;
using System.Data.Common;

namespace Net.Code.ADONet.Tests.Unit
{
    class FakeTransaction : DbTransaction
    {
        private FakeConnection _connection;
        private IsolationLevel _isolationLevel;

        public FakeTransaction(FakeConnection connection, IsolationLevel isolationLevel)
        {
            _connection = connection;
            _isolationLevel = isolationLevel;
        }

        public override void Commit()
        {
        }

        public override void Rollback()
        {
        }

        protected override DbConnection DbConnection
        {
            get { return _connection; }
        }

        public override IsolationLevel IsolationLevel
        {
            get { return _isolationLevel; }
        }
    }
}