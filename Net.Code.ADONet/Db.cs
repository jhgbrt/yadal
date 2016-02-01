// to support older C#/.Net versions, undefine some of these 
#define DYNAMIC
#define ASYNC

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Reflection;
#if ASYNC
using System.Threading.Tasks;
#endif
#if DYNAMIC
using Microsoft.CSharp.RuntimeBinder;
#endif
#if DEBUG 
using System.Diagnostics;
#endif

// Yet Another Data Access Layer
// usage: 
//   using (var db = new Db()) {}; 
//   using (var db = new Db(connectionString)) {}; 
//   using (var db = new Db(connectionString, providerName)) {}; 
//   using (var db = Db.FromConfig());
// from there it should be discoverable.
// inline SQL FTW!
namespace Net.Code.ADONet
{
    public interface IDb : IDisposable
    {
        void Connect();
        /// <summary>
        /// The actual IDbConnection (which will be open)
        /// </summary>
        IDbConnection Connection { get; }

        /// <summary>
        /// The ADO.Net connection string
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// The ADO.Net ProviderName for this connection
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Entry point for configuring the db with provider-specific stuff.
        /// </summary>
        /// <returns></returns>
        
        /// <summary>
        /// Extension point for custom configuration of the db connection
        /// </summary>
        IDbConfigurationBuilder Configure();

        /// <summary>
        /// Create a SQL query command builder
        /// </summary>
        /// <param name="sqlQuery"></param>
        /// <returns>a CommandBuilder instance</returns>
        CommandBuilder Sql(string sqlQuery);

        /// <summary>
        /// Create a Stored Procedure command
        /// </summary>
        /// <param name="sprocName">name of the stored procedure</param>
        /// <returns>a CommandBuilder instance</returns>
        CommandBuilder StoredProcedure(string sprocName);

        /// <summary>
        /// Create a SQL command and execute it immediately (non query)
        /// </summary>
        /// <param name="command"></param>
        int Execute(string command);
    }

    /// <summary>
    /// To enable logging, set the Log property of the Logger class
    /// </summary>
    public class Logger
    {
#if DEBUG
        public static Action<string> Log = s => { Debug.WriteLine(s); };
#else
        public static Action<string> Log;
#endif

        internal static void LogCommand(IDbCommand command)
        {
            if (Log == null) return;
            Log(command.CommandText);
            foreach (IDbDataParameter p in command.Parameters)
            {
                Log(string.Format("{0} = {1}", p.ParameterName, p.Value));
            }
        }
    }

    public interface IConnectionFactory
    {
        /// <summary>
        /// Create the ADO.Net IDbConnection
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>the connection</returns>
        IDbConnection CreateConnection(string connectionString);
    }

    public interface IDbConfigurationBuilder
    {
        /// <summary>
        /// Provides a hook to configure an ADO.Net DbCommmand just after it is created. 
        /// For example, the Oracle.DataAccess API requires the BindByName property to be set
        /// to true for the datareader to enable named access to the result columns (Note that 
        /// for this situation you don't need to do anything, it's handled by default).
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IDbConfigurationBuilder OnPrepareCommand(Action<IDbCommand> action);
    }

    class DbConfigurationBuilder : IDbConfigurationBuilder
    {
        private readonly DbConfig _dbConfig;
 
        internal DbConfigurationBuilder(DbConfig dbConfig)
        {
            _dbConfig = dbConfig;
        }

        public IDbConfigurationBuilder OnPrepareCommand(Action<IDbCommand> action)
        {
            _dbConfig.PrepareCommand = action;
            return this;
        }

        private DbConfigurationBuilder Default()
        {
            OnPrepareCommand(a => {});
            return this;
        }

        private DbConfigurationBuilder SqlServer()
        {
            OnPrepareCommand(command => { });
            return this;
        }
        class Option<T>
        {
            public bool HasValue { get; private set; }
            public T Value { get; private set; }

