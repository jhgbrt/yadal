using System.Collections.Generic;
using System.Data.Common;

namespace Net.Code.ADONet.Tests.Integration
{
    public class Configuration
    {
        public const string DatabaseName = "TESTDB";
        const string Password = "P#ssword1!";

        public static readonly IReadOnlyDictionary<string, string> ConnectionStrings = new Dictionary<string, string>
        {
            ["SqlServer"] = @$"Data Source=127.0.0.1;Initial Catalog={DatabaseName};User Id=sa;Password={Password};Persist Security Info=True;Encrypt=false",
            ["SqlServerMaster"] = @$"Data Source=127.0.0.1;Initial Catalog=master;User Id=sa;Password={Password};Persist Security Info=True;Encrypt=false",
            ["Oracle"] = @$"Data Source=localhost:1521/FREE;User ID={DatabaseName};Password=P_ssw0rd_1",
            ["OracleMaster"] = $@"Data Source=localhost:1521/FREE;DBA Privilege=SYSDBA;User Id=SYS;Password={Password}",
            ["SqLite"] = @"Data Source=:memory:",
            ["SqLiteMaster"] = @"Data Source=:memory:",
            ["MySql"] = $@"Server=localhost;Database={DatabaseName};Uid=root;Pwd=root;",
            ["MySqlMaster"] = $@"Server=localhost;Uid=root;Pwd=root;",
            ["PostgreSql"] = $@"Server=localhost;Port=5432;Database={DatabaseName.ToLower()};User ID=postgres;Password={Password}",
            ["PostgreSqlMaster"] = $@"Server=localhost;Port=5432;User ID=postgres;Password={Password}",
            ["DB2"] = $@"Server=localhost:50000;Database={DatabaseName};UID=db2inst1;Password={Password};Connect Timeout=5",
            ["DB2Master"] = $@"Server=localhost:50000;Database={DatabaseName};UID=db2inst1;Password={Password};Connect Timeout=5"
        };

        public static string GetConnectionStringProperty(string name, string keyword)
        {
            var connectionString = Configuration.ConnectionStrings[name];
            var connectionStringBuilder = new DbConnectionStringBuilder();
            connectionStringBuilder.ConnectionString = connectionString;
            var value = (string)connectionStringBuilder[keyword];
            return value;
        }
    }
}
