using Microsoft.Data.SqlClient;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Net.Code.ADONet
{
    using static DBNullHelper;

    public partial class CommandBuilder
    {
        private record struct AsyncExecutor(DbCommand Command)
        {
            /// <summary>
            /// executes the query as a datareader
            /// </summary>
            public async Task<DbDataReader> Reader()
            {
                var command = await PrepareAsync().ConfigureAwait(false);
                return await command.ExecuteReaderAsync().ConfigureAwait(false);
            }

            /// <summary>
            /// Executes the command, returning the first column of the first result as a scalar value
            /// </summary>
            public async Task<object> Scalar()
            {
                var command = await PrepareAsync().ConfigureAwait(false);
                return await command.ExecuteScalarAsync().ConfigureAwait(false);
            }

            /// <summary>
            /// Executes the command as a SQL statement, returning the number of rows affected
            /// </summary>
            public async Task<int> NonQuery()
            {
                var command = await PrepareAsync().ConfigureAwait(false);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            private async Task<DbCommand> PrepareAsync()
            {
                Logger.LogCommand(Command);
                if (Command.Connection.State == ConnectionState.Closed)
                    await Command.Connection.OpenAsync().ConfigureAwait(false);
                return Command;
            }
        }
    }

    public partial class CommandBuilder : IDisposable
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
        /// <param name = "text"></param>
        public CommandBuilder WithCommandText(string text)
        {
            Command.CommandText = text;
            return this;
        }

        /// <summary>
        /// Sets the command type
        /// </summary>
        /// <param name = "type"></param>
        public CommandBuilder OfType(CommandType type)
        {
            Command.CommandType = type;
            return this;
        }

        /// <summary>
        /// Adds a parameter for each property of the given object, with the property name as the name
        /// of the parameter and the property value as the corresponding parameter value
        /// </summary>
        /// <param name = "parameters"></param>
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
        /// <param name = "timeout"></param>
        public CommandBuilder WithTimeout(TimeSpan timeout)
        {
            Command.CommandTimeout = (int)timeout.TotalSeconds;
            return this;
        }

        /// <summary>
        /// Builder method - adds a name/value pair as parameter
        /// </summary>
        /// <param name = "name">the parameter name</param>
        /// <param name = "value">the parameter value</param>
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

        public CommandBuilder WithParameter<T>(T p)
            where T : IDbDataParameter
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
            while (reader.Read())
                yield return reader.ToExpando();
        }

        /// <summary>
        /// Executes the query and returns the result as a list of [T]. This method is slightly faster.
        /// than doing AsEnumerable().Select(selector). The selector is required to map objects as the
        /// underlying datareader is enumerated.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "selector">mapping function that transforms a datarecord (wrapped as a dynamic object) to an instance of type [T]</param>
        public IEnumerable<T> AsEnumerable<T>(Func<dynamic, T> selector) => Select(selector);
        /// <summary>
        /// Executes the query and returns the result as a list of [T] using the 'case-insensitive, underscore-agnostic column name to property mapping convention.'
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        public IEnumerable<T> AsEnumerable<T>()
        {
            using var reader = AsReader();
            var mapper = reader.GetMapper<T>(_config);
            while (reader.Read())
                yield return mapper(reader);
        }

        // enables linq 'select' syntax
        public IEnumerable<T> Select<T>(Func<dynamic, T> selector)
        {
            using var reader = AsReader();
            while (reader.Read())
                yield return selector(Dynamic.From(reader));
        }

        public IEnumerable<T> Select<T>(Func<IDataReader, T> selector)
        {
            using var reader = AsReader();
            while (reader.Read())
                yield return selector(reader);
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
                while (reader.Read())
                    list.Add(reader.ToExpando());
                yield return list;
            }
            while (reader.NextResult());
        }

        /// <summary>
        /// Executes the query and returns the result as a tuple of lists
        /// </summary>
        public (IReadOnlyCollection<T1>, IReadOnlyCollection<T2>) AsMultiResultSet<T1, T2>()
        {
            using var reader = AsReader();
            return (GetResultSet<T1>(reader, _config, out _), GetResultSet<T2>(reader, _config, out _));
        }

        /// <summary>
        /// Executes the query and returns the result as a tuple of lists
        /// </summary>
        public (IReadOnlyCollection<T1>, IReadOnlyCollection<T2>, IReadOnlyCollection<T3>) AsMultiResultSet<T1, T2, T3>()
        {
            using var reader = AsReader();
            return (GetResultSet<T1>(reader, _config, out _), GetResultSet<T2>(reader, _config, out _), GetResultSet<T3>(reader, _config, out _));
        }

        /// <summary>
        /// Executes the query and returns the result as a tuple of lists
        /// </summary>
        public (IReadOnlyCollection<T1>, IReadOnlyCollection<T2>, IReadOnlyCollection<T3>, IReadOnlyCollection<T4>) AsMultiResultSet<T1, T2, T3, T4>()
        {
            using var reader = AsReader();
            return (GetResultSet<T1>(reader, _config, out _), GetResultSet<T2>(reader, _config, out _), GetResultSet<T3>(reader, _config, out _), GetResultSet<T4>(reader, _config, out _));
        }

        /// <summary>
        /// Executes the query and returns the result as a tuple of lists
        /// </summary>
        public (IReadOnlyCollection<T1>, IReadOnlyCollection<T2>, IReadOnlyCollection<T3>, IReadOnlyCollection<T4>, IReadOnlyCollection<T5>) AsMultiResultSet<T1, T2, T3, T4, T5>()
        {
            using var reader = AsReader();
            return (GetResultSet<T1>(reader, _config, out _), GetResultSet<T2>(reader, _config, out _), GetResultSet<T3>(reader, _config, out _), GetResultSet<T4>(reader, _config, out _), GetResultSet<T5>(reader, _config, out _));
        }

        private static IReadOnlyCollection<T> GetResultSet<T>(IDataReader reader, DbConfig config, out bool moreResults)
        {
            var list = new List<T>();
            var map = reader.GetMapper<T>(config);
            while (reader.Read())
                list.Add(map(reader));
            moreResults = reader.NextResult();
            return list;
        }

        /// <summary>
        /// Executes the command, returning the first column of the first result, converted to the type T
        /// </summary>
        /// <typeparam name = "T">return type</typeparam>
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
        /// <typeparam name = "T">return type</typeparam>
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
        /// <typeparam name = "T"></typeparam>
        /// <param name = "selector">mapping function that transforms a datarecord (wrapped as a dynamic object) to an instance of type [T]</param>
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
        /// <typeparam name = "T"></typeparam>
        /// <param name = "selector">mapping function that transforms a datarecord (wrapped as a dynamic object) to an instance of type [T]</param>
        public async IAsyncEnumerable<T> AsEnumerableAsync<T>()
        {
            using var reader = await ExecuteAsync.Reader().ConfigureAwait(false);
            var map = reader.GetMapper<T>(_config);
            while (await reader.ReadAsync().ConfigureAwait(false))
                yield return map(reader);
        }

        public ValueTask<T> SingleAsync<T>() => AsEnumerableAsync<T>().SingleAsync();
        public void Dispose() => Command.Dispose();
        private AsyncExecutor ExecuteAsync => new(Command);
    }

    /// <summary>
    /// Class for runtime type conversion, including DBNull.Value to/from null. Supports reference types,
    /// value types and nullable value types
    /// </summary>
    /// <typeparam name = "T"></typeparam>
    public static class ConvertTo<T>
    {
        // ReSharper disable once StaticFieldInGenericType
        // clearly we *want* a static field for each instantiation of this generic class...
        /// <summary>
        /// The actual conversion method. Converts an object to any type using standard casting functionality,
        /// taking into account null/nullable types and avoiding DBNull issues. This method is set as a delegate
        /// at runtime (in the static constructor).
        /// </summary>
        public static readonly Func<object?, T?> From;
        static ConvertTo() => // Sets the From delegate, depending on whether T is a reference type, a nullable value type or a value type.
        From = CreateConvertFunction(typeof(T));
        private static Func<object?, T?> CreateConvertFunction(Type type) => type switch
        {
            Type t when !t.IsValueType => ConvertRefType,
            Type t when !t.IsNullableType() => ConvertValueType,
            Type t => CreateConvertNullableValueTypeFunc(t)};
        private static Func<object?, T?> CreateConvertNullableValueTypeFunc(Type type)
        {
            var delegateType = typeof(Func<object?, T?>);
            var methodInfo = typeof(ConvertTo<T>).GetMethod("ConvertNullableValueType", BindingFlags.NonPublic | BindingFlags.Static);
            var genericMethodForElement = methodInfo.MakeGenericMethod(type.GetGenericArguments()[0]);
            return (Func<object?, T?>)genericMethodForElement.CreateDelegate(delegateType);
        }

#pragma warning disable IDE0051 // Remove unused private members

#pragma warning disable RCS1213 // Remove unused member declaration.

        private static TElem? ConvertNullableValueType<TElem>(object value)
            where TElem : struct => IsNull(value) ? default(TElem? ) : ConvertPrivate<TElem>(value);
#pragma warning restore RCS1213 // Remove unused member declaration.

#pragma warning restore IDE0051 // Remove unused private members

        private static T? ConvertRefType(object? value) => IsNull(value) ? default : ConvertPrivate<T>(value!);
        private static T ConvertValueType(object? value) => IsNull(value) ? throw new NullReferenceException("Value is DbNull") : ConvertPrivate<T>(value!);
        private static TElem ConvertPrivate<TElem>(object value) => (TElem)Convert.ChangeType(value, typeof(TElem));
    }

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
        /// <summary>
        /// Instantiate Db with existing connection. The connection is only used for creating commands;
        /// it should be disposed by the caller when done.
        /// </summary>
        /// <param name = "connection">The existing connection</param>
        /// <param name = "config"></param>
        public Db(DbConnection connection, DbConfig config)
        {
            _connection = connection;
            _externalConnection = true;
            Config = config ?? DbConfig.Default;
        }

        /// <summary>
        /// Instantiate Db with connectionString and a custom IConnectionFactory
        /// </summary>
        /// <param name = "connectionString">the connection string</param>
        /// <param name = "providerFactory">the connection provider factory</param>
        public Db(string connectionString, DbProviderFactory providerFactory) : this(connectionString, DbConfig.FromProviderFactory(providerFactory), providerFactory)
        {
        }

        /// <summary>
        /// Instantiate Db with connectionString and a custom IConnectionFactory
        /// </summary>
        /// <param name = "connectionString">the connection string</param>
        /// <param name = "config"></param>
        /// <param name = "providerFactory">the connection factory</param>
        internal Db(string connectionString, DbConfig config, DbProviderFactory providerFactory)
        {
            _connection = providerFactory.CreateConnection();
            _connection.ConnectionString = connectionString;
            _externalConnection = false;
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
            if (_connection == null || _externalConnection)
                return;
            _connection.Dispose();
            _connection = null !;
        }

        /// <summary>
        /// Create a SQL query command builder
        /// </summary>
        /// <param name = "sqlQuery"></param>
        /// <returns>a CommandBuilder instance</returns>
        public CommandBuilder Sql(string sqlQuery) => CreateCommand(CommandType.Text, sqlQuery);
        /// <summary>
        /// Create a Stored Procedure command builder
        /// </summary>
        /// <param name = "sprocName">name of the sproc</param>
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
        /// <param name = "command"></param>
        public int Execute(string command) => Sql(command).AsNonQuery();
    }

    /// <summary>
    /// <para>
    /// The DbConfig class allows to configure database specific behaviour at runtime, without a direct
    /// dependency on the underlying ADO.Net provider. It does 2 things
    /// </para>
    /// <para>
    /// - provides a hook to configure a DbCommand in case some specific configuration is required. For example,
    ///   Oracle requires the BindByName property to be set to true for named parameters to work.
    /// - Sets the way database naming conventions are mapped to .Net naming conventions. For example, in Oracle,
    ///   database and column names are upper case and separated by underscores. Postgres defaults to lower case.
    ///   This includes also the escape character that indicates parameter names in queries with named parameters.
    /// </para>
    /// </summary>
    public record DbConfig(Action<IDbCommand> PrepareCommand, MappingConvention MappingConvention)
    {
        public static DbConfig FromProviderName(string providerName) => providerName switch
        {
            string s when s.StartsWith("Oracle") => Oracle,
            string s when s.StartsWith("Npgsql") => PostGreSQL,
            string s when s.StartsWith("IBM") => DB2,
            _ => Default
        };
        public static DbConfig FromProviderFactory(DbProviderFactory factory) => FromProviderName(factory.GetType().FullName);
        // By default, the Oracle driver does not support binding parameters by name;
        // one has to set the BindByName property on the OracleDbCommand.
        // Mapping: 
        // Oracle convention is to work with UPPERCASE_AND_UNDERSCORE instead of BookTitleCase
        public static readonly DbConfig Oracle = new(SetBindByName, new MappingConvention(StringExtensions.ToUpperWithUnderscores, StringExtensions.ToPascalCase, ':'));
        public static readonly DbConfig DB2 = new(NoOp, new MappingConvention(StringExtensions.ToUpperWithUnderscores, StringExtensions.ToPascalCase, '@'));
        public static readonly DbConfig PostGreSQL = new(NoOp, new MappingConvention(StringExtensions.ToLowerWithUnderscores, StringExtensions.ToPascalCase, '@'));
        public static readonly DbConfig Default = new(NoOp, new MappingConvention(StringExtensions.NoOp, StringExtensions.NoOp, '@'));
        private static void SetBindByName(dynamic c) => c.BindByName = true;
        private static void NoOp(dynamic c) { }
    }

    public static class DBNullHelper
    {
        public static bool IsNull(object? o) => o == null || DBNull.Value.Equals(o);
        public static object? FromDb(object? o) => IsNull(o) ? null : o;
        public static object? ToDb(object? o) => IsNull(o) ? DBNull.Value : o;
    }

    internal static class Dynamic
    {
        public static dynamic From(DataRow row) => From(row, (r, s) => r[s], row.Table.Columns.OfType<DataColumn>().Select(c => c.ColumnName));
        public static dynamic From(IDataRecord record) => From(record, (r, s) => r[s], GetMemberNames(record));
        public static dynamic From<TValue>(IDictionary<string, TValue> dictionary) => From(dictionary, (d, s) => d[s], dictionary.Keys);
        private static IEnumerable<string> GetMemberNames(IDataRecord record)
        {
            for (int i = 0; i < record.FieldCount; i++)
                yield return record.GetName(i);
        }

        private static dynamic From<T>(T item, Func<T, string, object?> getter, IEnumerable<string> memberNames) => new DynamicIndexer<T>(item, getter, memberNames);
        private class DynamicIndexer<T> : DynamicObject
        {
            private readonly T _item;
            private readonly Func<T, string, object?> _getter;
            private readonly IEnumerable<string> _memberNames;
            public DynamicIndexer(T item, Func<T, string, object?> getter, IEnumerable<string> memberNames)
            {
                _item = item;
                _getter = getter;
                _memberNames = memberNames;
            }

            public sealed override bool TryGetIndex(GetIndexBinder b, object[] i, out object? r) => ByMemberName(out r, (string)i[0]);
            public sealed override bool TryGetMember(GetMemberBinder b, out object? r) => ByMemberName(out r, b.Name);
            public sealed override IEnumerable<string> GetDynamicMemberNames() => _memberNames;
            private bool ByMemberName(out object? result, string memberName)
            {
                var value = _getter(_item, memberName);
                result = DBNullHelper.FromDb(value);
                return true;
            }
        }
    }

    public partial class CommandBuilder
    {
        private record struct Executor(DbCommand Command)
        {
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
                Logger.LogCommand(Command);
                if (Command.Connection.State == ConnectionState.Closed)
                    Command.Connection.Open();
                return Command;
            }
        }
    }

    internal sealed class FastReflection<T>
    {
        private FastReflection()
        {
        }

        private static readonly Type Type = typeof(FastReflection<T>);
        public static FastReflection<T> Instance = new();
        public IReadOnlyDictionary<string, Action<T, object?>> GetSettersForType() => _setters.GetOrAdd(typeof(T), d => d.GetProperties().Where(p => p.SetMethod != null).ToDictionary(p => p.Name, GetSetDelegate));
        private readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, Action<T, object?>>> _setters = new();
        private static Action<T, object?> GetSetDelegate(PropertyInfo p)
        {
            var method = p.GetSetMethod();
            var genericHelper = Type.GetMethod(nameof(CreateSetterDelegateHelper), BindingFlags.Static | BindingFlags.NonPublic);
            var constructedHelper = genericHelper.MakeGenericMethod(typeof(T), method.GetParameters()[0].ParameterType);
            return (Action<T, object?>)constructedHelper.Invoke(null, new object[]{method});
        }

        private static Action<TTarget, object> CreateSetterDelegateHelper<TTarget, TProperty>(MethodInfo method)
        {
            var action = (Action<TTarget, TProperty?>)method.CreateDelegate(typeof(Action<TTarget, TProperty>));
            return (target, param) => action(target, ConvertTo<TProperty>.From(param));
        }

        public IReadOnlyDictionary<string, Func<T, object?>> GetGettersForType() => _getters.GetOrAdd(typeof(T), t => t.GetProperties().Where(p => p.GetMethod != null).ToDictionary(p => p.Name, GetGetDelegate));
        private readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, Func<T, object?>>> _getters = new();
        private static Func<T, object?> GetGetDelegate(PropertyInfo p)
        {
            var method = p.GetGetMethod();
            var genericHelper = Type.GetMethod(nameof(CreateGetterDelegateHelper), BindingFlags.Static | BindingFlags.NonPublic);
            var constructedHelper = genericHelper.MakeGenericMethod(typeof(T), method.ReturnType);
            return (Func<T, object?>)constructedHelper.Invoke(null, new object[]{method});
        }

        private static Func<TTarget, object?> CreateGetterDelegateHelper<TTarget, TProperty>(MethodInfo method)
        {
            var func = (Func<TTarget, TProperty>)method.CreateDelegate(typeof(Func<TTarget, TProperty>));
            return target => ConvertTo<TProperty>.From(func(target));
        }
    }

    public interface IDb : IDisposable
    {
        /// <summary>
        /// Open a connection to the database. Not required.
        /// </summary>
        void Connect();
        /// <summary>
        /// Disconnect from the database.
        /// </summary>
        void Disconnect();
        /// <summary>
        /// Open a connection to the database. Not required.
        /// </summary>
        Task ConnectAsync();
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
        /// <param name = "sqlQuery"></param>
        /// <returns>a CommandBuilder instance</returns>
        CommandBuilder Sql(string sqlQuery);
        /// <summary>
        /// Create a Stored Procedure command
        /// </summary>
        /// <param name = "sprocName">name of the stored procedure</param>
        /// <returns>a CommandBuilder instance</returns>
        CommandBuilder StoredProcedure(string sprocName);
        /// <summary>
        /// Create a SQL command and execute it immediately (non query)
        /// </summary>
        /// <param name = "command"></param>
        int Execute(string command);
        /// <summary>
        /// DB name mapping convention
        /// </summary>
        MappingConvention MappingConvention { get; }
    }

    /// <summary>
    /// To enable logging, set the Log property of the Logger class
    /// </summary>
    public static class Logger
    {
        public static Action<string>? Log = null;
        internal static void LogCommand(IDbCommand command)
        {
            if (Log == null)
                return;
            Log.Invoke(command.CommandText);
            foreach (IDbDataParameter p in command.Parameters)
            {
                Log.Invoke($"{p.ParameterName} = {p.Value}");
            }
        }
    }

    internal static class DataRecordExtensions
    {
        private record struct Setter<T>(int FieldIndex, Action<T, object?> SetValue);
        private static List<Setter<T>> GetSetters<T>(this IDataReader reader, DbConfig config)
        {
            var convention = config.MappingConvention;
            var setters = FastReflection<T>.Instance.GetSettersForType();
            var list = new List<Setter<T>>(reader.FieldCount);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var columnName = convention.FromDb(reader.GetName(i));
                if (setters.TryGetValue(columnName, out var setter))
                {
                    list.Add(new(i, setter));
                }
            }

            return list;
        }

        static IEnumerable<(string name, Type type)> Parameters(this ConstructorInfo constructors) => constructors.GetParameters().Select(p => (p.Name, p.ParameterType));
        internal static Func<IDataRecord, T> GetMapper<T>(this IDataReader reader, DbConfig config)
        {
            var type = typeof(T);
            var properties = type.GetProperties().Select(p => (p.Name, p.PropertyType));
            // convention: if there is only a constructor with parameters for all properties
            // assume basic 'record-like' class
            var constructors = type.GetConstructors();
            var constructor = constructors.Length == 1 ? constructors.SingleOrDefault(c => c.Parameters().SequenceEqual(properties)) : null;
            if (constructor == null)
            {
                var setterMap = GetSetters<T>(reader, config);
                constructor = type.GetConstructor(Array.Empty<Type>());
                return record =>
                {
                    var item = (T)constructor.Invoke(null);
                    foreach (var setter in setterMap)
                    {
                        var val = DBNullHelper.FromDb(record.GetValue(setter.FieldIndex));
                        setter.SetValue(item, val);
                    }

                    return item;
                };
            }
            else
            {
                return record =>
                {
                    var values = properties.Select(p => DBNullHelper.FromDb(record.GetValue(record.GetOrdinal(p.Name))));
                    return (T)constructor.Invoke(values.ToArray());
                };
            }
        }

        /// <summary>
        /// Convert a datarecord into a dynamic object, so that properties can be simply accessed
        /// using standard C# syntax.
        /// </summary>
        /// <param name = "rdr">the data record</param>
        /// <returns>A dynamic object with fields corresponding to the database columns</returns>
        internal static dynamic ToExpando(this IDataRecord rdr) => Dynamic.From(rdr.NameValues().ToDictionary(p => p.name, p => p.value));
        internal static IEnumerable<(string name, object? value)> NameValues(this IDataRecord record)
        {
            for (var i = 0; i < record.FieldCount; i++)
                yield return (record.GetName(i), record[i]);
        }

        /// <summary>
        /// Get a value from an IDataRecord by column name. This method supports all types,
        /// as long as the DbType is convertible to the CLR Type passed as a generic argument.
        /// Also handles conversion from DbNull to null, including nullable types.
        /// </summary>
        public static TResult? Get<TResult>(this IDataRecord record, string name) => record.Get<TResult>(record.GetOrdinal(name));
        /// <summary>
        /// Get a value from an IDataRecord by index. This method supports all types,
        /// as long as the DbType is convertible to the CLR Type passed as a generic argument.
        /// Also handles conversion from DbNull to null, including nullable types.
        /// </summary>
        public static TResult? Get<TResult>(this IDataRecord record, int c) => ConvertTo<TResult>.From(record[c]);
    }

    internal static class DataTableExtensions
    {
        public static IEnumerable<dynamic> AsEnumerable(this DataTable dataTable) => dataTable.Rows.OfType<DataRow>().Select(Dynamic.From);
        public static IEnumerable<T> Select<T>(this DataTable dt, Func<dynamic, T> selector) => dt.AsEnumerable().Select(selector);
        public static IEnumerable<dynamic> Where(this DataTable dt, Func<dynamic, bool> predicate) => dt.AsEnumerable().Where(predicate);
        /// <summary>
        /// Executes the query (using datareader) and fills a datatable
        /// </summary>
        public static DataTable AsDataTable(this CommandBuilder commandBuilder)
        {
            using var reader = commandBuilder.AsReader();
            var tb = new DataTable();
            tb.Load(reader);
            return tb;
        }

        public static DataTable ToDataTable<T>(this IEnumerable<T> items)
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var table = new DataTable(typeof(T).Name);
            foreach (var prop in props)
            {
                var propType = prop.PropertyType.GetUnderlyingType();
                table.Columns.Add(prop.Name, propType);
            }

            var values = new object[props.Length];
            foreach (var item in items)
            {
                for (var i = 0; i < props.Length; i++)
                    values[i] = props[i].GetValue(item, null);
                table.Rows.Add(values);
            }

            return table;
        }
    }

    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Adapter from IEnumerable[T] to IDataReader
        /// </summary>
        public static DbDataReader AsDataReader<T>(this IEnumerable<T> input) => new EnumerableDataReaderImpl<T>(input);
        private class EnumerableDataReaderImpl<T> : DbDataReader
        {
            private readonly IEnumerable<T> _list;
            private readonly IEnumerator<T> _enumerator;
            private bool _disposed;
            private static readonly PropertyInfo[] Properties;
            private static readonly IReadOnlyDictionary<string, int> PropertyIndexesByName;
            private static readonly IReadOnlyDictionary<string, Func<T, object?>> Getters;
            static EnumerableDataReaderImpl()
            {
                var propertyInfos = typeof(T).GetProperties();
                Properties = propertyInfos.ToArray();
                Getters = FastReflection<T>.Instance.GetGettersForType();
                PropertyIndexesByName = Properties.Select((p, i) => (p, i)).ToDictionary(x => x.p.Name, x => x.i);
            }

            public EnumerableDataReaderImpl(IEnumerable<T> list)
            {
                _list = list;
                _enumerator = _list.GetEnumerator();
            }

            public override string GetName(int i) => Properties[i].Name;
            public override string GetDataTypeName(int i) => Properties[i].PropertyType.Name;
            public override IEnumerator GetEnumerator() => _enumerator;
            public override Type GetFieldType(int i) => Properties[i].PropertyType;
            public override object? GetValue(int i) => DBNullHelper.ToDb(Getters[Properties[i].Name](_enumerator.Current));
            public override int GetValues(object? [] values)
            {
                var length = Math.Min(values.Length, Properties.Length);
                for (int i = 0; i < length; i++)
                {
                    values[i] = GetValue(i);
                }

                return length;
            }

            public override int GetOrdinal(string name) => PropertyIndexesByName[name];
            public override bool GetBoolean(int i) => this.Get<bool>(i);
            public override byte GetByte(int i) => this.Get<byte>(i);
            public override long GetBytes(int i, long dataOffset, byte[] buffer, int bufferoffset, int length) => Get(i, dataOffset, buffer, bufferoffset, length);
            public override char GetChar(int i) => this.Get<char>(i);
            public override long GetChars(int i, long dataOffset, char[] buffer, int bufferoffset, int length) => Get(i, dataOffset, buffer, bufferoffset, length);
            public override Guid GetGuid(int i) => this.Get<Guid>(i);
            public override short GetInt16(int i) => this.Get<short>(i);
            public override int GetInt32(int i) => this.Get<int>(i);
            public override long GetInt64(int i) => this.Get<long>(i);
            public override float GetFloat(int i) => this.Get<float>(i);
            public override double GetDouble(int i) => this.Get<double>(i);
            public override string? GetString(int i) => this.Get<string>(i);
            public override decimal GetDecimal(int i) => this.Get<decimal>(i);
            public override DateTime GetDateTime(int i) => this.Get<DateTime>(i);
            private long Get<TElem>(int i, long dataOffset, TElem[] buffer, int bufferoffset, int length)
            {
                var data = this.Get<TElem[]>(i);
                if (data is null)
                    return 0;
                var maxLength = Math.Min((long)buffer.Length - bufferoffset, length);
                maxLength = Math.Min(data.Length - dataOffset, maxLength);
                Array.Copy(data, (int)dataOffset, buffer, bufferoffset, length);
                return maxLength;
            }

            public override bool IsDBNull(int i) => DBNull.Value.Equals(GetValue(i));
            public override int FieldCount => Properties.Length;
            public override bool HasRows => _list.Any();
            public override object? this[int i] => GetValue(i);
            public override object? this[string name] => GetValue(GetOrdinal(name));
            public override void Close() => Dispose();
            public override DataTable GetSchemaTable() => (
                from x in EnumerableDataReaderImpl<T>.Properties.Select((p, i) => (p, i))let p = x.p
                select new
                {
                ColumnName = p.Name, ColumnOrdinal = x.i, ColumnSize = int.MaxValue, // must be filled in and large enough for ToDataTable
 AllowDBNull = p.PropertyType.IsNullableType() || !p.PropertyType.IsValueType, // assumes string nullable
 DataType = p.PropertyType.GetUnderlyingType(), }

            ).ToDataTable();
            public override bool NextResult()
            {
                _enumerator?.Dispose();
                return false;
            }

            public override bool Read() => _enumerator.MoveNext();
            public override int Depth => 0;
            public override bool IsClosed => _disposed;
            public override int RecordsAffected => 0;
            protected override void Dispose(bool disposing) => _disposed = true;
        }
    }

    internal static class StringExtensions
    {
        public static string ToUpperRemoveSpecialChars(this string str) => string.IsNullOrEmpty(str) ? str : Regex.Replace(str, @"([^\w]|_)", "").ToUpperInvariant();
        public static string ToPascalCase(this string str) => str?.Aggregate((sb: new StringBuilder(), transform: (Func<char, char>)char.ToUpper), (t, c) => char.IsLetterOrDigit(c) ? (t.sb.Append(t.transform(c)), char.ToLower) : (t.sb, char.ToUpper)).sb.ToString() ?? string.Empty;
        public static string PascalCaseToSentence(this string source) => string.IsNullOrEmpty(source) ? source : string.Join(" ", SplitUpperCase(source));
        public static string ToUpperWithUnderscores(this string source) => string.Join("_", SplitUpperCase(source).Select(s => s.ToUpperInvariant()));
        public static string ToLowerWithUnderscores(this string source) => string.Join("_", SplitUpperCase(source).Select(s => s.ToLowerInvariant()));
        public static string NoOp(this string source) => source;
        private static IEnumerable<string> SplitUpperCase(string source)
        {
            var wordStart = 0;
            var letters = source.ToCharArray();
            var previous = char.MinValue;
            for (var i = 1; i < letters.Length; i++)
            {
                if (char.IsUpper(letters[i]) && !char.IsWhiteSpace(previous))
                {
                    yield return new string (letters, wordStart, i - wordStart);
                    wordStart = i;
                }

                previous = letters[i];
            }

            yield return new string (letters, wordStart, letters.Length - wordStart);
        }
    }

    internal static class TypeExtensions
    {
        public static bool HasCustomAttribute<TAttribute>(this MemberInfo t, Func<TAttribute, bool> whereClause) => t.GetCustomAttributes(false).OfType<TAttribute>().Any(whereClause);
        public static Type GetUnderlyingType(this Type type) => type.IsNullableType() ? Nullable.GetUnderlyingType(type) : type;
        public static bool IsNullableType(this Type type) => type.IsGenericType && !type.IsGenericTypeDefinition && typeof(Nullable<>) == type.GetGenericTypeDefinition();
    }

    public static class DbExtensions
    {
        /// <summary>
        /// Insert a list of items
        /// </summary>
        public static void Insert<T>(this IDb db, IEnumerable<T> items) => Do(db, items, QueryFactory<T>.Create(db.MappingConvention).Insert);
        /// <summary>
        /// Insert a list of items
        /// </summary>
        public static async Task InsertAsync<T>(this IDb db, IEnumerable<T> items) => await DoAsync(db, items, QueryFactory<T>.Create(db.MappingConvention).Insert).ConfigureAwait(false);
        /// <summary>
        /// Update a list of items
        /// </summary>
        public static void Update<T>(this IDb db, IEnumerable<T> items) => Do(db, items, QueryFactory<T>.Create(db.MappingConvention).Update);
        /// <summary>
        /// Update a list of items
        /// </summary>
        public static async Task UpdateAsync<T>(this IDb db, IEnumerable<T> items) => await DoAsync(db, items, QueryFactory<T>.Create(db.MappingConvention).Update).ConfigureAwait(false);
        /// <summary>
        /// Delete a list of items
        /// </summary>
        public static void Delete<T>(this IDb db, IEnumerable<T> items) => Do(db, items, QueryFactory<T>.Create(db.MappingConvention).Delete);
        /// <summary>
        /// Delete a list of items
        /// </summary>
        public static async Task DeleteAsync<T>(this IDb db, IEnumerable<T> items) => await DoAsync(db, items, QueryFactory<T>.Create(db.MappingConvention).Delete).ConfigureAwait(false);
        private static void Do<T>(IDb db, IEnumerable<T> items, string query)
        {
            using var commandBuilder = db.Sql(query);
            foreach (var item in items)
            {
                commandBuilder.WithParameters(item).AsNonQuery();
            }
        }

        private static async Task DoAsync<T>(IDb db, IEnumerable<T> items, string query)
        {
            using var commandBuilder = db.Sql(query);
            foreach (var item in items)
            {
                await commandBuilder.WithParameters(item).AsNonQueryAsync().ConfigureAwait(false);
            }
        }
    }

    public record struct MappingConvention(Func<string, string> ToDb, Func<string, string> FromDb, char Escape)
    {
        /// <summary>
        /// Maps column names to property names based on exact, case sensitive match. Database artefacts are named exactly
        /// like the .Net objects.
        /// </summary>
        public static readonly MappingConvention Default = new(StringExtensions.NoOp, StringExtensions.NoOp, '@');
        public string Parameter(string s) => $"{Escape}{s}";
    }

    public record Query(string Insert, string Update, string Delete, string Select, string SelectAll, string Count);
    internal sealed class QueryFactory<T>
    {
        private static readonly string[] AllPropertyNames;
        private static readonly string[] InsertPropertyNames;
        private static readonly string[] KeyPropertyNames;
        private static readonly string[] NonKeyPropertyNames;
        internal static Query Create(MappingConvention convention)
        {
            var insertColumns = string.Join(", ", QueryFactory<T>.InsertPropertyNames.Select(convention.ToDb));
            var insertValues = string.Join(", ", InsertPropertyNames.Select(s => $"{convention.Parameter(s)}"));
            var whereClause = string.Join(" AND ", KeyPropertyNames.Select(s => $"{convention.ToDb(s)} = {convention.Parameter(s)}"));
            var updateColumns = string.Join(", ", NonKeyPropertyNames.Select(s => $"{convention.ToDb(s)} = {convention.Parameter(s)}"));
            var allColumns = string.Join(", ", QueryFactory<T>.AllPropertyNames.Select(convention.ToDb));
            var tableName = convention.ToDb(typeof(T).Name);
            return new Query($"INSERT INTO {tableName} ({insertColumns}) VALUES ({insertValues})", $"UPDATE {tableName} SET {updateColumns} WHERE {whereClause}", $"DELETE FROM {tableName} WHERE {whereClause}", $"SELECT {allColumns} FROM {tableName} WHERE {whereClause}", $"SELECT {allColumns} FROM {tableName}", $"SELECT COUNT(*) FROM {tableName}");
        }

        static QueryFactory()
        {
            var properties = typeof(T).GetProperties();
            var keyProperties = properties.Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(KeyAttribute)));
            if (!keyProperties.Any())
                keyProperties = properties.Where(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
            if (!keyProperties.Any())
                keyProperties = properties.Where(p => p.Name.Equals($"{typeof(T).Name}Id", StringComparison.OrdinalIgnoreCase)).ToArray();
            var dbGenerated = keyProperties.Where(p => p.HasCustomAttribute<DatabaseGeneratedAttribute>(a => a.DatabaseGeneratedOption != DatabaseGeneratedOption.None));
            var nonKeyProperties = properties.Except(keyProperties);
            AllPropertyNames = properties.Select(p => p.Name).ToArray();
            InsertPropertyNames = properties.Except(dbGenerated).Select(p => p.Name).ToArray();
            KeyPropertyNames = keyProperties.Select(p => p.Name).ToArray();
            NonKeyPropertyNames = nonKeyProperties.Select(p => p.Name).ToArray();
        }
    }
}

