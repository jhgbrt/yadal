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
            var ddl1 = "DROP TABLE PERSON IF EXISTS; \n" +
                " DROP TABLE ADDRESS IF EXISTS;";

            using (var db = MasterDb())
            {
                db.Execute(ddl1);
            }

        }
    }
}