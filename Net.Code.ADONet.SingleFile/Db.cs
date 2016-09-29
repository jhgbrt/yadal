using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Configuration;
using System.ComponentModel;
using System.Dynamic;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlClient;

namespace Net.Code.ADONet
{
    public class CommandBuilder
    {
        private readonly DbConfig _config;
        public CommandBuilder(IDbCommand command, DbConfig config)
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
        public CommandBuilder WithParameter(string name, object value)
        {
            IDataParameter p;
            if (Command.Parameters.Contains(name))
            {
                p = (IDbDataParameter)Command.Parameters[name];
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

        public CommandBuilder WithParameter<T>(T p)where T : IDbDataParameter
        {
            Command.Parameters.Add(p);
            return this;
        }

        public CommandBuilder InTransaction(IDbTransaction tx)
        {
            Command.Transaction = tx;
            return this;
        }

        /// <summary>
        /// The raw IDbCommand instance
        /// </summary>
        public IDbCommand Command
        {
            get;
        }

        /// <summary>
        /// Executes the query and returns the result as a list of dynamic objects. 
        /// </summary>
        public IEnumerable<dynamic> AsEnumerable() => Execute.Reader().ToExpandoList();
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
        public IEnumerable<T> AsEnumerable<T>() => AsReader().AsEnumerable<T>(_config);
        // enables linq 'select' syntax
        public IEnumerable<T> Select<T>(Func<dynamic, T> selector) => Execute.Reader().ToDynamicDataRecord().Select(selector);
        /// <summary>
        /// Executes the query and returns the result as a list of lists
        /// </summary>
        public IEnumerable<IReadOnlyCollection<dynamic>> AsMultiResultSet()
        {
            using (var reader = Execute.Reader())
            {
                foreach (var item in reader.ToMultiResultSet())
                    yield return item;
            }
        }

        /// <summary>
        /// Executes the query and returns the result as a tuple of lists
        /// </summary>
        public MultiResultSet<T1, T2> AsMultiResultSet<T1, T2>()
        {
            using (var reader = Execute.Reader())
            {
                bool more;
                return MultiResultSet.Create(reader.GetResultSet<T1>(_config, out more), reader.GetResultSet<T2>(_config, out more));
            }
        }

        /// <summary>
        /// Executes the query and returns the result as a tuple of lists
        /// </summary>
        public MultiResultSet<T1, T2, T3> AsMultiResultSet<T1, T2, T3>()
        {
            using (var reader = Execute.Reader())
            {
                bool more;
                return MultiResultSet.Create(reader.GetResultSet<T1>(_config, out more), reader.GetResultSet<T2>(_config, out more), reader.GetResultSet<T3>(_config, out more));
            }
        }

        /// <summary>
        /// Executes the query and returns the result as a tuple of lists
        /// </summary>
        public MultiResultSet<T1, T2, T3, T4> AsMultiResultSet<T1, T2, T3, T4>()
        {
            using (var reader = Execute.Reader())
            {
                bool more;
                return MultiResultSet.Create(reader.GetResultSet<T1>(_config, out more), reader.GetResultSet<T2>(_config, out more), reader.GetResultSet<T3>(_config, out more), reader.GetResultSet<T4>(_config, out more));
            }
        }

        /// <summary>
        /// Executes the query and returns the result as a tuple of lists
        /// </summary>
        public MultiResultSet<T1, T2, T3, T4, T5> AsMultiResultSet<T1, T2, T3, T4, T5>()
        {
            using (var reader = Execute.Reader())
            {
                bool more;
                return MultiResultSet.Create(reader.GetResultSet<T1>(_config, out more), reader.GetResultSet<T2>(_config, out more), reader.GetResultSet<T3>(_config, out more), reader.GetResultSet<T4>(_config, out more), reader.GetResultSet<T5>(_config, out more));
            }
        }

        /// <summary>
        /// Executes the command, returning the first column of the first result, converted to the type T
        /// </summary>
        /// <typeparam name = "T">return type</typeparam>
        public T AsScalar<T>() => ConvertTo<T>.From(AsScalar());
        public object AsScalar() => Execute.Scalar();
        /// <summary>
        /// Executes the command as a SQL statement, returning the number of rows affected
        /// </summary>
        public int AsNonQuery() => Execute.NonQuery();
        /// <summary>
        /// Executes the command as a datareader. Use this if you need best performance.
        /// </summary>
        public IDataReader AsReader() => Execute.Reader();
        public DataTable AsDataTable() => Execute.DataTable();
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
        /// <typeparam name = "T">return type</typeparam>
        public async Task<T> AsScalarAsync<T>()
        {
            var result = await ExecuteAsync.Scalar();
            return ConvertTo<T>.From(result);
        }

        /// <summary>
        /// Executes the query and returns the result as a list of dynamic objects asynchronously
        /// This method is only supported if the underlying provider propertly implements async behaviour.
        /// </summary>
        public async Task<IEnumerable<dynamic>> AsEnumerableAsync()
        {
            var reader = await ExecuteAsync.Reader();
            return reader.ToExpandoList();
        }

        /// <summary>
        /// Executes the query and returns the result as a list of [T] asynchronously
        /// This method is only supported if the underlying provider propertly implements async behaviour.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "selector">mapping function that transforms a datarecord (wrapped as a dynamic object) to an instance of type [T]</param>
        public async Task<IEnumerable<T>> AsEnumerableAsync<T>(Func<dynamic, T> selector)
        {
            var reader = await ExecuteAsync.Reader();
            return reader.ToDynamicDataRecord().Select(selector);
        }

        /// <summary>
        /// Executes the query and returns the result as a list of lists asynchronously
        /// This method is only supported if the underlying provider propertly implements async behaviour.
        /// </summary>
        public async Task<IEnumerable<IReadOnlyCollection<dynamic>>> AsMultiResultSetAsync()
        {
            using (var reader = await ExecuteAsync.Reader())
            {
                return reader.ToMultiResultSet().ToList();
            }
        }

        private AsyncExecutor ExecuteAsync => new AsyncExecutor((DbCommand)Command);
        class Executor
        {
            private readonly IDbCommand _command;
            public Executor(IDbCommand command)
            {
                _command = command;
            }

            /// <summary>
            /// executes the query as a datareader
            /// </summary>
            public IDataReader Reader() => Prepare().ExecuteReader();
            /// <summary>
            /// Executes the query (using datareader) and fills a datatable
            /// </summary>
            public DataTable DataTable()
            {
                using (var reader = Reader())
                {
                    var tb = new DataTable();
                    tb.Load(reader);
                    return tb;
                }
            }

            /// <summary>
            /// Executes the command, returning the first column of the first result as a scalar value
            /// </summary>
            public object Scalar() => Prepare().ExecuteScalar();
            /// <summary>
            /// Executes the command as a SQL statement, returning the number of rows affected
            /// </summary>
            public int NonQuery() => Prepare().ExecuteNonQuery();
            private IDbCommand Prepare()
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
            public async Task<IDataReader> Reader()
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
        public static readonly Func<object, T> From;
        static ConvertTo()
        {
            // Sets the From delegate, depending on whether T is a reference type, a nullable value type or a value type.
            From = CreateConvertFunction(typeof (T));
        }

        private static Func<object, T> CreateConvertFunction(Type type)
        {
            if (!type.IsValueType)
            {
                return ConvertRefType;
            }

            if (type.IsNullableType())
            {
                var delegateType = typeof (Func<object, T>);
                var methodInfo = typeof (ConvertTo<T>).GetMethod("ConvertNullableValueType", BindingFlags.NonPublic | BindingFlags.Static);
                var genericMethodForElement = methodInfo.MakeGenericMethod(type.GetGenericArguments()[0]);
                return (Func<object, T>)Delegate.CreateDelegate(delegateType, genericMethodForElement);
            }

            return ConvertValueType;
        }

        // ReSharper disable once UnusedMember.Local
        // (used via reflection!)
        private static TElem? ConvertNullableValueType<TElem>(object value)where TElem : struct => DBNullHelper.IsNull(value) ? (TElem? )null : ConvertPrivate<TElem>(value);
        private static T ConvertRefType(object value) => DBNullHelper.IsNull(value) ? default (T) : ConvertPrivate<T>(value);
        private static T ConvertValueType(object value)
        {
            if (DBNullHelper.IsNull(value))
            {
                throw new NullReferenceException("Value is DbNull");
            }

            return ConvertPrivate<T>(value);
        }

        private static TElem ConvertPrivate<TElem>(object value) => (TElem)(Convert.ChangeType(value, typeof (TElem)));
    }

    static class DataReaderExtensions
    {
        public static IEnumerable<IDataRecord> AsEnumerable(this IDataReader reader)
        {
            using (reader)
            {
                while (reader.Read())
                    yield return reader;
            }
        }

        public static IEnumerable<T> AsEnumerable<T>(this IDataReader reader, DbConfig config)
        {
            var setterMap = reader.GetSetterMap<T>(config);
            using (reader)
            {
                while (reader.Read())
                    yield return reader.MapTo<T>(setterMap);
            }
        }

        internal static IEnumerable<dynamic> ToExpandoList(this IDataReader reader)
        {
            using (reader)
            {
                while (reader.Read())
                    yield return reader.ToExpando();
            }
        }

        internal static IEnumerable<dynamic> ToDynamicDataRecord(this IDataReader reader)
        {
            using (reader)
            {
                while (reader.Read())
                    yield return Dynamic.From(reader);
            }
        }

        internal static IEnumerable<IReadOnlyCollection<dynamic>> ToMultiResultSet(this IDataReader reader)
        {
            do
            {
                var list = new Collection<dynamic>();
                while (reader.Read())
                    list.Add(reader.ToExpando());
                yield return list;
            }
            while (reader.NextResult());
        }

        internal static IReadOnlyCollection<T> GetResultSet<T>(this IDataReader reader, DbConfig config, out bool moreResults)
        {
            var list = new List<T>();
            var map = reader.GetSetterMap<T>(config);
            while (reader.Read())
                list.Add(reader.MapTo<T>(map));
            moreResults = reader.NextResult();
            return list;
        }
    }

    public static class DataRecordExtensions
    {
        internal class Setter<T>
        {
            public Setter(int fieldIndex, Action<T, object> action)
            {
                FieldIndex = fieldIndex;
                Action = action;
            }

            public int FieldIndex
            {
                get;
                private set;
            }

            public Action<T, object> Action
            {
                get;
                private set;
            }
        }

        internal class SetterMap<T> : List<Setter<T>>
        {
        }

        private static readonly IDictionary<Type, object> SetterMaps = new ConcurrentDictionary<Type, object>();
        internal static SetterMap<T> GetSetterMap<T>(this IDataReader reader, DbConfig config)
        {
            dynamic dmap;
            if (SetterMaps.TryGetValue(typeof (T), out dmap))
                return dmap;
            var map = new SetterMap<T>();
            var convention = config.MappingConvention;
            var setters = FastReflection.Instance.GetSettersForType<T>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var columnName = convention.FromDb(reader.GetName(i));
                Action<T, object> setter;
                if (setters.TryGetValue(columnName, out setter))
                {
                    map.Add(new Setter<T>(i, setter));
                }
            }

            SetterMaps.Add(typeof (T), map);
            return map;
        }

        internal static T MapTo<T>(this IDataRecord record, SetterMap<T> setterMap)
        {
            var result = Activator.CreateInstance<T>();
            foreach (var item in setterMap)
            {
                var val = DBNullHelper.FromDb(record.GetValue(item.FieldIndex));
                Action<T, object> setter = item.Action;
                setter(result, val);
            }

            return result;
        }

        /// <summary>
        /// Convert a datarecord into a dynamic object, so that properties can be simply accessed
        /// using standard C# syntax.
        /// </summary>
        /// <param name = "rdr">the data record</param>
        /// <returns>A dynamic object with fields corresponding to the database columns</returns>
        internal static dynamic ToExpando(this IDataRecord rdr)
        {
            var d = new Dictionary<string, object>();
            for (var i = 0; i < rdr.FieldCount; i++)
            {
                var name = rdr.GetName(i);
                var value = rdr[i];
                d.Add(name, value);
            }

            return Dynamic.From(d);
        }

        /// <summary>
        /// Get a value from an IDataRecord by column name. This method supports all types,
        /// as long as the DbType is convertible to the CLR Type passed as a generic argument.
        /// Also handles conversion from DbNull to null, including nullable types.
        /// </summary>
        public static TResult Get<TResult>(this IDataRecord reader, string name) => reader.Get<TResult>(reader.GetOrdinal(name));
        /// <summary>
        /// Get a value from an IDataRecord by index. This method supports all types,
        /// as long as the DbType is convertible to the CLR Type passed as a generic argument.
        /// Also handles conversion from DbNull to null, including nullable types.
        /// </summary>
        public static TResult Get<TResult>(this IDataRecord reader, int c) => ConvertTo<TResult>.From(reader[c]);
    }

    public static class DataTableExtensions
    {
        static dynamic ToDynamic(this DataRow dr) => Dynamic.From(dr);
        public static IEnumerable<dynamic> AsEnumerable(this DataTable dataTable) => dataTable.Rows.OfType<DataRow>().Select(ToDynamic);
        public static IEnumerable<T> Select<T>(this DataTable dt, Func<dynamic, T> selector) => dt.AsEnumerable().Select(selector);
        public static IEnumerable<dynamic> Where(this DataTable dt, Func<dynamic, bool> predicate) => dt.AsEnumerable().Where(predicate);
    }

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
        internal DbConfig Config
        {
            get;
        }

        public string ProviderName => Config.ProviderName;
        private readonly string _connectionString;
        private Lazy<IDbConnection> _connection;
        private readonly IConnectionFactory _connectionFactory;
        private readonly IDbConnection _externalConnection;
        /// <summary>
        /// Instantiate Db with existing connection. The connection is only used for creating commands; 
        /// it should be disposed by the caller when done.
        /// </summary>
        /// <param name = "connection">The existing connection</param>
        /// <param name = "config"></param>
        public Db(IDbConnection connection, DbConfig config)
        {
            _externalConnection = connection;
            Config = config ?? DbConfig.Default;
        }

        /// <summary>
        /// Instantiate Db with connectionString and DbProviderName
        /// </summary>
        /// <param name = "connectionString">The connection string</param>
        /// <param name = "providerName">The ADO .Net Provider name. When not specified, 
        /// the default value is used (see DefaultProviderName)</param>
        public Db(string connectionString, string providerName): this (connectionString, DbConfig.FromProviderName(providerName))
        {
        }

        /// <summary>
        /// Instantiate Db with connectionString and a custom IConnectionFactory
        /// </summary>
        /// <param name = "connectionString">the connection string</param>
        /// <param name = "config"></param>
        /// <param name = "connectionFactory">the connection factory</param>
        internal Db(string connectionString, DbConfig config, IConnectionFactory connectionFactory = null)
        {
            _connectionString = connectionString;
            _connectionFactory = connectionFactory ?? new AdoNetProviderFactory(config.ProviderName);
            _connection = new Lazy<IDbConnection>(CreateConnection);
            Config = config;
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
            var providerName = connectionStringSettings.ProviderName;
            return new Db(connectionString, providerName);
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
            if (_connection == null || !_connection.IsValueCreated)
                return;
            _connection.Value.Dispose();
            _connection = null;
        }

        /// <summary>
        /// Create a SQL query command builder
        /// </summary>
        /// <param name = "sqlQuery"></param>
        /// <returns>a CommandBuilder instance</returns>
        public CommandBuilder Sql(string sqlQuery) => CreateCommand(CommandType.Text, sqlQuery);
        /// <summary>
        /// Create a Stored Procedure command
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
        class AdoNetProviderFactory : IConnectionFactory
        {
            private readonly string _providerInvariantName;
            public AdoNetProviderFactory(string providerInvariantName)
            {
                _providerInvariantName = providerInvariantName;
            }

            public IDbConnection CreateConnection(string connectionString)
            {
                var connection = DbProviderFactories.GetFactory(_providerInvariantName).CreateConnection();
                // ReSharper disable once PossibleNullReferenceException
                connection.ConnectionString = connectionString;
                return connection;
            }
        }
    }