            public void SetValue(T v)
            {
                Value = v;
                HasValue = true;
            }
        }
        private static Option<PropertyInfo> _bindByName = new Option<PropertyInfo>();
        private DbConfigurationBuilder Oracle()
        {
            // By default, the Oracle driver does not support binding parameters by name;
            // one has to set the BindByName property on the OracleDbCommand.
            // Since we don't want to have a hard reference to Oracle.DataAccess here,
            // we use reflection.
            // The day Oracle decides to make a breaking change, this will blow up with 
            // a runtime exception
            OnPrepareCommand(command =>
            {
                if (!_bindByName.HasValue)
                {
                    _bindByName.SetValue(command.GetType().GetProperty("BindByName"));
                }
                if (_bindByName.Value != null)
                {
                    _bindByName.Value.SetValue(command, true);
                }
            });
            return this;
        }

        public DbConfigurationBuilder FromProviderName(string providerName)
        {
            switch (providerName)
            {
                case "Oracle.DataAccess.Client":
                case "Oracle.ManagedDataAccess.Client":
                    return Oracle();
                case "System.Data.SqlClient":
                    return SqlServer();
                default :
                    return Default();

            }
        }
    }

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

    public class DbConfig
    {
        private static readonly Action<IDbCommand> Empty = c => {};

        public DbConfig() : this(Empty)
        {
        }

        public DbConfig(Action<IDbCommand> prepareCommand)
        {
            PrepareCommand = prepareCommand;
        }

        public Action<IDbCommand> PrepareCommand { get; internal set; }
    }

    /// <summary>
    /// A class that wraps a database.
    /// </summary>
    public class Db : IDb
    {
        private readonly DbConfig _config = new DbConfig();

        public string ProviderName { get { return _providerName; }}

        public IDbConfigurationBuilder Configure()
        {
            return ConfigurePriv();
        }

        private DbConfigurationBuilder ConfigurePriv()
        {
            return new DbConfigurationBuilder(_config);
        }

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
        /// Instantiate Db with existing connection. The connection is only used for creating commands; it should be disposed by the caller when done.
        /// </summary>
        /// <param name="connection">The existing connection</param>
        /// <param name="providerName">The ADO .Net Provider name. When not specified, the default value is used (see DefaultProviderName)</param>
        public Db(IDbConnection connection, string providerName = null)
        {
            _externalConnection = connection;
            ConfigurePriv().FromProviderName(providerName);
        }

        /// <summary>
        /// Instantiate Db with connectionString and DbProviderName
        /// </summary>
        /// <param name="connectionString">The connection string</param>
        /// <param name="providerName">The ADO .Net Provider name. When not specified, the default value is used (see DefaultProviderName)</param>
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
            ConfigurePriv().FromProviderName(_providerName);
        }


        /// <summary>
        /// Factory method, instantiating the Db class from the first connectionstring in the app.config or web.config file.
        /// </summary>
        /// <returns>Db</returns>
        public static Db FromConfig()
        {
            var connectionStrings = ConfigurationManager.ConnectionStrings;
            var connectionStringSettings = connectionStrings[0];
            return FromConfig(connectionStringSettings);
        }

