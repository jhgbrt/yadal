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

        public override IEnumerable<string> GetDropAndRecreateDdl()
        {
            var databaseName = Configuration.GetConnectionStringProperty(Name, "Database");

            var ddl = $"""
                DROP DATABASE IF EXISTS {databaseName};
                CREATE DATABASE {databaseName}
                """;
            yield return ddl;
        }
    }
}