    public class DbConfig
    {
        internal DbConfig(Action<IDbCommand> prepareCommand, IMappingConvention convention, string providerName)
        {
            PrepareCommand = prepareCommand;
            MappingConvention = convention;
            ProviderName = providerName;
        }

        public Action<IDbCommand> PrepareCommand
        {
            get;
        }

        internal IMappingConvention MappingConvention
        {
            get;
        }

        public string ProviderName
        {
            get;
        }

        public static readonly DbConfig Default = Create("System.Data.SqlClient");
        public static DbConfig FromProviderName(string providerName)
        {
            return !string.IsNullOrEmpty(providerName) && providerName.StartsWith("Oracle") ? Oracle(providerName) : Create(providerName);
        }

        // By default, the Oracle driver does not support binding parameters by name;
        // one has to set the BindByName property on the OracleDbCommand.
        // Mapping: 
        // Oracle convention is to work with UPPERCASE_AND_UNDERSCORE instead of BookTitleCase
        private static DbConfig Oracle(string providerName) => new DbConfig(SetBindByName, Net.Code.ADONet.MappingConvention.OracleStyle, providerName);
        private static DbConfig Create(string providerName) => new DbConfig(c =>
        {
        }

        , Net.Code.ADONet.MappingConvention.Default, providerName);
        private static void SetBindByName(dynamic c) => c.BindByName = true;
    }

