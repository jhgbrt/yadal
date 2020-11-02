using Microsoft.Data.Sqlite;

namespace Net.Code.ADONet.Tests.Integration.Databases
{
    public class MSSqLiteDb : BaseDb<SqLiteDb>
    {
        public MSSqLiteDb() : base(SqliteFactory.Instance)
        {
        }

        public override void DropAndRecreate()
        {
        }
    }
}