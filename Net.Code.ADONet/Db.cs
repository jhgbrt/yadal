
using System;
using System.Data;
using System.Data.Common;

namespace Net.Code.ADONet
{

    /// <summary>
    /// <para>Yet Another Data Access Layer</para>
    /// <para>usage: </para>
    /// <para>using (var db = new Db(connectionString, providerFactory)) {};</para>
    /// <para>
    /// from there it should be discoverable.
    /// inline SQL FTW!
    /// </para>
    /// </summary>
    public class Db : IDb
    {
        internal DbConfig Config { get; }
        internal IMappingConvention MappingConvention => Config.MappingConvention;

        private IDbConnection _connection;
        private readonly bool _externalConnection;

        /// <summary>
        /// Instantiate Db with existing connection. The connection is only used for creating commands; 
        /// it should be disposed by the caller when done.
        /// </summary>
        /// <param name="connection">The existing connection</param>
        /// <param name="config"></param>
        public Db(IDbConnection connection, DbConfig config)
        {
            _connection = connection;
            _externalConnection = true;
            Config = config ?? DbConfig.Default;
        }

#if NETFRAMEWORK
        /// <summary>
        /// Instantiate Db with connectionString and DbProviderName
        /// </summary>
        /// <param name="connectionString">The connection string</param>
        /// <param name="providerName">The ADO .Net Provider name. When not specified, 
        /// the default value is used (see DefaultProviderName)</param>
        public Db(string connectionString, string providerName)
            : this(connectionString, DbConfig.FromProviderName(providerName), DbProviderFactories.GetFactory(providerName))
        {
        }

        [Obsolete("Use DbFactory.FromConfig", true)]
        public static Db FromConfig() => DbFactory.FromConfig();
        [Obsolete("Use DbFactory.FromConfig", true)]
        public static Db FromConfig(string connectionStringName)
            => DbFactory.FromConfig(connectionStringName);
#endif

        /// <summary>
        /// Instantiate Db with connectionString and a custom IConnectionFactory
        /// </summary>
        /// <param name="connectionString">the connection string</param>
        /// <param name="providerFactory">the connection provider factory</param>
        public Db(string connectionString, DbProviderFactory providerFactory)
            : this(connectionString, DbConfig.FromProviderFactory(providerFactory), providerFactory)
        {
        }


        /// <summary>
        /// Instantiate Db with connectionString and a custom IConnectionFactory
        /// </summary>
        /// <param name="connectionString">the connection string</param>
        /// <param name="config"></param>
        /// <param name="connectionFactory">the connection factory</param>
        internal Db(string connectionString, DbConfig config, DbProviderFactory connectionFactory)
        {
            Logger.Log("Db ctor");
            _connection = connectionFactory.CreateConnection(connectionString);
            _externalConnection = false;
            Config = config;
        }

        public void Connect()
        {
            Logger.Log("Db connect");
            if (_connection.State != ConnectionState.Open)
                _connection.Open();
        }

        /// <summary>
        /// The actual IDbConnection (which will be open)
        /// </summary>
        public IDbConnection Connection
        {
            get
            {
                Connect();
                return _connection;
            }
        }

        public string ConnectionString => _connection.ConnectionString;

        public void Dispose()
        {
            Logger.Log("Db dispose");
            if (_connection == null || _externalConnection) return;
            _connection.Dispose();
            _connection = null;
        }

        /// <summary>
        /// Create a SQL query command builder
        /// </summary>
        /// <param name="sqlQuery"></param>
        /// <returns>a CommandBuilder instance</returns>
        public CommandBuilder Sql(string sqlQuery) => CreateCommand(CommandType.Text, sqlQuery);

        /// <summary>
        /// Create a Stored Procedure command builder
        /// </summary>
        /// <param name="sprocName">name of the sproc</param>
        /// <returns>a CommandBuilder instance</returns>
        public CommandBuilder StoredProcedure(string sprocName) => CreateCommand(CommandType.StoredProcedure, sprocName);

        private CommandBuilder CreateCommand(CommandType commandType, string command)
        {
            var cmd = Connection.CreateCommand();
            Config.PrepareCommand(cmd);
            return new CommandBuilder(cmd, Config).OfType(commandType).WithCommandText(command);
        }

        /// <summary>
        /// Create a SQL command and execute it immediately (non query)
        /// </summary>
        /// <param name="command"></param>
        public int Execute(string command) => Sql(command).AsNonQuery();
    }
}