    public static class DBNullHelper
    {
        public static Type GetUnderlyingType(this Type type) => type.IsNullableType() ? new NullableConverter(type).UnderlyingType : type;
        public static bool IsNullableType(this Type type) => (type.IsGenericType && !type.IsGenericTypeDefinition) && (typeof (Nullable<>) == type.GetGenericTypeDefinition());
        public static bool IsNull(object o) => o == null || DBNull.Value.Equals(o);
        public static object FromDb(object o) => IsNull(o) ? null : o;
        public static object ToDb(object o) => IsNull(o) ? DBNull.Value : o;
    }

    static class Dynamic
    {
        public static dynamic From(DataRow row) => From(row, (r, s) => r[s]);
        public static dynamic From(IDataRecord record) => From(record, (r, s) => r[s]);
        public static dynamic From<TValue>(IDictionary<string, TValue> dictionary) => From(dictionary, (d, s) => d[s]);
        static dynamic From<T>(T item, Func<T, string, object> getter) => new DynamicIndexer<T>(item, getter);
        class DynamicIndexer<T> : DynamicObject
        {
            private readonly T _item;
            private readonly Func<T, string, object> _getter;
            public DynamicIndexer(T item, Func<T, string, object> getter)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));
                _item = item;
                _getter = getter;
            }

