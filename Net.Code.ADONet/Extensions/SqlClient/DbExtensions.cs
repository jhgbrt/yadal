using Microsoft.Data.SqlClient;

namespace Net.Code.ADONet.Extensions.SqlClient;

public static class DbExtensions
{
    /// <summary>
    /// Adds a table-valued parameter. Only supported on SQL Server (System.Data.SqlClient)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="commandBuilder"></param>
    /// <param name="name">parameter name</param>
    /// <param name="values">list of values</param>
    /// <param name="udtTypeName">name of the user-defined table type</param>
    public static CommandBuilder WithParameter<T>(this CommandBuilder commandBuilder, string name, IEnumerable<T> values, string udtTypeName)
    {
        var dataTable = values.ToDataTable();

        var p = new SqlParameter(name, SqlDbType.Structured)
        {
            TypeName = udtTypeName,
            Value = dataTable
        };

        return commandBuilder.WithParameter(p);
    }

    /// <summary>
    /// Assumes one to one mapping between
    /// - tablename and typename
    /// - property names and column names
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="db"></param>
    /// <param name="items"></param>
    public static void BulkCopy<T>(this IDb db, IEnumerable<T> items)
    {
        using var bcp = new SqlBulkCopy(db.ConnectionString)
        {
            DestinationTableName = typeof(T).Name
        };

        // by default, SqlBulkCopy assumes columns in the database 
        // are in same order as the columns of the source data reader
        // => add explicit column mappings by name
        foreach (var p in typeof(T).GetProperties())
        {
            bcp.ColumnMappings.Add(p.Name, p.Name);
        }

        var datareader = items.AsDataReader();
        bcp.WriteToServer(datareader);
    }
}