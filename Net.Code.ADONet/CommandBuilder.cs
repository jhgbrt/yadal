namespace Net.Code.ADONet;

public partial class CommandBuilder
{
    private readonly DbConfig _config;

    public CommandBuilder(DbCommand command, DbConfig config)
    {
        Command = command;
        _config = config;
    }

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
    public CommandBuilder WithParameters<T>(T parameters)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));
        var getters = FastReflection<T>.Instance.GetGettersForType();
        var props = parameters.GetType().GetProperties();
        foreach (var property in props)
        {
            WithParameter(property.Name, getters[property.Name](parameters));
        }
        return this;
    }

    /// <summary>
    /// Builder method - sets the command timeout
    /// </summary>
    /// <param name="timeout"></param>
    public CommandBuilder WithTimeout(TimeSpan timeout)
    {
        Command.CommandTimeout = (int)timeout.TotalSeconds;
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
        IDbDataParameter p;
        if (Command.Parameters.Contains(name))
        {
            p = Command.Parameters[name];
            p.Value = DBNullHelper.ToDb(value);
        }
        else
        {
            p = Command.CreateParameter();
            p.ParameterName = name;
            p.Value = DBNullHelper.ToDb(value);
            Command.Parameters.Add(p);
        }
        return this;
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
    public DbCommand Command { get; }

    /// <summary>
    /// Executes the query and returns the result as a list of dynamic objects.
    /// </summary>
    public IEnumerable<dynamic> AsEnumerable()
    {
        using var reader = AsReader();
        while (reader.Read()) yield return reader.ToExpando();
    }

    /// <summary>
    /// Executes the query and returns the result as a list of [T]. This method is slightly faster.
    /// than doing AsEnumerable().Select(selector). The selector is required to map objects as the
    /// underlying datareader is enumerated.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selector">mapping function that transforms a datarecord (wrapped as a dynamic object) to an instance of type [T]</param>
    public IEnumerable<T> AsEnumerable<T>(Func<dynamic, T> selector) => Select(selector);

    /// <summary>
    /// Executes the query and returns the result as a list of [T] using the 'case-insensitive, underscore-agnostic column name to property mapping convention.'
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public IEnumerable<T> AsEnumerable<T>()
    {
        using var reader = AsReader();
        var setterMap = reader.GetSetterMap<T>(_config);
        while (reader.Read())
            yield return reader.MapTo(setterMap);
    }

    // enables linq 'select' syntax
    public IEnumerable<T> Select<T>(Func<dynamic, T> selector)
    {
        using var reader = AsReader();
        while (reader.Read())
            yield return selector(Dynamic.From(reader));
    }

    /// <summary>
    /// Executes the query and returns the result as a list of lists
    /// </summary>
    public IEnumerable<IReadOnlyCollection<dynamic>> AsMultiResultSet()
    {
        using var reader = AsReader();
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
    public (IReadOnlyCollection<T1>, IReadOnlyCollection<T2>) AsMultiResultSet<T1, T2>()
    {
        using var reader = AsReader();
        return (
            GetResultSet<T1>(reader, _config, out _),
            GetResultSet<T2>(reader, _config, out _)
            );
    }

    /// <summary>
    /// Executes the query and returns the result as a tuple of lists
    /// </summary>
    public (IReadOnlyCollection<T1>, IReadOnlyCollection<T2>, IReadOnlyCollection<T3>) AsMultiResultSet<T1, T2, T3>()
    {
        using var reader = AsReader();
        return (
            GetResultSet<T1>(reader, _config, out _),
            GetResultSet<T2>(reader, _config, out _),
            GetResultSet<T3>(reader, _config, out _)
            );
    }
    /// <summary>
    /// Executes the query and returns the result as a tuple of lists
    /// </summary>
    public (IReadOnlyCollection<T1>, IReadOnlyCollection<T2>, IReadOnlyCollection<T3>, IReadOnlyCollection<T4>) AsMultiResultSet<T1, T2, T3, T4>()
    {
        using var reader = AsReader();
        return (
            GetResultSet<T1>(reader, _config, out _),
            GetResultSet<T2>(reader, _config, out _),
            GetResultSet<T3>(reader, _config, out _),
            GetResultSet<T4>(reader, _config, out _)
            );
    }
    /// <summary>
    /// Executes the query and returns the result as a tuple of lists
    /// </summary>
    public (IReadOnlyCollection<T1>, IReadOnlyCollection<T2>, IReadOnlyCollection<T3>, IReadOnlyCollection<T4>, IReadOnlyCollection<T5>) AsMultiResultSet<T1, T2, T3, T4, T5>()
    {
        using var reader = AsReader();
        return (
            GetResultSet<T1>(reader, _config, out _),
            GetResultSet<T2>(reader, _config, out _),
            GetResultSet<T3>(reader, _config, out _),
            GetResultSet<T4>(reader, _config, out _),
            GetResultSet<T5>(reader, _config, out _)
            );
    }
    private static IReadOnlyCollection<T> GetResultSet<T>(IDataReader reader, DbConfig config, out bool moreResults)
    {
        var list = new List<T>();
        var map = reader.GetSetterMap<T>(config);
        while (reader.Read()) list.Add(reader.MapTo(map));
        moreResults = reader.NextResult();
        return list;
    }

    /// <summary>
    /// Executes the command, returning the first column of the first result, converted to the type T
    /// </summary>
    /// <typeparam name="T">return type</typeparam>
    public T? AsScalar<T>() => ConvertTo<T>.From(AsScalar());

    public object AsScalar() => Execute.Scalar();

    /// <summary>
    /// Executes the command as a SQL statement, returning the number of rows affected
    /// </summary>
    public int AsNonQuery() => Execute.NonQuery();

    /// <summary>
    /// Executes the command as a datareader. Use this if you need best performance.
    /// </summary>
    public IDataReader AsReader() => Execute.Reader();

    public T Single<T>() => AsEnumerable<T>().Single();

    private Executor Execute => new(Command);

    /// <summary>
    /// Executes the command as a statement, returning the number of rows affected asynchronously
    /// This method is only supported if the underlying provider propertly implements async behaviour.
    /// </summary>
    public Task<int> AsNonQueryAsync() => ExecuteAsync.NonQuery();

    /// <summary>
    /// Executes the command, returning the first column of the first result, converted to the type T asynchronously.
    /// This method is only supported if the underlying provider propertly implements async behaviour.
    /// </summary>
    /// <typeparam name="T">return type</typeparam>
    public async Task<T?> AsScalarAsync<T>()
    {
        var result = await ExecuteAsync.Scalar().ConfigureAwait(false);
        return ConvertTo<T>.From(result);
    }

    /// <summary>
    /// Executes the query and returns the result as a list of dynamic objects asynchronously
    /// This method is only supported if the underlying provider propertly implements async behaviour.
    /// </summary>
    public async IAsyncEnumerable<dynamic> AsEnumerableAsync()
    {
        using var reader = await ExecuteAsync.Reader().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
            yield return reader.ToExpando();
    }

    /// <summary>
    /// Executes the query and returns the result as a list of [T] asynchronously
    /// This method is only supported if the underlying provider propertly implements async behaviour.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selector">mapping function that transforms a datarecord (wrapped as a dynamic object) to an instance of type [T]</param>
    public async IAsyncEnumerable<T> AsEnumerableAsync<T>(Func<dynamic, T> selector)
    {
        using var reader = await ExecuteAsync.Reader().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            var d = Dynamic.From(reader);
            var item = selector(d);
            yield return item;
        }
    }

    /// <summary>
    /// Executes the query and returns the result as a list of [T] asynchronously
    /// This method is only supported if the underlying provider propertly implements async behaviour.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="selector">mapping function that transforms a datarecord (wrapped as a dynamic object) to an instance of type [T]</param>
    public async IAsyncEnumerable<T> AsEnumerableAsync<T>()
    {
        using var reader = await ExecuteAsync.Reader().ConfigureAwait(false);
        var setterMap = reader.GetSetterMap<T>(_config);
        while (await reader.ReadAsync().ConfigureAwait(false))
            yield return reader.MapTo(setterMap);
    }

    public ValueTask<T> SingleAsync<T>() => AsEnumerableAsync<T>().SingleAsync();

    private AsyncExecutor ExecuteAsync => new(Command);
}
