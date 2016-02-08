using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Net.Code.ADONet.Extensions.SqlClient
{
    public static class SqlServer
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

            commandBuilder.Command.Parameters.Add(p);
            return commandBuilder;
        }

        /// <summary>
        /// Assumes on to one mapping between 
        /// - tablename and typename 
        /// - property names and column names
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="db"></param>
        /// <param name="items"></param>
        public static void BulkInsert<T>(this IDb db, IEnumerable<T> items)
        {
            using (var bcp = new SqlBulkCopy(db.ConnectionString))
            {
                bcp.DestinationTableName = typeof (T).Name;

                // by default, SqlBulkCopy assumes columns in the database 
                // are in same order as the columns of the source data reader
                // => add explicit column mappings by name
                foreach (var p in typeof (T).GetProperties())
                {
                    bcp.ColumnMappings.Add(p.Name, p.Name);
                }

                var dataTable = items.AsDataReader();
                bcp.WriteToServer(dataTable);
            }
        }
    }
}
