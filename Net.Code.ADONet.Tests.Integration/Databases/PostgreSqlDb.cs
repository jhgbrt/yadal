using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Npgsql;

namespace Net.Code.ADONet.Tests.Integration.Databases
{
    public class PostgreSqlDb : BaseDb<PostgreSqlDb>
    {
        public PostgreSqlDb() : base(NpgsqlFactory.Instance)
        {
        }

        public override IEnumerable<string> GetDropAndRecreateDdl()
        {
            var databaseName = Configuration.GetConnectionStringProperty(Name, "Database");

            yield return $"DROP DATABASE IF EXISTS {databaseName};";
            yield return $"CREATE DATABASE {databaseName};";
        }
       
    }
}