using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Net.Code.ADONet;

public partial class CommandBuilder(DbCommand command, DbConfig config, ILogger? logger = null) : IDisposable
{
    /// <summary>
    /// Sets the command text
    /// </summary>
    /// <param name="text"></param>
    public CommandBuilder WithCommandText(string text)
    {
        Command.CommandText = text;
        return this;
    }

    /// <summary>
    /// Sets the command type
    /// </summary>
    /// <param name="type"></param>
    public CommandBuilder OfType(CommandType type)
    {
        Command.CommandType = type;
        return this;
    }

    /// <summary>
    /// Adds a parameter for each property of the given object, with the property name as the name
    /// of the parameter and the property value as the corresponding parameter value
    /// </summary>
    /// <param name="parameters"></param>
    [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
    [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
    public CommandBuilder WithParameters<T>(T parameters)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        var runtimeType = parameters.GetType();
        if (runtimeType == typeof(T))
        {
            foreach (var getter in FastReflection<T>.Instance.GetGettersForType())
            {
                WithParameter(getter.Key, getter.Value(parameters));
            }
        }
        else
        {
            // the static type T differs from the runtime type (e.g. T is object or a base class):
            // use the properties and getters of the runtime type
            foreach (var (name, getter) in FastReflection.GetGetters(runtimeType))
            {
                WithParameter(name, getter(parameters));
            }
        }
        return this;
    }

    internal CommandBuilder WithKey(DbKey key)
    {
        foreach (var v in key.values)
        {
            WithParameter(v.name, v.value);
        }
        return this;
    }

    /// <summary>
    /// Builder method - sets the command timeout. The timeout is rounded up to whole seconds;
    /// <see cref="TimeSpan.Zero"/> means no timeout.
    /// </summary>
    /// <param name="timeout"></param>
    public CommandBuilder WithTimeout(TimeSpan timeout)
    {
        if (timeout < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The timeout must not be negative");
        // ADO.NET timeouts are whole seconds, and 0 means 'wait indefinitely':
        // round up, so that a sub-second timeout does not silently become 'no timeout'
        Command.CommandTimeout = checked((int)Math.Ceiling(timeout.TotalSeconds));
        return this;
    }

    /// <summary>
    /// Builder method - adds a name/value pair as parameter
    /// </summary>
    /// <param name="name">the parameter name</param>
    /// <param name="value">the parameter value</param>
    /// <returns>the same CommandBuilder instance</returns>
    public CommandBuilder WithParameter(string name, object? value)
    {
        GetOrAddParameter(name).Value = DBNullHelper.ToDb(value);
        return this;
    }

    /// <summary>
    /// Builder method - adds a name/value pair as parameter, with an explicit database type and (optionally) size.
    /// Setting the type avoids relying on the provider's type inference; setting a size for variable-length
    /// types (strings, binary) lets the database reuse query plans across different value lengths.
    /// </summary>
    /// <param name="name">the parameter name</param>
    /// <param name="value">the parameter value</param>
    /// <param name="dbType">the database type of the parameter</param>
    /// <param name="size">the size of the parameter (for variable-length types)</param>
    /// <returns>the same CommandBuilder instance</returns>
    public CommandBuilder WithParameter(string name, object? value, DbType dbType, int? size = null)
    {
        var p = GetOrAddParameter(name);
        p.DbType = dbType;
        if (size.HasValue)
            p.Size = size.Value;
        p.Value = DBNullHelper.ToDb(value);
        return this;
    }

    private IDbDataParameter GetOrAddParameter(string name)
    {
        if (Command.Parameters.Contains(name))
            return Command.Parameters[name];
        var p = Command.CreateParameter();
        p.ParameterName = name;
        Command.Parameters.Add(p);
        return p;
    }

    public CommandBuilder WithParameter<T>(T p) where T : IDbDataParameter
    {
        Command.Parameters.Add(p);
        return this;
    }

    public CommandBuilder InTransaction(DbTransaction tx)
    {
        Command.Transaction = tx;
        return this;
    }

    /// <summary>
    /// The raw IDbCommand instance
    /// </summary>
    public DbCommand Command => command;

    /// <summary>
    /// Executes the query and returns the result as a list of dynamic objects.
    /// </summary>
    [RequiresDynamicCode(Trimming.DynamicMessage)]
    public IEnumerable<dynamic> AsEnumerable()
    {
        using var reader = Execute.Reader();
        while (reader.Read()) yield return reader.ToExpando();
    }

    /// <summary>
    /// Executes the query and returns the result as a list of [T]. This method is slightly faster.
    /// than doing AsEnumerable().Select(selector). The selector is required to map objects as the
    /// underlying datareader is enumerated.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selector">mapping function that transforms a datarecord (wrapped as a dynamic object) to an instance of type [T]</param>
    [RequiresDynamicCode(Trimming.DynamicMessage)]
    public IEnumerable<T> AsEnumerable<T>(Func<dynamic, T> selector) => Select(selector);

    /// <summary>
    /// Executes the query and returns the result as a list of [T]. This method is slightly faster.
    /// than doing AsEnumerable().Select(selector). The selector is required to map objects as the
    /// underlying datareader is enumerated.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selector">mapping function that transforms a datarecord to an instance of type [T]</param>
    public IEnumerable<T> AsEnumerable<T>(Func<IDataRecord, T> selector) => Select(selector);

    /// <summary>
    /// Executes the query and returns the result as a list of [T]. Columns are mapped to properties
    /// by name, using the mapping convention of the database (exact match first, then case-insensitive),
    /// or to the parameters of a single 'record-like' constructor.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
    [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
    public IEnumerable<T> AsEnumerable<T>()
    {
        using var reader = Execute.Reader();
        var mapper = reader.GetMapper<T>(config);
        while (reader.Read())
            yield return mapper(reader);
    }

    // enables linq 'select' syntax
    [RequiresDynamicCode(Trimming.DynamicMessage)]
    public IEnumerable<T> Select<T>(Func<dynamic, T> selector)
    {
        using var reader = Execute.Reader();
        while (reader.Read())
            yield return selector(Dynamic.From(reader));
    }

    // enables linq 'select' syntax
    public IEnumerable<T> Select<T>(Func<IDataRecord, T> selector)
    {
        using var reader = Execute.Reader();
        while (reader.Read())
            yield return selector(reader);
    }


    /// <summary>
    /// Executes the query and returns the result as a list of lists
    /// </summary>
    [RequiresDynamicCode(Trimming.DynamicMessage)]
    public IEnumerable<IReadOnlyCollection<dynamic>> AsMultiResultSet()
    {
        using var reader = Execute.Reader();
        do
        {
            var list = new Collection<dynamic>();
            while (reader.Read()) list.Add(reader.ToExpando());
            yield return list;
        } while (reader.NextResult());
    }
    /// <summary>
    /// Executes the query and returns the result as a tuple of lists
    /// </summary>
    [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
    [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
    public (IReadOnlyCollection<T1>, IReadOnlyCollection<T2>) AsMultiResultSet<T1, T2>()
    {
        using var reader = Execute.Reader();
        return (
            GetResultSet<T1>(reader, config),
            GetResultSet<T2>(reader, config)
            );
    }

    /// <summary>
    /// Executes the query and returns the result as a tuple of lists
    /// </summary>
    [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
    [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
    public (IReadOnlyCollection<T1>, IReadOnlyCollection<T2>, IReadOnlyCollection<T3>) AsMultiResultSet<T1, T2, T3>()
    {
        using var reader = Execute.Reader();
        return (
            GetResultSet<T1>(reader, config),
            GetResultSet<T2>(reader, config),
            GetResultSet<T3>(reader, config)
            );
    }
    /// <summary>
    /// Executes the query and returns the result as a tuple of lists
    /// </summary>
    [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
    [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
    public (IReadOnlyCollection<T1>, IReadOnlyCollection<T2>, IReadOnlyCollection<T3>, IReadOnlyCollection<T4>) AsMultiResultSet<T1, T2, T3, T4>()
    {
        using var reader = Execute.Reader();
        return (
            GetResultSet<T1>(reader, config),
            GetResultSet<T2>(reader, config),
            GetResultSet<T3>(reader, config),
            GetResultSet<T4>(reader, config)
            );
    }
    /// <summary>
    /// Executes the query and returns the result as a tuple of lists
    /// </summary>
    [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
    [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
    public (IReadOnlyCollection<T1>, IReadOnlyCollection<T2>, IReadOnlyCollection<T3>, IReadOnlyCollection<T4>, IReadOnlyCollection<T5>) AsMultiResultSet<T1, T2, T3, T4, T5>()
    {
        using var reader = Execute.Reader();
        return (
            GetResultSet<T1>(reader, config),
            GetResultSet<T2>(reader, config),
            GetResultSet<T3>(reader, config),
            GetResultSet<T4>(reader, config),
            GetResultSet<T5>(reader, config)
            );
    }

    [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
    [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
    private static IReadOnlyCollection<T> GetResultSet<T>(IDataReader reader, DbConfig config)
    {
        var list = new List<T>();
        var map = reader.GetMapper<T>(config);
        while (reader.Read()) list.Add(map(reader));
        reader.NextResult();
        return list;
    }

    /// <summary>
    /// Executes the command, returning the first column of the first result, converted to the type T
    /// </summary>
    /// <typeparam name="T">return type</typeparam>
    public T? AsScalar<T>() => ConvertTo<T>.From(AsScalar());

    public object? AsScalar() => Execute.Scalar();

    /// <summary>
    /// Executes the command as a SQL statement, returning the number of rows affected
    /// </summary>
    public int AsNonQuery() => Execute.NonQuery();

    /// <summary>
    /// Executes the command as a datareader. Use this if you need best performance.
    /// The returned reader is a <see cref="DbDataReader"/>.
    /// </summary>
    public IDataReader AsReader() => Execute.Reader();

    [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
    [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
    public T Single<T>() => AsEnumerable<T>().Single();

    private Executor Execute => new(Command, logger ?? NullLogger.Instance);

    /// <summary>
    /// Executes the command as a statement, returning the number of rows affected asynchronously
    /// </summary>
    public Task<int> AsNonQueryAsync() => AsNonQueryAsync(CancellationToken.None);

    /// <summary>
    /// Executes the command as a statement, returning the number of rows affected asynchronously
    /// </summary>
    public Task<int> AsNonQueryAsync(CancellationToken cancellationToken) => ExecuteAsync.NonQuery(cancellationToken);

    /// <summary>
    /// Executes the command, returning the first column of the first result, converted to the type T asynchronously.
    /// </summary>
    /// <typeparam name="T">return type</typeparam>
    public Task<T?> AsScalarAsync<T>() => AsScalarAsync<T>(CancellationToken.None);

    /// <summary>
    /// Executes the command, returning the first column of the first result, converted to the type T asynchronously.
    /// </summary>
    /// <typeparam name="T">return type</typeparam>
    public async Task<T?> AsScalarAsync<T>(CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync.Scalar(cancellationToken).ConfigureAwait(false);
        return ConvertTo<T>.From(result);
    }

    /// <summary>
    /// Executes the command as a datareader asynchronously. The caller is responsible for disposing the reader.
    /// </summary>
    public Task<DbDataReader> AsReaderAsync(CancellationToken cancellationToken = default) => ExecuteAsync.Reader(cancellationToken);

    /// <summary>
    /// Executes the query and returns the result as a list of dynamic objects asynchronously
    /// </summary>
    [RequiresDynamicCode(Trimming.DynamicMessage)]
    public IAsyncEnumerable<dynamic> AsEnumerableAsync() => AsEnumerableAsync(CancellationToken.None);

    /// <summary>
    /// Executes the query and returns the result as a list of dynamic objects asynchronously
    /// </summary>
    [RequiresDynamicCode(Trimming.DynamicMessage)]
    public async IAsyncEnumerable<dynamic> AsEnumerableAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var reader = await ExecuteAsync.Reader(cancellationToken).ConfigureAwait(false);
        await using var _ = reader.ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            yield return reader.ToExpando();
    }

    /// <summary>
    /// Executes the query and returns the result as a list of [T] asynchronously
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selector">mapping function that transforms a datarecord (wrapped as a dynamic object) to an instance of type [T]</param>
    [RequiresDynamicCode(Trimming.DynamicMessage)]
    public IAsyncEnumerable<T> AsEnumerableAsync<T>(Func<dynamic, T> selector) => AsEnumerableAsync(selector, CancellationToken.None);

    /// <summary>
    /// Executes the query and returns the result as a list of [T] asynchronously
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selector">mapping function that transforms a datarecord (wrapped as a dynamic object) to an instance of type [T]</param>
    /// <param name="cancellationToken"></param>
    [RequiresDynamicCode(Trimming.DynamicMessage)]
    public async IAsyncEnumerable<T> AsEnumerableAsync<T>(Func<dynamic, T> selector, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var reader = await ExecuteAsync.Reader(cancellationToken).ConfigureAwait(false);
        await using var _ = reader.ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            yield return selector(Dynamic.From(reader));
    }

    /// <summary>
    /// Executes the query and returns the result as a list of [T] asynchronously
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selector">mapping function that transforms a datarecord to an instance of type [T]</param>
    public IAsyncEnumerable<T> AsEnumerableAsync<T>(Func<IDataRecord, T> selector) => AsEnumerableAsync(selector, CancellationToken.None);

    /// <summary>
    /// Executes the query and returns the result as a list of [T] asynchronously
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selector">mapping function that transforms a datarecord to an instance of type [T]</param>
    /// <param name="cancellationToken"></param>
    public async IAsyncEnumerable<T> AsEnumerableAsync<T>(Func<IDataRecord, T> selector, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var reader = await ExecuteAsync.Reader(cancellationToken).ConfigureAwait(false);
        await using var _ = reader.ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            yield return selector(reader);
    }

    /// <summary>
    /// Executes the query and returns the result as a list of [T] asynchronously
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
    [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
    public IAsyncEnumerable<T> AsEnumerableAsync<T>() => AsEnumerableAsync<T>(CancellationToken.None);

    /// <summary>
    /// Executes the query and returns the result as a list of [T] asynchronously
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
    [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
    public async IAsyncEnumerable<T> AsEnumerableAsync<T>([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var reader = await ExecuteAsync.Reader(cancellationToken).ConfigureAwait(false);
        await using var _ = reader.ConfigureAwait(false);
        var map = reader.GetMapper<T>(config);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            yield return map(reader);
    }

    [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
    [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
    public ValueTask<T> SingleAsync<T>() => SingleAsync<T>(CancellationToken.None);

    [RequiresUnreferencedCode(Trimming.ReflectionMessage)]
    [RequiresDynamicCode(Trimming.DynamicCodeMessage)]
    public ValueTask<T> SingleAsync<T>(CancellationToken cancellationToken) => AsyncEnumerableHelpers.SingleAsync(AsEnumerableAsync<T>(cancellationToken), cancellationToken);

    public void Dispose() => Command.Dispose();

    private AsyncExecutor ExecuteAsync => new(Command, logger ?? NullLogger.Instance);
}
