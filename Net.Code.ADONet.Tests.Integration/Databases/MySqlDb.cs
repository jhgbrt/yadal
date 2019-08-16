using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Net.Code.ADONet.Tests.Integration.Data;

namespace Net.Code.ADONet.Tests.Integration.Databases
{
    public class MySqlDb : BaseDb<MySqlDb>
    {
        public MySqlDb() : base(MySqlClientFactory.Instance)
        {
        }
 
        public override void DropAndRecreate()
        {
            var databaseName = GetConnectionStringProperty("Database");

            var ddl = string.Format("DROP DATABASE IF EXISTS {0};\r\n" +
                                    "CREATE DATABASE {0}", databaseName);

            using (var db = MasterDb())
            {
                db.Execute(ddl);
            }

        }

        public override void BulkInsert(IDb db, IEnumerable<Person> list)
        {
            base.BulkInsert(db, list);
        }
    }
}