namespace Net.Code.ADONet.Extensions.SqlClient
{
    public static class DbExtensions
    {
        /// <summary>
        /// Adds a table-valued parameter. Only supported on SQL Server (System.Data.SqlClient)
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "commandBuilder"></param>
        /// <param name = "name">parameter name</param>
        /// <param name = "values">list of values</param>
        /// <param name = "udtTypeName">name of the user-defined table type</param>
        public static CommandBuilder WithParameter<T>(this CommandBuilder commandBuilder, string name, IEnumerable<T> values, string udtTypeName)
        {
            var dataTable = values.ToDataTable();
            var p = new SqlParameter(name, SqlDbType.Structured)
            {
                TypeName = udtTypeName,
                Value = dataTable
            };
            return commandBuilder.WithParameter(p);
        }

        /// <summary>
        /// Assumes one to one mapping between
        /// - tablename and typename
        /// - property names and column names
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "db"></param>
        /// <param name = "items"></param>
        public static void BulkCopy<T>(this IDb db, IEnumerable<T> items)
        {
            using var bcp = new SqlBulkCopy(db.ConnectionString)
            {
                DestinationTableName = typeof(T).Name
            };
            // by default, SqlBulkCopy assumes columns in the database 
            // are in same order as the columns of the source data reader
            // => add explicit column mappings by name
            foreach (var p in typeof(T).GetProperties())
            {
                bcp.ColumnMappings.Add(p.Name, p.Name);
            }

            var datareader = items.AsDataReader();
            bcp.WriteToServer(datareader);
        }
    }
}

namespace System.Runtime.CompilerServices
{
    internal class IsExternalInit { }
}