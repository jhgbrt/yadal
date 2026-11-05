using System.Threading;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Net.Code.ADONet;

/// <summary>
/// <para>Yet Another Data Access Layer</para>
/// <para>usage: </para>
/// <code>
/// await using var db = new Db(connectionString, SqlClientFactory.Instance);
/// var people = await db.Sql("SELECT * FROM Person WHERE Age > @age")
///     .WithParameters(new { age = 18 })
///     .AsEnumerableAsync&lt;Person&gt;()
///     .ToListAsync();
/// </code>
/// <para>
/// A Db instance wraps a single connection, which is opened when the first command is executed and
/// stays open until the Db is disposed. A Db instance is not thread-safe; create one per unit of work
/// and rely on connection pooling.
/// </para>
/// </summary>
public class Db : IDb
{
    internal DbConfig Config { get; }
    public MappingConvention MappingConvention => Config.MappingConvention;

    private DbConnection _connection;
    private readonly bool _externalConnection;
    private readonly ILogger _logger;

    /// <summary>
    /// Instantiate Db with existing connection. The connection is only used for creating commands;
    /// it should be disposed by the caller when done.
    /// </summary>
    /// <param name="connection">The existing connection</param>
    /// <param name="config"></param>
    /// <param name="logger"></param>
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
    /// <param name="logger"></param>
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
    /// <param name="logger"></param>
    internal Db(string connectionString, DbConfig config, DbProviderFactory providerFactory, ILogger? logger = null)
    {
        _connection = providerFactory.CreateConnection() ?? throw new InvalidOperationException($"{providerFactory.GetType()} did not create a connection");
        _connection.ConnectionString = connectionString;
        _externalConnection = false;
        _logger = logger ?? NullLogger.Instance;
        Config = config;
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Instantiate Db with a <see cref="DbDataSource"/> (e.g. NpgsqlDataSource). The Db creates (and owns)
    /// a connection from the data source. The database configuration is derived from the data source type.
    /// </summary>
    /// <param name="dataSource">the data source</param>
    /// <param name="logger"></param>
    public Db(DbDataSource dataSource, ILogger? logger = null)
        : this(dataSource, DbConfig.FromProviderName(dataSource.GetType().FullName ?? dataSource.GetType().Name), logger)
    {
    }

    /// <summary>
    /// Instantiate Db with a <see cref="DbDataSource"/> (e.g. NpgsqlDataSource). The Db creates (and owns)
    /// a connection from the data source.
    /// </summary>
    /// <param name="dataSource">the data source</param>
    /// <param name="config">the database configuration</param>
    /// <param name="logger"></param>
    public Db(DbDataSource dataSource, DbConfig config, ILogger? logger = null)
    {
        _connection = dataSource.CreateConnection();
        _externalConnection = false;
        _logger = logger ?? NullLogger.Instance;
        Config = config ?? DbConfig.Default;
    }
#endif

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

    public Task ConnectAsync() => ConnectAsync(CancellationToken.None);

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The actual DbConnection (which will be opened, synchronously, if it is not open yet).
    /// Call <see cref="ConnectAsync()"/> first to open it asynchronously.
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

    public async ValueTask DisposeAsync()
    {
        if (_connection == null || _externalConnection) return;
        await _connection.DisposeAsync().ConfigureAwait(false);
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
        // the connection is opened (synchronously or asynchronously) when the command is executed
        var cmd = _connection.CreateCommand();
        Config.PrepareCommand(cmd);
        return new CommandBuilder(cmd, Config, _logger).OfType(commandType).WithCommandText(command);
    }

    /// <summary>
    /// Create a SQL command and execute it immediately (non query)
    /// </summary>
    /// <param name="command"></param>
    public int Execute(string command) => Sql(command).AsNonQuery();

    /// <summary>
    /// Executes the same SQL statement once for each parameter set. Uses <see cref="DbBatch"/> when the
    /// provider supports it (one round trip per <paramref name="batchSize"/> statements); otherwise reuses
    /// a single command.
    /// </summary>
    internal void ExecuteForEach(string sql, IEnumerable<IEnumerable<(string name, object? value)>> parameterSets, int batchSize = DefaultBatchSize)
    {
#if NET8_0_OR_GREATER
        if (CanCreateBatch)
        {
            Connect();
            foreach (var chunk in parameterSets.Chunk(batchSize))
            {
                using var batch = CreateBatch(sql, chunk);
                batch.ExecuteNonQuery();
            }
            return;
        }
#endif
        using var commandBuilder = Sql(sql);
        foreach (var parameters in parameterSets)
        {
            foreach (var (name, value) in parameters)
                commandBuilder.WithParameter(name, value);
            commandBuilder.AsNonQuery();
        }
    }

    /// <inheritdoc cref="ExecuteForEach"/>
    internal async Task ExecuteForEachAsync(string sql, IEnumerable<IEnumerable<(string name, object? value)>> parameterSets, CancellationToken cancellationToken, int batchSize = DefaultBatchSize)
    {
#if NET8_0_OR_GREATER
        if (CanCreateBatch)
        {
            await ConnectAsync(cancellationToken).ConfigureAwait(false);
            foreach (var chunk in parameterSets.Chunk(batchSize))
            {
                var batch = CreateBatch(sql, chunk);
                await using var _ = batch.ConfigureAwait(false);
                await batch.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            return;
        }
#endif
        using var commandBuilder = Sql(sql);
        foreach (var parameters in parameterSets)
        {
            foreach (var (name, value) in parameters)
                commandBuilder.WithParameter(name, value);
            await commandBuilder.AsNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    internal const int DefaultBatchSize = 100;

#if NET8_0_OR_GREATER
    private bool CanCreateBatch => Config.HasNoCommandPreparation && _connection.CanCreateBatch;

    private DbBatch CreateBatch(string sql, IEnumerable<IEnumerable<(string name, object? value)>> parameterSets)
    {
        var batch = _connection.CreateBatch();
        DbCommand? parameterFactory = null;
        try
        {
            foreach (var parameters in parameterSets)
            {
                var batchCommand = batch.CreateBatchCommand();
                batchCommand.CommandText = sql;
                batchCommand.CommandType = CommandType.Text;
                foreach (var (name, value) in parameters)
                {
                    var p = batchCommand.CanCreateParameter
                        ? batchCommand.CreateParameter()
                        : (parameterFactory ??= _connection.CreateCommand()).CreateParameter();
                    p.ParameterName = name;
                    p.Value = DBNullHelper.ToDb(value);
                    batchCommand.Parameters.Add(p);
                }
                batch.BatchCommands.Add(batchCommand);
            }
            Logger.LogBatch(_logger, sql, batch.BatchCommands.Count);
            return batch;
        }
        catch
        {
            batch.Dispose();
            throw;
        }
        finally
        {
            parameterFactory?.Dispose();
        }
    }
#endif
}
