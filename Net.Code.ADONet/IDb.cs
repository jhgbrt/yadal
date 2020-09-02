using System;
using System.Data.Common;

namespace Net.Code.ADONet
{
    public interface IDb : IDisposable
    {
        /// <summary>
        /// Open a connection to the database. Not required.
        /// </summary>
        void Connect();

#if NETCOREAPP
        /// <summary>
        /// Open a connection to the database. Not required.
        /// </summary>
        Task ConnectAsync() {Connect();return Task.FromResult(false); }
#endif
        /// <summary>
        /// The actual DbConnection (which will be open)
        /// </summary>
        DbConnection Connection { get; }
     
        /// <summary>
        /// The ADO.Net connection string
        /// </summary>
        string ConnectionString { get; }
        
        /// <summary>
        /// Create a SQL query command builder
        /// </summary>
        /// <param name="sqlQuery"></param>
        /// <returns>a CommandBuilder instance</returns>
        CommandBuilder Sql(string sqlQuery);
     
        /// <summary>
        /// Create a Stored Procedure command
        /// </summary>
        /// <param name="sprocName">name of the stored procedure</param>
        /// <returns>a CommandBuilder instance</returns>
        CommandBuilder StoredProcedure(string sprocName);
        
        /// <summary>
        /// Create a SQL command and execute it immediately (non query)
        /// </summary>
        /// <param name="command"></param>
        int Execute(string command);
    }
}