using System.Linq;
using Npgsql;

namespace Net.Code.ADONet.Tests.Integration.Databases
{
    public class PostgreSqlDb : BaseDb<PostgreSqlDb>
    {
        public PostgreSqlDb() : base(NpgsqlFactory.Instance)
        {
        }

        public override void DropAndRecreate()
        {
            var databaseName = GetConnectionStringProperty("Database");

            using (var db = MasterDb())
            {
                var dropped = db.Execute($"DROP DATABASE IF EXISTS {databaseName};");
                var created = db.Execute($"CREATE DATABASE {databaseName};");
                var ok = db.Execute($"SELECT 1 from pg_database WHERE datname='{databaseName}';");
                var dbs = db.Sql("SELECT datname FROM pg_database").AsEnumerable().Select(d => (string)d.datname).ToList();
            }
        }
        public override Data.Person Project(dynamic d)
        {
            return new Data.Person
            {
                Id = d.id,
                Email = d.email,
                Name = d.name,
                OptionalNumber = d.optional_number,
                RequiredNumber = d.required_number
            };
        }
    }
}