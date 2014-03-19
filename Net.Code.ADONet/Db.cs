using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;

// Yet Another Data Access Layer
// usage: 
//   using (var db = new Db()) {}; 
//   using (var db = Db.FromConfig());
// from there it should be discoverable.
// inline SQL FTW!
using System.Threading;
using System.Threading.Tasks;

namespace Net.Code.ADONet
{
    public interface IConnectionFactory
    {
        /// <summary>
        /// Create the ADO.Net IDbConnection
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>the connection</returns>
        IDbConnection CreateConnection(string connectionString);
    }

    public interface IDb : IDisposable
    {
        /// <summary>
        /// The actual IDbConnection (which will be open)
        /// </summary>
        IDbConnection Connection { get; }

        string ConnectionString { get; }

        /// <summary>
        /// Entry point for configuring the db with provider-specific stuff.
        /// Specifically, allows to set the async adapter
        /// </summary>
        /// <returns></returns>
        
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
        /// <param name="sprocName">name of the sproc</param>
        /// <returns>a CommandBuilder instance</returns>
        CommandBuilder StoredProcedure(string sprocName);

        /// <summary>
        /// Create a SQL command and execute it immediately (non query)
        /// </summary>
        /// <param name="command"></param>
        int Execute(string command);
    }

    public class Logger
    {
        public static Action<string> Log = s => Trace.WriteLine(s);
    }

    public interface IDbConfigurationBuilder
    {
        IDbConfigurationBuilder SetAsyncAdapter(IAsyncAdapter asyncAdapter);
        IDbConfigurationBuilder OnPrepareCommand(Action<IDbCommand> action);
    }

    class DbConfigurationBuilder : IDbConfigurationBuilder
    {
        private readonly DbConfig _dbConfig;
 
        internal DbConfigurationBuilder(DbConfig dbConfig)
        {
            _dbConfig = dbConfig;
        }

        public IDbConfigurationBuilder SetAsyncAdapter(IAsyncAdapter asyncAdapter)
        {
            _dbConfig.AsyncAdapter = asyncAdapter;
            return this;
        }

        public IDbConfigurationBuilder OnPrepareCommand(Action<IDbCommand> action)
        {
            _dbConfig.PrepareCommand = action;
            return this;
        }

        public DbConfigurationBuilder Default()
        {
            SetAsyncAdapter(new NotSupportedAsyncAdapter());
            OnPrepareCommand(a => {});
            return this;
        }

        public DbConfigurationBuilder SqlServer()
        {
            SetAsyncAdapter(new SqlAsyncAdapter());
            OnPrepareCommand(a => { });
            return this;
        }

        public DbConfigurationBuilder Oracle()
        {
            SetAsyncAdapter(new NotSupportedAsyncAdapter());
            OnPrepareCommand(command =>
                             {
                                 dynamic c = command;
                                 c.BindByName = true;
                             });
            return this;
        }

