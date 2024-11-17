using IBM.Data.Db2;

namespace Net.Code.ADONet.Tests.Integration.Databases
{
    public class DB2Db : BaseDb<DB2Db>
    {
        public DB2Db() : base(DB2Factory.Instance)
        {
        }
    }
}