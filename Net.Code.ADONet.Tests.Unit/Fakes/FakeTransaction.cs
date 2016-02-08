using System.Data;
using System.Data.Common;

namespace Net.Code.ADONet.Tests.Unit
{
    class FakeTransaction : DbTransaction
    {
        private readonly FakeConnection _connection;

        public FakeTransaction(FakeConnection connection, IsolationLevel isolationLevel)
        {
            _connection = connection;
            IsolationLevel = isolationLevel;
        }

        public override void Commit()
        {
        }

        public override void Rollback()
        {
        }

        protected override DbConnection DbConnection => _connection;

        public override IsolationLevel IsolationLevel { get; }
    }
}