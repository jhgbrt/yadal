using System.Diagnostics.CodeAnalysis;
using System.Threading;

using Microsoft.Data.SqlClient;

namespace Net.Code.ADONet.Extensions.SqlClient;

public static class DbExtensions
{
    /// <param name="commandBuilder"></param>
    extension(CommandBuilder commandBuilder)
    {
        /// <summary>
        /// Adds a table-valued parameter. Only supported on SQL Server (System.Data.SqlClient)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">parameter name</param>
        /// <param name="values">list of values</param>
        /// <param name="udtTypeName">name of the user-defined table type</param>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public CommandBuilder WithParameter<T>(string name, IEnumerable<T> values, string udtTypeName)
        {
            var dataTable = values.ToDataTable();

            var p = new SqlParameter(name, SqlDbType.Structured)
            {
                TypeName = udtTypeName,
                Value = dataTable
            };

            return commandBuilder.WithParameter(p);
        }
    }

    /// <param name="db"></param>
    extension(IDb db)
    {
        /// <summary>
        /// Bulk copies the items to the table for type T, using the connection of the Db.
        /// Assumes one to one mapping between
        /// - tablename and typename
        /// - property names and column names
        /// (following the mapping convention of the Db)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public void BulkCopy<T>(IEnumerable<T> items) => db.BulkCopy(items, null);

        /// <summary>
        /// Bulk copies the items to the table for type T, using the connection of the Db, in the given transaction.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="transaction">the transaction (on the Db's connection) to use, or null</param>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public void BulkCopy<T>(IEnumerable<T> items, SqlTransaction? transaction)
        {
            using var bcp = GetBulkCopy<T>(db, GetSqlConnection(db.Connection), transaction);
            using var datareader = items.AsDataReader();
            bcp.WriteToServer(datareader);
        }

        /// <summary>
        /// Bulk copies the items to the table for type T, using the connection of the Db.
        /// Assumes one to one mapping between
        /// - tablename and typename
        /// - property names and column names
        /// (following the mapping convention of the Db)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public ValueTask BulkCopyAsync<T>(IEnumerable<T> items) => db.BulkCopyAsync(items, null, CancellationToken.None);

        /// <summary>
        /// Bulk copies the items to the table for type T, using the connection of the Db, in the given transaction.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="transaction">the transaction (on the Db's connection) to use, or null</param>
        /// <param name="cancellationToken"></param>
        [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
        [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
        public async ValueTask BulkCopyAsync<T>(IEnumerable<T> items, SqlTransaction? transaction, CancellationToken cancellationToken = default)
        {
            await db.ConnectAsync().ConfigureAwait(false);
            using var bcp = GetBulkCopy<T>(db, GetSqlConnection(db.Connection), transaction);
            var datareader = items.AsDataReader();
            await using var _ = datareader.ConfigureAwait(false);
            await bcp.WriteToServerAsync(datareader, cancellationToken).ConfigureAwait(false);
        }
    }

    private static SqlConnection GetSqlConnection(DbConnection connection)
        => connection as SqlConnection
        ?? throw new NotSupportedException($"BulkCopy requires a {typeof(SqlConnection).FullName}, but the connection is a {connection.GetType().FullName}");

    [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
    private static SqlBulkCopy GetBulkCopy<T>(IDb db, SqlConnection connection, SqlTransaction? transaction)
    {
        // Use the Db's own connection rather than a new connection from db.ConnectionString: once a
        // connection has been opened, SqlClient removes the password from its ConnectionString
        // (unless Persist Security Info=true), so a new connection would fail to log in.
        var bcp = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
        {
            DestinationTableName = typeof(T).GetTableName(db.MappingConvention)
        };

        // by default, SqlBulkCopy assumes columns in the database 
        // are in same order as the columns of the source data reader
        // => add explicit column mappings by name
        foreach (var p in typeof(T).GetProperties().Where(TypeExtensions.IsReadable))
        {
            bcp.ColumnMappings.Add(p.Name, p.GetColumnName(db.MappingConvention));
        }

        return bcp;
    }
}
