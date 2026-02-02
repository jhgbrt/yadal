using System.Collections.Generic;
using System.Data.Common;

using Net.Code.ADONet.Tests.Integration.Data;

namespace Net.Code.ADONet.Tests.Integration.Databases
{
    public interface IDatabaseImpl
    {
        IReadOnlyDictionary<string, string>? AlternateConnectionStrings { get; }
        string CreatePersonTable { get; }
        string DropPersonTable { get; }
        string DropProductTable { get; }
        string CreateAddressTable { get; }
        string CreateProductTable { get; }
        string DropAddressTable { get; }

        IEnumerable<string> GetAfterInitSql();
        IEnumerable<string> GetDropAndRecreateDdl();

        bool SupportsMultipleResultSets { get; }
        bool SupportsTableValuedParameters { get; }
        bool SupportsBulkInsert { get; }

        public string Name { get; }
        CommandBuilder CreateMultiResultSetCommand(IDb db, string query1, string query2);

        DbConfig Config { get; }
        DbProviderFactory Factory { get; }
    }
}