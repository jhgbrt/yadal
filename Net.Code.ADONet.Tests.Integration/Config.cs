using System.Collections.Generic;

namespace Net.Code.ADONet.Tests.Integration
{
    public class Configuration
    {
        public const string DatabaseName = "TESTDB";
        const string Password = "P@ssword1!";

        public static readonly IReadOnlyDictionary<string, string> ConnectionStrings = new Dictionary<string, string>
        {
            ["SqlServer"] = @$"Data Source=localhost;Initial Catalog={DatabaseName};User Id=sa;Password={Password};Persist Security Info=True",
            ["SqlServerMaster"] = @$"Data Source=localhost;Initial Catalog=master;User Id=sa;Password={Password};Persist Security Info=True",
            ["Oracle"] = @$"Data Source=localhost:1521/XE;User ID={DatabaseName};Password={Password}",
            ["OracleMaster"] = $@"Data Source=localhost:1521/XE;DBA Privilege=SYSDBA;User Id=sys;Password={Password}",
            ["SqLite"] = @"Data Source=:memory:",
            ["SqLiteMaster"] = @"Data Source=:memory:",
            ["MySql"] = $@"Server=localhost;Database={DatabaseName};Uid=root;Pwd=root;",
            ["MySqlMaster"] = $@"Server=localhost;Uid=root;Pwd=root;",
            ["PostgreSql"] = $@"Server=localhost;Port=5432;Database={DatabaseName.ToLower()};User ID=postgres;Password={Password}",
            ["PostgreSqlMaster"] = $@"Server=localhost;Port=5432;User ID=postgres;Password={Password}",
            ["DB2"] = $@"Server=localhost:50000;Database={DatabaseName};UID=db2inst1;Password={Password}",
            ["DB2Master"] = $@"Server=localhost:50000;Database={DatabaseName};UID=db2inst1;Password={Password}"
        };
    }
}