        public DbConfigurationBuilder FromProviderName(string providerName)
        {
            switch (providerName)
            {
                case "Oracle.DataAccess.Client":
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

    internal class DbConfig
    {
        public Action<IDbCommand> PrepareCommand { get; internal set; }
        public IAsyncAdapter AsyncAdapter { get; internal set; }
    }

    /// <summary>
    /// A class that wraps a database.
    /// </summary>
    public class Db : IDb
    {
        private readonly DbConfig _config = new DbConfig();

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

        /// <summary>
        /// Instantiate Db with existing connection. The connection is only used for creating commands; it should be disposed by the caller when done.
        /// </summary>
        /// <param name="connection">The existing connection</param>
        /// <param name="providerName"></param>
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
            : this(connectionString, new AdoNetProviderFactory(providerName ?? DefaultProviderName), providerName)
        {
        }

        /// <summary>
        /// Instantiate Db with connectionString and a custom IConnectionFactory
        /// </summary>
        /// <param name="connectionString">the connection string</param>
        /// <param name="connectionFactory">the connection factory</param>
        public Db(string connectionString, IConnectionFactory connectionFactory, string providerName = null)
        {
            _connectionString = connectionString;
            _connectionFactory = connectionFactory;
            _connection = new Lazy<IDbConnection>(CreateConnection);
            var providerInvariantName = providerName ?? DefaultProviderName;
            ConfigurePriv().FromProviderName(providerInvariantName);
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

        /// <summary>
        /// The actual IDbConnection (which will be open)
        /// </summary>
        public IDbConnection Connection { get { return _externalConnection ?? _connection.Value; } }

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
            return new CommandBuilder(cmd, _config.AsyncAdapter).OfType(commandType).WithCommandText(command);
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

        public static IEnumerable<dynamic> ToDynamic(this IEnumerable<IDataRecord> input)
        {
            return from item in input select item.ToExpando();
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
            while (reader.Read()) yield return reader.ToExpando();
        }
    }

    public class CommandBuilder
    {
        private readonly IDbCommand _command;
        private readonly IAsyncAdapter _asyncAdapter;

        public CommandBuilder(IDbCommand command) : this(command, null)
        {
        }
 
        public CommandBuilder(IDbCommand command, IAsyncAdapter asyncAdapter)
        {
            _command = command;
            _asyncAdapter = asyncAdapter;
        }

        /// <summary>
        /// The raw IDbCommand instance
        /// </summary>
        public IDbCommand Command
        {
            get { return _command; }
        }

        /// <summary>
        /// executes the query and returns the result as a list of dynamic objects
        /// </summary>
        /// <returns></returns>
        public IEnumerable<dynamic> AsEnumerable()
        {
            return Execute().Reader().AsEnumerable().ToDynamic();
        }

        /// <summary>
        /// executes the query and returns the result as a list of lists
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IEnumerable<dynamic>> AsMultiResultSet()
        {
            using (var reader = Execute().Reader())
            {
                return reader.ToMultiResultSet();
            }
        }

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

        /// <summary>
        /// Executes the command as a SQL statement, not returning any results
        /// </summary>
        public int AsNonQuery()
        {
            return Execute().NonQuery();
        }

        private Executor Execute()
        {
            Log();
            return new Executor(_command);
        }

        public async Task<int> AsNonQueryAsync()
        {
            await PrepareAsync();
            return await AsyncAdapter.ExecuteNonQueryAsync(_command);
        }

        public async Task<T> AsScalarAsync<T>()
        {
            await PrepareAsync();
            var result = await AsyncAdapter.ExecuteScalarAsync(_command);
            return ConvertTo<T>.From(result);
        }

        public async Task<IEnumerable<dynamic>> AsEnumerableAsync()
        {
            await PrepareAsync();
            var reader = await AsyncAdapter.ExecuteReaderAsync(_command);
            return reader.AsEnumerable().ToDynamic();
        }

        public async Task<IEnumerable<IEnumerable<dynamic>>> AsMultiResultSetAsync()
        {
            await PrepareAsync();
            using (var reader = await AsyncAdapter.ExecuteReaderAsync(_command))
            {
                return reader.ToMultiResultSet();
            }
        }

        private async Task PrepareAsync()
        {
            Log();
            await AsyncAdapter.OpenConnectionAsync(Command.Connection);
        }

        private void Log()
        {
            Logger.Log(Command.CommandText);
            if (Command.Parameters != null) foreach (IDbDataParameter p in Command.Parameters)
            {
                Logger.Log(string.Format("{0} = {1}", p.ParameterName, p.Value));
            }
        }


        private IAsyncAdapter AsyncAdapter
        {
            get { return _asyncAdapter; }
        }

        public CommandBuilder WithCommandText(string text)
        {
            _command.CommandText = text;
            return this;
        }

        public CommandBuilder OfType(CommandType type)
        {
            _command.CommandType = type;
            return this;
        }

        public CommandBuilder WithParameters(dynamic parameters)
        {
            object o = parameters;
            var props = o.GetType().GetProperties();
            foreach (var item in props)
            {
                WithParameter(item.Name, item.GetValue(o, null));
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
            var p = Command.CreateParameter();
            p.ParameterName = name;
            p.Value = DBNullHelper.ToDb(value);
            Command.Parameters.Add(p);
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

    }

    public class Executor
    {
        private IDbCommand _command;

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
            Logger.Log(_command.CommandText);
            foreach (IDbDataParameter p in _command.Parameters)
            {
                Logger.Log(string.Format("{0} = {1}", p.ParameterName, p.Value));
            }
            if (_command.Connection.State == ConnectionState.Closed)
                _command.Connection.Open();
            return _command;
        }

    }

    public interface IAsyncAdapter
    {
        Task<int> ExecuteNonQueryAsync(IDbCommand command);
        Task<object> ExecuteScalarAsync(IDbCommand command);
        Task<IDataReader> ExecuteReaderAsync(IDbCommand command);
        Task OpenConnectionAsync(IDbConnection connection);
    }

    public class SqlAsyncAdapter : IAsyncAdapter
    {
        public async Task<int> ExecuteNonQueryAsync(IDbCommand command)
        {
            var sqlCommand = (SqlCommand)command;
            var result = await sqlCommand.ExecuteNonQueryAsync(CancellationToken.None);
            return result;
        }

        public async Task<object> ExecuteScalarAsync(IDbCommand command)
        {
            var sqlCommand = (SqlCommand)command;
            var result = await sqlCommand.ExecuteScalarAsync();
            return result;
        }

        public async Task<IDataReader> ExecuteReaderAsync(IDbCommand command)
        {
            var sqlCommand = (SqlCommand)command;
            var result = await sqlCommand.ExecuteReaderAsync();
            return result;
        }

        public async Task OpenConnectionAsync(IDbConnection connection)
        {
            if (connection.State == ConnectionState.Closed)
            {
                var sqlConnection = (SqlConnection)connection;
                await sqlConnection.OpenAsync();
            }
        }
    }

    public class NotSupportedAsyncAdapter : IAsyncAdapter
    {
        public Task<int> ExecuteNonQueryAsync(IDbCommand command)
        {
            throw new NotSupportedException("Async is not supported or not configured for this provider. Enable async support by setting the IAsyncAdapter via Db.Configure().");
        }

        public Task<object> ExecuteScalarAsync(IDbCommand command)
        {
            throw new NotSupportedException("Async is not supported or not configured for this provider. Enable async support by setting the IAsyncAdapter via Db.Configure().");
        }

        public Task<IDataReader> ExecuteReaderAsync(IDbCommand command)
        {
            throw new NotSupportedException("Async is not supported or not configured for this provider. Enable async support by setting the IAsyncAdapter via Db.Configure().");
        }

        public Task OpenConnectionAsync(IDbConnection connection)
        {
            throw new NotSupportedException();
        }
    }

    public static class EnumerableToDatatable
    {

        public static DataTable ToDataTable<T>(this IEnumerable<T> items)
        {
            var table = new DataTable(typeof(T).Name);

            PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                Type propType = prop.PropertyType;

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

    public static class DataRecordExtensions
    {
        // stolen from Massive
        /// <summary>
        /// Convert a datarecord into a dynamic object, so that properties can be simply accessed
        /// using standard C# syntax.
        /// </summary>
        /// <param name="rdr">the data record</param>
        /// <returns>A dynamic object with fields corresponding to the database columns</returns>
        public static dynamic ToExpando(this IDataRecord rdr)
        {
            dynamic e = new ExpandoObject();
            var d = e as IDictionary<string, object>;
            for (var i = 0; i < rdr.FieldCount; i++)
            {
                string name = rdr.GetName(i);
                object value = rdr[i];
                d.Add(name, DBNullHelper.FromDb(value));
            }
            return e;
        }

        /// <summary>
        /// Get a value from an IDataRecord by column name. This method supports all types,
        /// as long as the DbType is convertible to the CLR Type passed as a generic argument.
        /// Also handles conversion from DbNull to null, including nullable types.
        /// </summary>
        public static TResult Get<TResult>(this IDataRecord reader, string name)
        {
            return reader.Get<TResult>(reader.GetOrdinal(name));
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
    // not needed if you use ToExpando()
    public static class ConvertTo<T>
    {
        // ReSharper disable StaticFieldInGenericType
        // clearly we *want* a static field for each instantiation of this generic class...
        /// <summary>
        /// The actual conversion method. Converts an object to any type using standard casting functionality, taking into account null/nullable types
        /// and avoiding DBNull issues. This method is set as a delegate at runtime (in the static constructor).
        /// </summary>
        public static readonly Func<object, T> From;
        // ReSharper restore StaticFieldInGenericType

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
                var genericMethodForElement = methodInfo.MakeGenericMethod(new[] { type.GetGenericArguments()[0] });
                return (Func<object, T>)Delegate.CreateDelegate(delegateType, genericMethodForElement);
            }
            return ConvertValueType;
        }

        // ReSharper disable UnusedMember.Local
        // (used via reflection!)
        private static TElem? ConvertNullableValueType<TElem>(object value) where TElem : struct
        {
            return DBNullHelper.IsNull(value) ? (TElem?)null : ConvertPrivate<TElem>(value);
        }
        // ReSharper restore UnusedMember.Local

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