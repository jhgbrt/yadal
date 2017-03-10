using System;
using System.Data;
using System.Data.Common;
#if !NETSTANDARD1_6
using System.Configuration;
#endif

namespace Net.Code.ADONet
{
    /// <summary>
    /// <para> Yet Another Data Access Layer</para>
    /// <para>usage: </para>
    /// <para>using (var db = new Db()) {};                                 </para>
    /// <para>using (var db = new Db(connectionString)) {};                 </para>
    /// <para>using (var db = new Db(connectionString, providerName)) {};   </para>
    /// <para>using (var db = Db.FromConfig());                             </para>
    /// <para>using (var db = Db.FromConfig(connectionStringName));         </para>
    /// <para>
    /// from there it should be discoverable.
    /// inline SQL FTW!
    /// </para>
    /// </summary>
    public class Db : IDb
    {
        internal DbConfig Config { get; }
        internal IMappingConvention MappingConvention => Config.MappingConvention;
        public string ProviderName => Config.ProviderName;

        private readonly string _connectionString;
        private Lazy<IDbConnection> _connection;
        private readonly DbProviderFactory _connectionFactory;
        private readonly IDbConnection _externalConnection;

        /// <summary>
        /// Instantiate Db with existing connection. The connection is only used for creating commands; 
        /// it should be disposed by the caller when done.
        /// </summary>
        /// <param name="connection">The existing connection</param>
        /// <param name="config"></param>
        public Db(IDbConnection connection, DbConfig config)
        {
            _externalConnection = connection;
            Config = config ?? DbConfig.Default;
        }

#if !NETSTANDARD1_6
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

        /// <summary>
        /// Factory method, instantiating the Db class from the first connectionstring 
        /// in the app.config or web.config file.
        /// </summary>
        /// <returns>Db</returns>
        public static Db FromConfig() => FromConfig(ConfigurationManager.ConnectionStrings[0]);

        /// <summary>
        /// Factory method, instantiating the Db class from a named connectionstring 
        /// in the app.config or web.config file.
        /// </summary>
        public static Db FromConfig(string connectionStringName)
            => FromConfig(ConfigurationManager.ConnectionStrings[connectionStringName]);

        private static Db FromConfig(ConnectionStringSettings connectionStringSettings)
        {
            var connectionString = connectionStringSettings.ConnectionString;
            var providerName = connectionStringSettings.ProviderName;
            return new Db(connectionString, providerName);
        }
#endif

        /// <summary>
        /// Instantiate Db with connectionString and a custom IConnectionFactory
        /// </summary>
        /// <param name="connectionString">the connection string</param>
        /// <param name="config"></param>
        /// <param name="connectionFactory">the connection factory</param>
        internal Db(string connectionString, DbConfig config, DbProviderFactory connectionFactory)
        {
            _connectionString = connectionString;
            _connectionFactory = connectionFactory;
            _connection = new Lazy<IDbConnection>(CreateConnection);
            Config = config;
        }

        public void Connect()
        {
            var connection = _externalConnection ?? _connection.Value;
            if (connection.State != ConnectionState.Open)
                connection.Open();
        }

        /// <summary>
        /// The actual IDbConnection (which will be open)
        /// </summary>
        public IDbConnection Connection
        {
            get
            {
                Connect();
                return _externalConnection ?? _connection.Value;
            }
        }

        public string ConnectionString => _connectionString;

        private IDbConnection CreateConnection() => _connectionFactory.CreateConnection(_connectionString);

        public void Dispose()
        {
            if (_connection == null || !_connection.IsValueCreated) return;
            _connection.Value.Dispose();
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