            public override bool TryGetIndex(GetIndexBinder b, object[] i, out object r) => ByMemberName(out r, (string)i[0]);
            public sealed override bool TryGetMember(GetMemberBinder b, out object r) => ByMemberName(out r, b.Name);
            private bool ByMemberName(out object result, string memberName)
            {
                var value = _getter(_item, memberName);
                result = DBNullHelper.FromDb(value);
                return true;
            }
        }
    }

    public static class EnumerableExtensions
    {
        public static DataTable ToDataTable<T>(this IEnumerable<T> items)
        {
            var table = new DataTable(typeof (T).Name);
            var props = typeof (T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
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

        /// <summary>
        /// Adapter from IEnumerable[T] to IDataReader
        /// </summary>
        public static IDataReader AsDataReader<T>(this IEnumerable<T> input) => new EnumerableDataReaderImpl<T>(input);
        private class EnumerableDataReaderImpl<T> : DbDataReader
        {
            private readonly IEnumerable<T> _list;
            private IEnumerator<T> _enumerator;
            private bool _disposed;
            // ReSharper disable StaticFieldInGenericType
            private static readonly PropertyInfo[] Properties;
            private static readonly IReadOnlyDictionary<string, int> PropertyIndexesByName;
            private static readonly IReadOnlyDictionary<string, Func<T, object>> Getters;
            // ReSharper restore StaticFieldInGenericType
            static EnumerableDataReaderImpl()
            {
                var propertyInfos = typeof (T).GetProperties();
                Properties = propertyInfos.ToArray();
                Getters = FastReflection.Instance.GetGettersForType<T>();
                PropertyIndexesByName = Properties.Select((p, i) => new
                {
                p, i
                }

                ).ToDictionary(x => x.p.Name, x => x.i);
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
            public override object GetValue(int i) => DBNullHelper.ToDb(Getters[Properties[i].Name](_enumerator.Current));
            public override int GetValues(object[] values)
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
            public override string GetString(int i) => this.Get<string>(i);
            public override decimal GetDecimal(int i) => this.Get<decimal>(i);
            public override DateTime GetDateTime(int i) => this.Get<DateTime>(i);
            long Get<TElem>(int i, long dataOffset, TElem[] buffer, int bufferoffset, int length)
            {
                var data = this.Get<TElem[]>(i);
                var maxLength = Math.Min((long)buffer.Length - bufferoffset, length);
                maxLength = Math.Min(data.Length - dataOffset, maxLength);
                Array.Copy(data, dataOffset, buffer, bufferoffset, length);
                return maxLength;
            }

            public override bool IsDBNull(int i) => DBNull.Value.Equals(GetValue(i));
            public override int FieldCount => Properties.Length;
            public override bool HasRows => _list.Any();
            public override object this[int i] => GetValue(i);
            public override object this[string name] => GetValue(GetOrdinal(name));
            public override void Close() => Dispose();
            public override DataTable GetSchemaTable()
            {
                var q =
                    from x in Properties.Select((p, i) => new
                    {
                    p, i
                    }

                    )let p = x.p
                    let nullable = p.PropertyType.IsNullableType()let dataType = p.PropertyType.GetUnderlyingType()select new
                    {
                    ColumnName = p.Name, ColumnOrdinal = x.i, ColumnSize = int.MaxValue, // must be filled in and large enough for ToDataTable
 AllowDBNull = nullable || !p.PropertyType.IsValueType, // assumes string nullable
 DataType = dataType, }

                ;
                var dt = q.ToDataTable();
                return dt;
            }

            public override bool NextResult()
            {
                _enumerator?.Dispose();
                _enumerator = null;
                return false;
            }

            public override bool Read() => _enumerator.MoveNext();
            public override int Depth => 0;
            public override bool IsClosed => _disposed;
            public override int RecordsAffected => 0;
            protected override void Dispose(bool disposing) => _disposed = true;
        }
    }

    public class FastReflection
    {
        private FastReflection()
        {
        }

        public static FastReflection Instance = new FastReflection();
        public IReadOnlyDictionary<string, Action<T, object>> GetSettersForType<T>()
        {
            var setters = _setters.GetOrAdd(new
            {
            Type = typeof (T)}

            , d => ((Type)d.Type).GetProperties().ToDictionary(p => p.Name, GetSetDelegate<T>));
            return (IReadOnlyDictionary<string, Action<T, object>>)setters;
        }

        private readonly ConcurrentDictionary<dynamic, object> _setters = new ConcurrentDictionary<dynamic, object>();
        static Action<T, object> GetSetDelegate<T>(PropertyInfo p)
        {
            var method = p.GetSetMethod();
            var genericHelper = typeof (FastReflection).GetMethod(nameof(CreateSetterDelegateHelper), BindingFlags.Static | BindingFlags.NonPublic);
            var constructedHelper = genericHelper.MakeGenericMethod(typeof (T), method.GetParameters()[0].ParameterType);
            return (Action<T, object>)constructedHelper.Invoke(null, new object[]{method});
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        // ReSharper disable once UnusedMember.Local
        static object CreateSetterDelegateHelper<TTarget, TProperty>(MethodInfo method)where TTarget : class
        {
            var action = (Action<TTarget, TProperty>)Delegate.CreateDelegate(typeof (Action<TTarget, TProperty>), method);
            Action<TTarget, object> ret = (target, param) => action(target, ConvertTo<TProperty>.From(param));
            return ret;
        }

        public IReadOnlyDictionary<string, Func<T, object>> GetGettersForType<T>()
        {
            var setters = _getters.GetOrAdd(new
            {
            Type = typeof (T)}

            , d => ((Type)d.Type).GetProperties().ToDictionary(p => p.Name, GetGetDelegate<T>));
            return (IReadOnlyDictionary<string, Func<T, object>>)setters;
        }

        private readonly ConcurrentDictionary<dynamic, object> _getters = new ConcurrentDictionary<dynamic, object>();
        static Func<T, object> GetGetDelegate<T>(PropertyInfo p)
        {
            var method = p.GetGetMethod();
            var genericHelper = typeof (FastReflection).GetMethod(nameof(CreateGetterDelegateHelper), BindingFlags.Static | BindingFlags.NonPublic);
            var constructedHelper = genericHelper.MakeGenericMethod(typeof (T), method.ReturnType);
            return (Func<T, object>)constructedHelper.Invoke(null, new object[]{method});
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        // ReSharper disable once UnusedMember.Local
        static object CreateGetterDelegateHelper<TTarget, TProperty>(MethodInfo method)where TTarget : class
        {
            var func = (Func<TTarget, TProperty>)Delegate.CreateDelegate(typeof (Func<TTarget, TProperty>), method);
            Func<TTarget, object> ret = target => ConvertTo<TProperty>.From(func(target));
            return ret;
        }
    }

    internal interface IConnectionFactory
    {
        /// <summary>
        /// Create the ADO.Net IDbConnection
        /// </summary>
        /// <param name = "connectionString"></param>
        /// <returns>the connection</returns>
        IDbConnection CreateConnection(string connectionString);
    }

    public interface IDb : IDisposable
    {
        /// <summary>
        /// Open a connection to the database. Not required.
        /// </summary>
        void Connect();
        /// <summary>
        /// The actual IDbConnection (which will be open)
        /// </summary>
        IDbConnection Connection
        {
            get;
        }

        /// <summary>
        /// The ADO.Net connection string
        /// </summary>
        string ConnectionString
        {
            get;
        }

        /// <summary>
        /// The ADO.Net ProviderName for this connection
        /// </summary>
        string ProviderName
        {
            get;
        }

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
    }

    /// <summary>
    /// To enable logging, set the Log property of the Logger class
    /// </summary>
    class Logger
    {
#if DEBUG
        internal static Action<string> Log = s => { Debug.WriteLine(s); };
#else
        public static Action<string> Log;
#endif
        internal static void LogCommand(IDbCommand command)
        {
            if (Log == null)
                return;
            Log(command.CommandText);
            foreach (IDbDataParameter p in command.Parameters)
            {
                Log($"{p.ParameterName} = {p.Value}");
            }
        }
    }

    public interface IMappingConvention
    {
        string FromDb(string s);
        string ToDb(string s);
        string Parameter(string s);
    }

    internal class MappingConvention : IMappingConvention
    {
        private readonly Func<string, string> _fromDb;
        private readonly Func<string, string> _toDb;
        private readonly char _escape;
        private MappingConvention(Func<string, string> todb, Func<string, string> fromdb, char escape)
        {
            _toDb = todb;
            _fromDb = fromdb;
            _escape = escape;
        }

        /// <summary>
        /// Maps column names to property names based on exact, case sensitive match. Database artefacts are named exactly
        /// like the .Net objects.
        /// </summary>
        public static readonly IMappingConvention Default = new MappingConvention(s => s, s => s, '@');
        /// <summary>
        /// Maps column names to property names based on case insensitive match, ignoring underscores. Database artefacts are named using
        /// UPPER_CASE_AND_UNDERSCORES
        /// </summary>
        public static readonly IMappingConvention OracleStyle = new MappingConvention(s => s.ToPascalCase(), s => s.ToUpperWithUnderscores(), ':');
        /// <summary>
        /// Maps column names to property names based on case insensitive match, ignoring underscores. Database artefacts are named using
        /// lower_case_and_underscores
        /// </summary>
        public static readonly IMappingConvention UnderScores = new MappingConvention(s => s.ToPascalCase(), s => s.ToLowerWithUnderscores(), '@');
        public string FromDb(string s) => _toDb(s);
        public string ToDb(string s) => _fromDb(s);
        public string Parameter(string s) => $"{_escape}{s}";
    }

    public static class MultiResultSet
    {
        public static MultiResultSet<T1, T2> Create<T1, T2>(IReadOnlyCollection<T1> set1, IReadOnlyCollection<T2> set2)
        {
            return new MultiResultSet<T1, T2>(set1, set2);
        }

        public static MultiResultSet<T1, T2, T3> Create<T1, T2, T3>(IReadOnlyCollection<T1> set1, IReadOnlyCollection<T2> set2, IReadOnlyCollection<T3> set3)
        {
            return new MultiResultSet<T1, T2, T3>(set1, set2, set3);
        }

        public static MultiResultSet<T1, T2, T3, T4> Create<T1, T2, T3, T4>(IReadOnlyCollection<T1> set1, IReadOnlyCollection<T2> set2, IReadOnlyCollection<T3> set3, IReadOnlyCollection<T4> set4)
        {
            return new MultiResultSet<T1, T2, T3, T4>(set1, set2, set3, set4);
        }

        public static MultiResultSet<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(IReadOnlyCollection<T1> set1, IReadOnlyCollection<T2> set2, IReadOnlyCollection<T3> set3, IReadOnlyCollection<T4> set4, IReadOnlyCollection<T5> set5)
        {
            return new MultiResultSet<T1, T2, T3, T4, T5>(set1, set2, set3, set4, set5);
        }
    }

    public sealed class MultiResultSet<T1, T2>
    {
        public MultiResultSet(IReadOnlyCollection<T1> set1, IReadOnlyCollection<T2> set2)
        {
            Set1 = set1;
            Set2 = set2;
        }

        public IReadOnlyCollection<T1> Set1
        {
            get;
        }

        public IReadOnlyCollection<T2> Set2
        {
            get;
        }
    }

    public sealed class MultiResultSet<T1, T2, T3>
    {
        public MultiResultSet(IReadOnlyCollection<T1> set1, IReadOnlyCollection<T2> set2, IReadOnlyCollection<T3> set3)
        {
            Set1 = set1;
            Set2 = set2;
            Set3 = set3;
        }

        public IReadOnlyCollection<T1> Set1
        {
            get;
        }

        public IReadOnlyCollection<T2> Set2
        {
            get;
        }

        public IReadOnlyCollection<T3> Set3
        {
            get;
        }
    }

    public sealed class MultiResultSet<T1, T2, T3, T4>
    {
        public MultiResultSet(IReadOnlyCollection<T1> set1, IReadOnlyCollection<T2> set2, IReadOnlyCollection<T3> set3, IReadOnlyCollection<T4> set4)
        {
            Set1 = set1;
            Set2 = set2;
            Set3 = set3;
            Set4 = set4;
        }

        public IReadOnlyCollection<T1> Set1
        {
            get;
        }

        public IReadOnlyCollection<T2> Set2
        {
            get;
        }

        public IReadOnlyCollection<T3> Set3
        {
            get;
        }

        public IReadOnlyCollection<T4> Set4
        {
            get;
        }
    }

    public sealed class MultiResultSet<T1, T2, T3, T4, T5>
    {
        public MultiResultSet(IReadOnlyCollection<T1> set1, IReadOnlyCollection<T2> set2, IReadOnlyCollection<T3> set3, IReadOnlyCollection<T4> set4, IReadOnlyCollection<T5> set5)
        {
            Set1 = set1;
            Set2 = set2;
            Set3 = set3;
            Set4 = set4;
            Set5 = set5;
        }

        public IReadOnlyCollection<T1> Set1
        {
            get;
        }

        public IReadOnlyCollection<T2> Set2
        {
            get;
        }

        public IReadOnlyCollection<T3> Set3
        {
            get;
        }

        public IReadOnlyCollection<T4> Set4
        {
            get;
        }

        public IReadOnlyCollection<T5> Set5
        {
            get;
        }
    }

    public static class StringExtensions
    {
        public static string ToUpperRemoveSpecialChars(this string str) => string.IsNullOrEmpty(str) ? str : Regex.Replace(str, @"([^\w]|_)", "").ToUpperInvariant();
        public static string ToPascalCase(this string str) => string.IsNullOrEmpty(str) ? str : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(str.ToLower()).Replace("_", "");
        public static string PascalCaseToSentence(this string source) => string.IsNullOrEmpty(source) ? source : string.Join(" ", SplitUpperCase(source));
        public static string ToUpperWithUnderscores(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;
            return string.Join("_", SplitUpperCase(source).Select(s => s.ToUpperInvariant()));
        }

        public static string ToLowerWithUnderscores(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;
            return string.Join("_", SplitUpperCase(source).Select(s => s.ToLowerInvariant()));
        }

        static IEnumerable<string> SplitUpperCase(string source)
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
}

namespace Net.Code.ADONet.Extensions
{
    public static class DbExtensions
    {
        /// <summary>
        /// Please note: this is an experimental feature, API may change or be removed in future versions"
        /// </summary>
        public static void Insert<T>(this IDb db, IEnumerable<T> items)
        {
            var query = Query<T>.Create(db.ProviderName).Insert;
            Do(db, items, query);
        }

        /// <summary>
        /// Please note: this is an experimental feature, API may change or be removed in future versions"
        /// </summary>
        public static void Update<T>(this IDb db, IEnumerable<T> items)
        {
            var query = Query<T>.Create(db.ProviderName).Update;
            Do(db, items, query);
        }

        /// <summary>
        /// Please note: this is an experimental feature, API may change or be removed in future versions"
        /// </summary>
        public static void Delete<T>(this IDb db, IEnumerable<T> items)
        {
            var query = Query<T>.Create(db.ProviderName).Delete;
            Do(db, items, query);
        }

        private static void Do<T>(IDb db, IEnumerable<T> items, string query)
        {
            var commandBuilder = db.Sql(query);
            foreach (var item in items)
            {
                commandBuilder.WithParameters(item).AsNonQuery();
            }
        }
    }

    public interface IQueryGenerator
    {
        string Insert
        {
            get;
        }

        string Delete
        {
            get;
        }

        string Update
        {
            get;
        }

        string Select
        {
            get;
        }

        string SelectAll
        {
            get;
        }

        string Count
        {
            get;
        }
    }

    public class Query<T> : IQueryGenerator
    {
        private string _insert;
        private string _delete;
        private string _update;
        private string _selectAll;
        private string _select;
        private string _count;
        public static IQueryGenerator Create(string providerName) => Create(DbConfig.FromProviderName(providerName).MappingConvention);
        internal static IQueryGenerator Create(IMappingConvention convention) => new Query<T>(convention);
        Query(IMappingConvention convention)
        {
            var properties = typeof (T).GetProperties();
            var keyProperties = properties.Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof (KeyAttribute))).ToArray();
            if (!keyProperties.Any())
                keyProperties = properties.Where(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (!keyProperties.Any())
                keyProperties = properties.Where(p => p.Name.Equals($"{typeof (T).Name}Id", StringComparison.OrdinalIgnoreCase)).ToArray();
            var dbGenerated = keyProperties.Where(p => p.HasCustomAttribute<DatabaseGeneratedAttribute>(a => a.DatabaseGeneratedOption != DatabaseGeneratedOption.None));
            var allPropertyNames = properties.Select(p => convention.ToDb(p.Name)).ToArray();
            var insertPropertyNames = properties.Except(dbGenerated).Select(p => p.Name).ToArray();
            var keyPropertyNames = keyProperties.Select(p => p.Name).ToArray();
            var nonKeyProperties = properties.Except(keyProperties).ToArray();
            var nonKeyPropertyNames = nonKeyProperties.Select(p => p.Name).ToArray();
            Func<string, string> assign = s => $"{convention.ToDb(s)} = {convention.Parameter(s)}";
            var insertColumns = string.Join(", ", insertPropertyNames.Select(convention.ToDb));
            var insertValues = string.Join(", ", insertPropertyNames.Select(s => $"{convention.Parameter(s)}"));
            var whereClause = string.Join(" AND ", keyPropertyNames.Select(assign));
            var updateColumns = string.Join(", ", nonKeyPropertyNames.Select(assign));
            var allColumns = string.Join(", ", allPropertyNames);
            var tableName = convention.ToDb(typeof (T).Name);
            _insert = $"INSERT INTO {tableName} ({insertColumns}) VALUES ({insertValues})";
            _delete = $"DELETE FROM {tableName} WHERE {whereClause}";
            _update = $"UPDATE {tableName} SET {updateColumns} WHERE {whereClause}";
            _select = $"SELECT {allColumns} FROM {tableName} WHERE {whereClause}";
            _selectAll = $"SELECT {allColumns} FROM {tableName}";
            _count = $"SELECT COUNT(*) FROM {tableName}";
        }

        public string Insert => _insert;
        public string Delete => _delete;
        public string Update => _update;
        public string Select => _select;
        public string SelectAll => _selectAll;
        public string Count => _count;
    }

    internal static class TypeExtensions
    {
        public static bool HasCustomAttribute<TAttribute>(this MemberInfo t, Func<TAttribute, bool> whereClause) => t.GetCustomAttributes(false).OfType<TAttribute>().Where(whereClause).Any();
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
            {TypeName = udtTypeName, Value = dataTable};
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
            using (var bcp = new SqlBulkCopy(db.ConnectionString))
            {
                bcp.DestinationTableName = typeof (T).Name;
                // by default, SqlBulkCopy assumes columns in the database 
                // are in same order as the columns of the source data reader
                // => add explicit column mappings by name
                foreach (var p in typeof (T).GetProperties())
                {
                    bcp.ColumnMappings.Add(p.Name, p.Name);
                }

                var dataTable = items.AsDataReader();
                bcp.WriteToServer(dataTable);
            }
        }
    }
}