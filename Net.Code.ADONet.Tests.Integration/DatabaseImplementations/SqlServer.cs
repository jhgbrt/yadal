using System;
using System.Configuration;
using System.Data.SqlClient;

namespace Net.Code.ADONet.Tests.Integration
{
    public class SqlServer : BaseDb {
        public override void DropAndRecreate()
        {
            var connectionString = ConfigurationManager.ConnectionStrings[Name].ConnectionString;
            var connectionStringBuilder = new SqlConnectionStringBuilder
            {
                ConnectionString = connectionString
            };
            var databaseName = connectionStringBuilder.InitialCatalog;

            var ddl = string.Format("if exists (SELECT * FROM sys.databases WHERE Name = \'{0}\') \r\n" +
                                    "begin\r\n" +
                                    "\texec msdb.dbo.sp_delete_database_backuphistory \'{0}\'\r\n" +
                                    "\talter database {0} SET  SINGLE_USER WITH ROLLBACK IMMEDIATE\r\n" +
                                    "\tdrop database {0}\r\n" +
                                    "end\r\n" +
                                    "create database {0}\r\n", databaseName);

            using (var db = Db.FromConfig(MasterName))
            {
                db.Execute(ddl);
            }

            using (var db = Db.FromConfig(Name))
            {
                db.Execute("CREATE TYPE IdSet AS TABLE (Id int)");
            }

        }

        protected override Type ProviderType => typeof (SqlClientFactory);

        public override bool SupportsTableValuedParameters => true;

    }
}