using System;
using System.Configuration;
using System.Data;

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
        private readonly DbConfig _config;

        public string ProviderName => _providerName;

        public IDbConfigurationBuilder Configure() => new DbConfigurationBuilder(_config);

        private DbConfigurationBuilder ConfigurePriv() => new DbConfigurationBuilder();

        /// <summary>
        /// The default DbProvider name is "System.Data.SqlClient" (for sql server).
        /// </summary>
        public static string DefaultProviderName = "System.Data.SqlClient";

        private readonly string _connectionString;
        private Lazy<IDbConnection> _connection;
        private readonly IConnectionFactory _connectionFactory;
        private readonly IDbConnection _externalConnection;
        private readonly string _providerName;

        /// <summary>
        /// Instantiate Db with existing connection. The connection is only used for creating commands; 
        /// it should be disposed by the caller when done.
        /// </summary>
        /// <param name="connection">The existing connection</param>
        /// <param name="providerName">The ADO .Net Provider name. When not specified, the default 
        /// value is used (see DefaultProviderName)</param>
        public Db(IDbConnection connection, string providerName = null)
        {
            _externalConnection = connection;
            _config = ConfigurePriv().FromProviderName(providerName).Config;
        }

        /// <summary>
        /// Instantiate Db with connectionString and DbProviderName
        /// </summary>
        /// <param name="connectionString">The connection string</param>
        /// <param name="providerName">The ADO .Net Provider name. When not specified, 
        /// the default value is used (see DefaultProviderName)</param>
        public Db(string connectionString, string providerName = null)
            : this(connectionString, new AdoNetProviderFactory(providerName ?? DefaultProviderName), providerName ?? DefaultProviderName)
        {
        }

        /// <summary>
        /// Instantiate Db with connectionString and a custom IConnectionFactory
        /// </summary>
        /// <param name="connectionString">the connection string</param>
        /// <param name="connectionFactory">the connection factory</param>
        /// <param name="providerName"></param>
        public Db(string connectionString, IConnectionFactory connectionFactory, string providerName = null)
        {
            _connectionString = connectionString;
            _connectionFactory = connectionFactory;
            _connection = new Lazy<IDbConnection>(CreateConnection);
            _providerName = providerName ?? DefaultProviderName;
            _config = ConfigurePriv().FromProviderName(_providerName).Config;
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
        public static Db FromConfig(string connectionStringName) => FromConfig(ConfigurationManager.ConnectionStrings[connectionStringName]);

        private static Db FromConfig(ConnectionStringSettings connectionStringSettings)
        {
            var connectionString = connectionStringSettings.ConnectionString;
            var providerName = !string.IsNullOrEmpty(connectionStringSettings.ProviderName)
                ? connectionStringSettings.ProviderName
                : DefaultProviderName;
            return new Db(connectionString, providerName);
        }

        public void Connect()
        {
            if (Connection.State != ConnectionState.Open)
                Connection.Open();
        }

        /// <summary>
        /// The actual IDbConnection (which will be open)
        /// </summary>
        public IDbConnection Connection
        {
            get
            {
                var dbConnection = _externalConnection ?? _connection.Value;
                return dbConnection;
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
        /// Create a Stored Procedure command
        /// </summary>
        /// <param name="sprocName">name of the sproc</param>
        /// <returns>a CommandBuilder instance</returns>
        public CommandBuilder StoredProcedure(string sprocName) => CreateCommand(CommandType.StoredProcedure, sprocName);

        private CommandBuilder CreateCommand(CommandType commandType, string command)
        {
            var cmd = Connection.CreateCommand();
            _config.PrepareCommand(cmd);
            return new CommandBuilder(cmd, _config.MappingConvention, _providerName).OfType(commandType).WithCommandText(command);
        }

        /// <summary>
        /// Create a SQL command and execute it immediately (non query)
        /// </summary>
        /// <param name="command"></param>
        public int Execute(string command) => Sql(command).AsNonQuery();
    }
}
