using System;
using System.Collections.Generic;
using System.Configuration;
using MySql.Data.MySqlClient;
using Net.Code.ADONet.Tests.Integration.Data;

namespace Net.Code.ADONet.Tests.Integration.Databases
{
    public class MySqlDb : BaseDb
    {
        public override void DropAndRecreate()
        {
            var connectionString = ConfigurationManager.ConnectionStrings[Name].ConnectionString;
            var connectionStringBuilder = new MySqlConnectionStringBuilder
            {
                ConnectionString = connectionString
            };
            var databaseName = connectionStringBuilder.Database;

            var ddl = string.Format("DROP DATABASE IF EXISTS {0};\r\n" +
                                    "CREATE DATABASE {0}", databaseName);

            using (var db = Db.FromConfig(MasterName))
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