using IBM.Data.DB2.Core;

namespace Net.Code.ADONet.Tests.Integration.Databases
{
    public class DB2Db : BaseDb<DB2Db>
    {
        public DB2Db() : base(DB2Factory.Instance)
        {
            
        }

        public override void DropAndRecreate()
        {
        }
    }
}