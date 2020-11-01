using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Net.Code.ADONet
{
    public class CommandBuilder
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
            var getters = FastReflection.Instance.GetGettersForType<T>();
            var props = parameters.GetType().GetProperties();
            foreach (var item in props)
            {
                WithParameter(item.Name, getters[item.Name](parameters));
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
        public CommandBuilder WithParameter(string name, object value)
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
                reader.GetResultSet<T1>(_config, out _),
                reader.GetResultSet<T2>(_config, out _)
                );
        }

        /// <summary>
        /// Executes the query and returns the result as a tuple of lists
        /// </summary>
        public (IReadOnlyCollection<T1>, IReadOnlyCollection<T2>, IReadOnlyCollection<T3>) AsMultiResultSet<T1, T2, T3>() 
        {
            using (var reader = AsReader())
            {
                return (
                    reader.GetResultSet<T1>(_config, out _),
                    reader.GetResultSet<T2>(_config, out _),
                    reader.GetResultSet<T3>(_config, out _)
                    );
            }
        }
        /// <summary>
        /// Executes the query and returns the result as a tuple of lists
        /// </summary>
        public (IReadOnlyCollection<T1>, IReadOnlyCollection<T2>, IReadOnlyCollection<T3>, IReadOnlyCollection<T4>) AsMultiResultSet<T1, T2, T3, T4>()
        {
            using var reader = AsReader();
            return (
                reader.GetResultSet<T1>(_config, out _),
                reader.GetResultSet<T2>(_config, out _),
                reader.GetResultSet<T3>(_config, out _),
                reader.GetResultSet<T4>(_config, out _)
                );
        }
        /// <summary>
        /// Executes the query and returns the result as a tuple of lists
        /// </summary>
        public (IReadOnlyCollection<T1>, IReadOnlyCollection<T2>, IReadOnlyCollection<T3>, IReadOnlyCollection<T4>, IReadOnlyCollection<T5>) AsMultiResultSet<T1, T2, T3, T4, T5>()
        {
            using var reader = AsReader();
            return (
                reader.GetResultSet<T1>(_config, out _),
                reader.GetResultSet<T2>(_config, out _),
                reader.GetResultSet<T3>(_config, out _),
                reader.GetResultSet<T4>(_config, out _),
                reader.GetResultSet<T5>(_config, out _)
                );
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

        private Executor Execute => new Executor(Command);

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
            var result = await ExecuteAsync.Scalar();
            return ConvertTo<T>.From(result);
        }

        /// <summary>
        /// Executes the query and returns the result as a list of dynamic objects asynchronously
        /// This method is only supported if the underlying provider propertly implements async behaviour.
        /// </summary>
        public async IAsyncEnumerable<dynamic> AsEnumerableAsync()
        {
            using var reader = await ExecuteAsync.Reader();
            while (await reader.ReadAsync())
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
            using var reader = await ExecuteAsync.Reader();
            while (await reader.ReadAsync())
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
            using var reader = await ExecuteAsync.Reader();
            var setterMap = reader.GetSetterMap<T>(_config);
            while (await reader.ReadAsync())
                yield return reader.MapTo(setterMap);
        }

        public ValueTask<T> SingleAsync<T>() => AsEnumerableAsync<T>().SingleAsync();

        private AsyncExecutor ExecuteAsync => new AsyncExecutor(Command);

        class Executor
        {
            private readonly DbCommand _command;
            public Executor(DbCommand command)
            {
                _command = command;
            }

            /// <summary>
            /// executes the query as a datareader
            /// </summary>
            public DbDataReader Reader() => Prepare().ExecuteReader();

            /// <summary>
            /// Executes the command, returning the first column of the first result as a scalar value
            /// </summary>
            public object Scalar() => Prepare().ExecuteScalar();

            /// <summary>
            /// Executes the command as a SQL statement, returning the number of rows affected
            /// </summary>
            public int NonQuery() => Prepare().ExecuteNonQuery();

            private DbCommand Prepare()
            {
                Logger.LogCommand(_command);
                if (_command.Connection.State == ConnectionState.Closed)
                    _command.Connection.Open();
                return _command;
            }
        }

        class AsyncExecutor
        {
            private readonly DbCommand _command;

            public AsyncExecutor(DbCommand command)
            {
                _command = command;
            }

            /// <summary>
            /// executes the query as a datareader
            /// </summary>
            public async Task<DbDataReader> Reader()
            {
                var command = await PrepareAsync();
                return await command.ExecuteReaderAsync();
            }

            /// <summary>
            /// Executes the command, returning the first column of the first result as a scalar value
            /// </summary>
            public async Task<object> Scalar()
            {
                var command = await PrepareAsync();
                return await command.ExecuteScalarAsync();
            }

            /// <summary>
            /// Executes the command as a SQL statement, returning the number of rows affected
            /// </summary>
            public async Task<int> NonQuery()
            {
                var command = await PrepareAsync();
                return await command.ExecuteNonQueryAsync();
            }

            private async Task<DbCommand> PrepareAsync()
            {
                Logger.LogCommand(_command);
                if (_command.Connection.State == ConnectionState.Closed)
                    await _command.Connection.OpenAsync();
                return _command;
            }

        }

    }
}