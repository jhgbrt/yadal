using System.Collections.Generic;

using Microsoft.Data.SqlClient;

using Net.Code.ADONet.Extensions.SqlClient;
using Net.Code.ADONet.Tests.Integration.Data;

namespace Net.Code.ADONet.Tests.Integration.Databases
{
    public class SqlServerDb : BaseDb<SqlServerDb>
    {
        public SqlServerDb() :
            base(SqlClientFactory.Instance)
        {
        }

        public override IReadOnlyDictionary<string, string> AlternateConnectionStrings
            => new Dictionary<string, string>
            {
                ["SqlServer"] = @$"Data Source=(localdb)\mssqllocaldb;Initial Catalog={Configuration.DatabaseName};Integrated Security=True;Encrypt=false",
                ["SqlServerMaster"] = @$"Data Source=(localdb)\mssqllocaldb;Initial Catalog=master;Integrated Security=True;Encrypt=false",
            };

        public override IEnumerable<string> GetDropAndRecreateDdl()
        {
            var databaseName = Configuration.GetConnectionStringProperty(Name, "Initial Catalog");

            var ddl = $"""
                if exists (SELECT * FROM sys.databases WHERE Name = '{databaseName}') 
                begin
                	exec msdb.dbo.sp_delete_database_backuphistory '{databaseName}'
                	alter database {databaseName} SET  SINGLE_USER WITH ROLLBACK IMMEDIATE
                	drop database {databaseName}
                end
                create database {databaseName}
                """;

            yield return ddl;
        }

        public override IEnumerable<string> GetAfterInitSql()
        {
            yield return "CREATE TYPE IdSet AS TABLE (Id int)";
        }

        public override bool SupportsTableValuedParameters => true;

        public override bool SupportsBulkInsert => true;
    }
}