        /// <summary>
        /// Factory method, instantiating the Db class from a named connectionstring in the app.config or web.config file.
        /// </summary>
        public static Db FromConfig(string connectionStringName)
        {
            var connectionStrings = ConfigurationManager.ConnectionStrings;
            return FromConfig(connectionStrings[connectionStringName]);
        }

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
                if (dbConnection.State == ConnectionState.Closed) 
                    dbConnection.Open();
                return dbConnection;
            }
        }

        public string ConnectionString
        {
            get { return _connectionString; }
        }

        private IDbConnection CreateConnection()
        {
            var connection = _connectionFactory.CreateConnection(_connectionString);
            return connection;
        }

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
        public CommandBuilder Sql(string sqlQuery)
        {
            return CreateCommand(CommandType.Text, sqlQuery);
        }

        /// <summary>
        /// Create a Stored Procedure command
        /// </summary>
        /// <param name="sprocName">name of the sproc</param>
        /// <returns>a CommandBuilder instance</returns>
        public CommandBuilder StoredProcedure(string sprocName)
        {
            return CreateCommand(CommandType.StoredProcedure, sprocName);
        }

        private CommandBuilder CreateCommand(CommandType commandType, string command)
        {
            var cmd = Connection.CreateCommand();
            _config.PrepareCommand(cmd);
            return new CommandBuilder(cmd).OfType(commandType).WithCommandText(command);
        }

        /// <summary>
        /// Create a SQL command and execute it immediately (non query)
        /// </summary>
        /// <param name="command"></param>
        public int Execute(string command)
        {
            return Sql(command).AsNonQuery();
        }
    }

    static class DataReaderExtensions
    {
        public static IEnumerable<IDataRecord> AsEnumerable(this IDataReader reader)
        {
            using (reader) { while (reader.Read()) yield return reader; }
        }

#if DYNAMIC
        public static IEnumerable<dynamic> ToExpandoList(this IEnumerable<IDataRecord> input)
        {
            return from item in input select item.ToExpando();
        }

        public static IEnumerable<dynamic> ToDynamicDataRecord(this IEnumerable<IDataRecord> input)
        {
            return from item in input select Dynamic.DataRecord(item);
        }

        public static IEnumerable<IEnumerable<dynamic>> ToMultiResultSet(this IDataReader reader)
        {
            do
            {
                yield return GetResultSet(reader);
            } while (reader.NextResult());
        }

        private static IEnumerable<dynamic> GetResultSet(IDataReader reader)
        {
            // need to materialize the record into an Expando here
            while (reader.Read()) yield return reader.ToExpando();
        }
#endif
    }

    public class CommandBuilder
    {
        private readonly IDbCommand _command;

        public CommandBuilder(IDbCommand command)
        {
            _command = command;
        }


        /// <summary>
        /// The raw IDbCommand instance
        /// </summary>
        public IDbCommand Command
        {
            get { return _command; }
        }

#if DYNAMIC 
        /// <summary>
        /// Executes the query and returns the result as a list of dynamic objects. 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<dynamic> AsEnumerable()
        {
            return Execute().Reader().AsEnumerable().ToExpandoList();
        }
        /// <summary>
        /// Executes the query and returns the result as a list of [T]. This method is slightly faster. 
        /// than doing AsEnumerable().Select(selector). The selector is required to map objects as the 
        /// underlying datareader is enumerated.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="selector">mapping function that transforms a datarecord (wrapped as a dynamic object) to an instance of type [T]</param>
        /// <returns></returns>
        public IEnumerable<T> AsEnumerable<T>(Func<dynamic, T> selector)
        {
            return Select(selector);
        }

        // enables linq 'select' syntax
        public IEnumerable<T> Select<T>(Func<dynamic, T> selector)
        {
            return Execute().Reader().AsEnumerable().ToDynamicDataRecord().Select(selector);
        }

        /// <summary>
        /// Executes the query and returns the result as a list of lists
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IEnumerable<dynamic>> AsMultiResultSet()
        {
            using (var reader = Execute().Reader())
            {
                return reader.ToMultiResultSet();
            }
        }
#endif
        /// <summary>
        /// Executes the command, returning the first column of the first result, converted to the type T
        /// </summary>
        /// <typeparam name="T">return type</typeparam>
        /// <returns></returns>
        public T AsScalar<T>()
        {
            var result = Execute().Scalar();
            return ConvertTo<T>.From(result);
        }

        public object AsScalar()
        {
            return Execute().Scalar();
        }

        /// <summary>
        /// Executes the command as a SQL statement, returning the number of rows affected
        /// </summary>
        public int AsNonQuery()
        {
            return Execute().NonQuery();
        }

        private Executor Execute()
        {
            return new Executor(_command);
        }

#if ASYNC
        /// <summary>
        /// Executes the command as a statement, returning the number of rows affected asynchronously
        /// This method is only supported if the underlying provider uses the ADO.Net base classes (i.e., their IDbCommand implementation
        /// inherits from System.Data.DbCommand). Moreover, this async method only makes sense if the provider
        /// implements the async behaviour by overriding the appropriate method.
        /// </summary>
        /// <returns></returns>
        public Task<int> AsNonQueryAsync()
        {
            return ExecuteAsync().NonQuery();
        }

        /// <summary>
        /// Executes the command, returning the first column of the first result, converted to the type T asynchronously. 
        /// This method is only supported if the underlying provider uses the ADO.Net base classes (i.e., their IDbCommand implementation
        /// inherits from System.Data.DbCommand). Moreover, this async method only makes sense if the provider
        /// implements the async behaviour by overriding the appropriate method.
        /// </summary>
        /// <typeparam name="T">return type</typeparam>
        /// <returns></returns>
        public async Task<T> AsScalarAsync<T>()
        {
            var result = await ExecuteAsync().Scalar();
            return ConvertTo<T>.From(result);
        }

#if DYNAMIC
        /// <summary>
        /// Executes the query and returns the result as a list of dynamic objects asynchronously
        /// This method is only supported if the underlying provider uses the ADO.Net base classes (i.e., their IDbCommand implementation
        /// inherits from System.Data.DbCommand). Moreover, this async method only makes sense if the provider
        /// implements the async behaviour by overriding the appropriate method.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<dynamic>> AsEnumerableAsync()
        {
            var reader = await ExecuteAsync().Reader();
            return reader.AsEnumerable().ToExpandoList();
        }

        /// <summary>
        /// Executes the query and returns the result as a list of [T] asynchronously
        /// This method is only supported if the underlying provider uses the ADO.Net base classes (i.e., their IDbCommand implementation
        /// inherits from System.Data.DbCommand). Moreover, this async method only makes sense if the provider
        /// implements the async behaviour by overriding the appropriate method.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="selector">mapping function that transforms a datarecord (wrapped as a dynamic object) to an instance of type [T]</param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> AsEnumerableAsync<T>(Func<dynamic, T> selector)
        {
            var reader = await ExecuteAsync().Reader();
            return reader.AsEnumerable().ToDynamicDataRecord().Select(selector);
        }

        /// <summary>
        /// Executes the query and returns the result as a list of lists asynchronously
        /// This method is only supported if the underlying provider uses the ADO.Net base classes (i.e., their IDbCommand implementation
        /// inherits from System.Data.DbCommand). Moreover, this async method only makes sense if the provider
        /// implements the async behaviour by overriding the appropriate method.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<IEnumerable<dynamic>>> AsMultiResultSetAsync()
        {
            using (var reader = await ExecuteAsync().Reader())
            {
                return reader.ToMultiResultSet();
            }
        }
#endif
        private AsyncExecutor ExecuteAsync()
        {
            return new AsyncExecutor((DbCommand) _command);
        }
#endif // ASYNC

        /// <summary>
        /// Sets the command text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public CommandBuilder WithCommandText(string text)
        {
            _command.CommandText = text;
            return this;
        }

        /// <summary>
        /// Sets the command type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public CommandBuilder OfType(CommandType type)
        {
            _command.CommandType = type;
            return this;
        }

        /// <summary>
        /// Adds a parameter for each property of the given object, with the property name as the name of the parameter 
        /// and the property value as the corresponding parameter value
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public CommandBuilder WithParameters(object parameters)
        {
            var props = parameters.GetType().GetProperties();
            foreach (var item in props)
            {
                WithParameter(item.Name, item.GetValue(parameters, null));
            }
            return this;
        }

        /// <summary>
        /// Builder method - sets the command timeout
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
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

            IDataParameter p;
            if (Command.Parameters.Contains(name))
            {
                p = (IDbDataParameter) Command.Parameters[name];
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

        /// <summary>
        /// Builder method - adds a table-valued parameter. Only supported on SQL Server (System.Data.SqlClient)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">parameter name</param>
        /// <param name="values">list of values</param>
        /// <param name="udtTypeName">name of the user-defined table type</param>
        /// <returns></returns>
        public CommandBuilder WithParameter<T>(string name, IEnumerable<T> values, string udtTypeName)
        {
            var dataTable = values.ToDataTable();

            var p = new SqlParameter(name, SqlDbType.Structured)
            {
                TypeName = udtTypeName,
                Value = dataTable
            };

            Command.Parameters.Add(p);
            return this;
        }

        /// <summary>
        /// Executes the command as a datareader. Use this if you need best performance.
        /// </summary>
        /// <returns></returns>
        public IDataReader AsReader()
        {
            return Execute().Reader();
        }

        public DataTable AsDataTable()
        {
            return Execute().DataTable();
        }

        public CommandBuilder InTransaction(IDbTransaction tx)
        {
            Command.Transaction = tx;
            return this;
        }
    }

    public class Executor
    {
        private readonly IDbCommand _command;

        public Executor(IDbCommand command)
        {
            _command = command;
        }

        /// <summary>
        /// executes the query as a datareader
        /// </summary>
        /// <returns></returns>
        public IDataReader Reader()
        {
            return Prepare().ExecuteReader();
        }

        /// <summary>
        /// Executes the query (using datareader) and fills a datatable
        /// </summary>
        /// <returns></returns>
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
        /// <returns></returns>
        public object Scalar()
        {
            var result = Prepare().ExecuteScalar();
            return result;
        }

        /// <summary>
        /// Executes the command as a SQL statement, returning the number of rows affected
        /// </summary>
        public int NonQuery()
        {
            return Prepare().ExecuteNonQuery();
        }

        private IDbCommand Prepare()
        {
            Logger.LogCommand(_command);
            if (_command.Connection.State == ConnectionState.Closed)
                _command.Connection.Open();
            return _command;
        }

    }

#if ASYNC
    public class AsyncExecutor
    {
        private readonly DbCommand _command;

        public AsyncExecutor(DbCommand command)
        {
            _command = command;
        }

        /// <summary>
        /// executes the query as a datareader
        /// </summary>
        /// <returns></returns>
        public async Task<IDataReader> Reader()
        {
            var command = await PrepareAsync();
            return await command.ExecuteReaderAsync();
        }

        /// <summary>
        /// Executes the command, returning the first column of the first result as a scalar value
        /// </summary>
        /// <returns></returns>
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
#endif
    public static class EnumerableToDatatable
    {

        public static DataTable ToDataTable<T>(this IEnumerable<T> items)
        {
            var table = new DataTable(typeof(T).Name);

            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                var propType = prop.PropertyType;

                if (DBNullHelper.IsNullableType(propType))
                    propType = new NullableConverter(propType).UnderlyingType;

                table.Columns.Add(prop.Name, propType);
            }

            foreach (var item in items)
            {
                var values = new object[props.Length];
                for (var i = 0; i < props.Length; i++)
                    values[i] = props[i].GetValue(item, null);

                table.Rows.Add(values);
            }
            return table;
        }
    }

    static class Dynamic
    {
        public static dynamic DataRow(DataRow row)
        {
            return From(row, (r, s) => r[s]);
        }
        public static dynamic DataRecord(IDataRecord record)
        {
            return From(record, (r, s) => r[s]);
        }
        public static dynamic Dictionary<TValue>(IReadOnlyDictionary<string, TValue> dictionary)
        {
            return From(dictionary, (d, s) => d[s]);
        }
        static dynamic From<T>(T item, Func<T, string, object> getter)
        {
            return new DynamicIndexer<T>(item, getter);
        }
        class DynamicIndexer<T> : DynamicObject
        {
            private readonly T _item;
            private readonly Func<T, string, object> _getter;

            public DynamicIndexer(T item, Func<T, string, object> getter)
            {
                _item = item;
                _getter = getter;
            }

            public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
            {
                var memberName = (string)indexes[0];
                return ByMemberName(out result, memberName);
            }

            public sealed override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                var memberName = binder.Name;
                return ByMemberName(out result, memberName);
            }

            private bool ByMemberName(out object result, string memberName)
            {
                var value = _getter(_item, memberName);
                result = DBNullHelper.FromDb(value);
                return true;
            }
        }
    }
    
    public static class DataTableExtensions
    {
        static dynamic ToDynamic(this DataRow dr)
        {
            return Dynamic.DataRow(dr);
        }
        public static IEnumerable<dynamic> AsEnumerable(this DataTable dataTable)
        {
            return dataTable.Rows.OfType<DataRow>().Select(ToDynamic);
        }
        public static IEnumerable<T> Select<T>(this DataTable dt, Func<dynamic, T> selector)
        {
            return dt.AsEnumerable().Select(selector);
        }
        public static IEnumerable<dynamic> Where(this DataTable dt, Func<dynamic, bool> predicate)
        {
            return dt.AsEnumerable().Where(predicate);
        }
    }

    public static class DataRecordExtensions
    {
        /// <summary>
        /// Convert a datarecord into a dynamic object, so that properties can be simply accessed
        /// using standard C# syntax.
        /// </summary>
        /// <param name="rdr">the data record</param>
        /// <returns>A dynamic object with fields corresponding to the database columns</returns>
        public static dynamic ToExpando(this IDataRecord rdr)
        {
            var d = new Dictionary<string,object>();
            for (var i = 0; i < rdr.FieldCount; i++)
            {
                var name = rdr.GetName(i);
                var value = rdr[i];
                d.Add(name, value);
            }
            return Dynamic.Dictionary(d);
        }

        /// <summary>
        /// Get a value from an IDataRecord by column name. This method supports all types,
        /// as long as the DbType is convertible to the CLR Type passed as a generic argument.
        /// Also handles conversion from DbNull to null, including nullable types.
        /// </summary>
        public static TResult Get<TResult>(this IDataRecord reader, string name)
        {
            var c = reader.GetOrdinal(name);
            return reader.Get<TResult>(c);
        }

        /// <summary>
        /// Get a value from an IDataRecord by index. This method supports all types,
        /// as long as the DbType is convertible to the CLR Type passed as a generic argument.
        /// Also handles conversion from DbNull to null, including nullable types.
        /// </summary>
        public static TResult Get<TResult>(this IDataRecord reader, int c)
        {
            return ConvertTo<TResult>.From(reader[c]);
        }
    }

    // refined this after finding somewhere on the interweb but can't find original source anymore
    // this class is a helper class for the Get<> extension method on IDataRecord
    // not needed if you use ToDynamic()
    public static class ConvertTo<T>
    {
        // ReSharper disable once StaticFieldInGenericType
        // clearly we *want* a static field for each instantiation of this generic class...
        /// <summary>
        /// The actual conversion method. Converts an object to any type using standard casting functionality, taking into account null/nullable types
        /// and avoiding DBNull issues. This method is set as a delegate at runtime (in the static constructor).
        /// </summary>
        public static readonly Func<object, T> From;

        /// <summary>
        /// Set the <see cref="From"/> delegate, depending on whether T is a reference type, a nullable value type or a value type.
        /// </summary>
        static ConvertTo()
        {
            From = CreateConvertFunction(typeof(T));
        }

        private static Func<object, T> CreateConvertFunction(Type type)
        {
            if (!type.IsValueType)
            {
                return ConvertRefType;
            }
            if (DBNullHelper.IsNullableType(type))
            {
                var delegateType = typeof(Func<object, T>);
                var methodInfo = typeof(ConvertTo<T>).GetMethod("ConvertNullableValueType", BindingFlags.NonPublic | BindingFlags.Static);
                var genericMethodForElement = methodInfo.MakeGenericMethod(type.GetGenericArguments()[0]);
                return (Func<object, T>)Delegate.CreateDelegate(delegateType, genericMethodForElement);
            }
            return ConvertValueType;
        }

        // ReSharper disable once UnusedMember.Local
        // (used via reflection!)
        private static TElem? ConvertNullableValueType<TElem>(object value) where TElem : struct
        {
            return DBNullHelper.IsNull(value) ? (TElem?)null : ConvertPrivate<TElem>(value);
        }

        private static T ConvertRefType(object value)
        {
            return DBNullHelper.IsNull(value) ? default(T) : ConvertPrivate<T>(value);
        }

        private static T ConvertValueType(object value)
        {
            if (DBNullHelper.IsNull(value))
            {
                throw new NullReferenceException("Value is DbNull");
            }
            return ConvertPrivate<T>(value);
        }

        private static TElem ConvertPrivate<TElem>(object value)
        {
            return (TElem)(Convert.ChangeType(value, typeof(TElem)));
        }

    }

    static class DBNullHelper
    {
        public static bool IsNullableType(Type type)
        {
            return
                (type.IsGenericType && !type.IsGenericTypeDefinition) &&
                (typeof(Nullable<>) == type.GetGenericTypeDefinition());
        }

        public static bool IsNull(object o)
        {
            return o == null || DBNull.Value.Equals(o);
        }

        public static object FromDb(object o)
        {
            return IsNull(o) ? null : o;
        }

        public static object ToDb(object o)
        {
            return IsNull(o) ? DBNull.Value : o;
        }

    }
}
