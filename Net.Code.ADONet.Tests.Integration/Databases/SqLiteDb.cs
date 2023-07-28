using System.Data.SQLite;

namespace Net.Code.ADONet.Tests.Integration.Databases
{
    public class SqLiteDb : BaseDb<SqLiteDb>
    {
        public SqLiteDb() : base(SQLiteFactory.Instance)
        {
        }
    }
}