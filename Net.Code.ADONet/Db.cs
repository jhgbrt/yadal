using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Net.Code.ADONet;

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
    public MappingConvention MappingConvention => Config.MappingConvention;

    private DbConnection _connection;
    private readonly bool _externalConnection;
    private ILogger _logger;

    /// <summary>
    /// Instantiate Db with existing connection. The connection is only used for creating commands;
    /// it should be disposed by the caller when done.
    /// </summary>
    /// <param name="connection">The existing connection</param>
    /// <param name="config"></param>
    public Db(DbConnection connection, DbConfig config, ILogger? logger = null)
    {
        _connection = connection;
        _externalConnection = true;
        _logger = logger ?? NullLogger.Instance;
        Config = config ?? DbConfig.Default;
    }

    /// <summary>
    /// Instantiate Db with connectionString and a custom IConnectionFactory
    /// </summary>
    /// <param name="connectionString">the connection string</param>
    /// <param name="providerFactory">the connection provider factory</param>
    public Db(string connectionString, DbProviderFactory providerFactory, ILogger? logger = null)
        : this(connectionString, DbConfig.FromProviderFactory(providerFactory), providerFactory, logger)
    {
    }

    /// <summary>
    /// Instantiate Db with connectionString and a custom IConnectionFactory
    /// </summary>
    /// <param name="connectionString">the connection string</param>
    /// <param name="config"></param>
    /// <param name="providerFactory">the connection factory</param>
    internal Db(string connectionString, DbConfig config, DbProviderFactory providerFactory, ILogger? logger = null)
    {
        _connection = providerFactory.CreateConnection();
        _connection.ConnectionString = connectionString;
        _externalConnection = false;
        _logger = logger ?? NullLogger.Instance;
        Config = config;
    }

    public void Connect()
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();
    }
    public void Disconnect()
    {
        if (_connection.State != ConnectionState.Closed)
            _connection.Close();
    }

    public async Task ConnectAsync()
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// The actual IDbConnection (which will be open)
    /// </summary>
    public DbConnection Connection
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
        if (_connection == null || _externalConnection) return;
        _connection.Dispose();
        _connection = null!;
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
        return new CommandBuilder(cmd, Config, _logger).OfType(commandType).WithCommandText(command);
    }

    /// <summary>
    /// Create a SQL command and execute it immediately (non query)
    /// </summary>
    /// <param name="command"></param>
    public int Execute(string command) => Sql(command).AsNonQuery();
}
