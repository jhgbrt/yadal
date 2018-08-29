using System;
using System.Configuration;
using System.Linq;
using MySql.Data.MySqlClient;
using Net.Code.ADONet.Tests.Integration.Data;

namespace Net.Code.ADONet.Tests.Integration.Databases
{
    public class PostgreSqlDb : BaseDb
    {
        public override void DropAndRecreate()
        {
            var connectionString = ConfigurationManager.ConnectionStrings[Name].ConnectionString;
            var connectionStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                ConnectionString = connectionString
            };
            var databaseName = connectionStringBuilder.Database;


            using (var db = Db.FromConfig(MasterName))
            {
                var dropped = db.Execute($"DROP DATABASE IF EXISTS {databaseName};");
                var created = db.Execute($"CREATE DATABASE {databaseName};");
                var ok = db.Execute($"SELECT 1 from pg_database WHERE datname='{databaseName}';");
                var dbs = db.Sql("SELECT datname FROM pg_database").AsEnumerable().Select(d => (string)d.datname).ToList();
            }

        }

        
    }
}