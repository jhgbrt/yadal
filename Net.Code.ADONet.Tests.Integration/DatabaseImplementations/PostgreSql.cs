using System;
using System.Configuration;
using MySql.Data.MySqlClient;

namespace Net.Code.ADONet.Tests.Integration
{
    public class PostgreSql : BaseDb
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

        protected override Type ProviderType => typeof(MySqlClientFactory);

        public override bool SupportsTableValuedParameters => false;

    }
}