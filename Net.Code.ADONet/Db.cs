using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Dynamic;
using System.Text.RegularExpressions;
#if DEBUG
using System.Diagnostics;
#endif

// Yet Another Data Access Layer
// usage: 
//   using (var db = new Db()) {}; 
//   using (var db = new Db(connectionString)) {}; 
//   using (var db = new Db(connectionString, providerName)) {}; 
//   using (var db = Db.FromConfig());
//   using (var db = Db.FromConfig(connectionStringName));
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
                Log($"{p.ParameterName} = {p.Value}");
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
        IDbConfigurationBuilder OnPrepareCommand(Action<IDbCommand> action);
        /// <summary>
        /// Set the mapping convention used to map property names and db column names
        /// </summary>
        /// <param name="convention"></param>
        IDbConfigurationBuilder WithMappingConvention(MappingConvention convention);
    }

    public static class StringExtensions
    {
        public static string ToUpperRemoveSpecialChars(this string str) 
            => string.IsNullOrEmpty(str) ? str : Regex.Replace(str, @"([^\w]|_)", "").ToUpperInvariant();
    }

    public class MappingConvention
    {
        private readonly Func<IDataRecord, int, string> _getColumnName;
        private readonly Func<PropertyInfo, string> _getPropertyName;

        public MappingConvention(
            Func<IDataRecord, int, string> getColumnName, 
            Func<PropertyInfo, string> getPropertyName)
        {
            _getColumnName = getColumnName;
            _getPropertyName = getPropertyName;
        }
        /// <summary>
        /// Maps column names to property names based on exact, case sensitive match
        /// </summary>
        public static readonly MappingConvention Strict = new MappingConvention((record, i) => record.GetName(i), p => p.Name);
        /// <summary>
        /// Maps column names to property names based on case insensitive match, ignoring underscores
        /// </summary>
        public static readonly MappingConvention Loose = new MappingConvention(
            (record, i) => record.GetName(i).ToUpperRemoveSpecialChars(), 
            p => p.Name.ToUpperRemoveSpecialChars()
            );

        public string GetName(IDataRecord record, int i) => _getColumnName(record, i);

        public string GetName(PropertyInfo property) => _getPropertyName(property);
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

        public IDbConfigurationBuilder WithMappingConvention(MappingConvention convention)
        {
            _dbConfig.MappingConvention = convention;
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
        private static readonly Option<PropertyInfo> BindByName = new Option<PropertyInfo>();
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
                if (!BindByName.HasValue)
                    BindByName.SetValue(command.GetType().GetProperty("BindByName"));
                BindByName.Value?.SetValue(command, true, null);
            });

            // Oracle convention is to work with UPPERCASE_AND_UNDERSCORE instead of BookTitleCase
            WithMappingConvention(MappingConvention.Loose);

            return this;
        }

        public DbConfigurationBuilder FromProviderName(string providerName)
        {
            switch (providerName)
            {
                case "Oracle.DataAccess.Client":
                case "Oracle.ManagedDataAccess.Client":
                    return Oracle();
            }
            return this;
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
        private static readonly Action<IDbCommand> Empty = c => { };

        private static readonly MappingConvention Default = MappingConvention.Strict;

        public DbConfig()
            : this(Empty, Default)
        {
        }

        public DbConfig(Action<IDbCommand> prepareCommand, MappingConvention convention)
        {
            PrepareCommand = prepareCommand;
            MappingConvention = convention;
        }

        public Action<IDbCommand> PrepareCommand { get; internal set; }
        public MappingConvention MappingConvention { get; internal set; }
    }

    /// <summary>
    /// A class that wraps a database.
    /// </summary>
    public class Db : IDb
    {
        private readonly DbConfig _config = new DbConfig();

        public string ProviderName => _providerName;

        public IDbConfigurationBuilder Configure() => ConfigurePriv();

        private DbConfigurationBuilder ConfigurePriv() => new DbConfigurationBuilder(_config);

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
            ConfigurePriv().FromProviderName(providerName);
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
            ConfigurePriv().FromProviderName(_providerName);
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
                if (dbConnection.State == ConnectionState.Closed)
                    dbConnection.Open();
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

    static class DataReaderExtensions
    {
        public static IEnumerable<IDataRecord> AsEnumerable(this IDataReader reader)
        {
            using (reader) { while (reader.Read()) yield return reader; }
        }

        public static IEnumerable<dynamic> ToExpandoList(this IEnumerable<IDataRecord> input) 
            => input.Select(item => item.ToExpando());

        public static IEnumerable<dynamic> ToDynamicDataRecord(this IEnumerable<IDataRecord> input) 
            => input.Select(item => Dynamic.DataRecord(item));

        public static IEnumerable<List<dynamic>> ToMultiResultSet(this IDataReader reader)
        {
            do
            {
                var list = new List<dynamic>();
                while (reader.Read()) list.Add(reader.ToExpando());
                yield return list;
            } while (reader.NextResult());
        }

        public static List<T> GetResultSet<T>(this IDataReader reader, MappingConvention convention, string provider, out bool moreResults) where T : new()
        {
            var list = new List<T>();
            while (reader.Read()) list.Add(reader.MapTo<T>(convention, provider));
            moreResults = reader.NextResult();
            return list;
        }
    }

    public class CommandBuilder
    {
        private readonly IDbCommand _command;
        private readonly MappingConvention _convention;
        private readonly string _provider;

        public CommandBuilder(IDbCommand command, MappingConvention convention = null, string provider = null)
        {
            _command = command;
            _convention = convention ?? MappingConvention.Strict;
            _provider = provider ?? Db.DefaultProviderName;
        }
        
        /// <summary>
        /// The raw IDbCommand instance
        /// </summary>
        public IDbCommand Command => _command;

        /// <summary>
        /// Executes the query and returns the result as a list of dynamic objects. 
        /// </summary>
        public IEnumerable<dynamic> AsEnumerable() => Execute.Reader().AsEnumerable().ToExpandoList();

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
            => AsReader().AsEnumerable().Select(r => r.MapTo<T>(_convention, _provider));

        // enables linq 'select' syntax
        public IEnumerable<T> Select<T>(Func<dynamic, T> selector) 
            => Execute.Reader().AsEnumerable().ToDynamicDataRecord().Select(selector);

        /// <summary>
        /// Executes the query and returns the result as a list of lists
        /// </summary>
        public IEnumerable<List<dynamic>> AsMultiResultSet()
        {
            using (var reader = Execute.Reader())
            {
                foreach (var item in reader.ToMultiResultSet()) yield return item;
            }
        }
        /// <summary>
        /// Executes the query and returns the result as a tuple of lists
        /// </summary>
        public Tuple<List<T1>, List<T2>> AsMultiResultSet<T1, T2>() where T1 : new() where T2 : new()
        {
            using (var reader = Execute.Reader())
            {
                bool more;
                var result1 = reader.GetResultSet<T1>(_convention, _provider, out more);
                var result2 = reader.GetResultSet<T2>(_convention, _provider, out more);
                return Tuple.Create(
                    result1,
                    result2
                    );
            }
        }
        /// <summary>
        /// Executes the query and returns the result as a tuple of lists
        /// </summary>
        public Tuple<List<T1>, List<T2>, List<T3>> AsMultiResultSet<T1, T2, T3>() where T1 : new() where T2 : new() where T3 : new()
        {
            using (var reader = Execute.Reader())
            {
                bool more;
                var result1 = reader.GetResultSet<T1>(_convention, _provider, out more);
                var result2 = reader.GetResultSet<T2>(_convention, _provider, out more);
                var result3 = reader.GetResultSet<T3>(_convention, _provider, out more);
                return Tuple.Create(
                    result1, result2, result3
                    );
            }
        }
        /// <summary>
        /// Executes the command, returning the first column of the first result, converted to the type T
        /// </summary>
        /// <typeparam name="T">return type</typeparam>
        public T AsScalar<T>() => ConvertTo<T>.From(AsScalar());

        public object AsScalar() => Execute.Scalar();

        /// <summary>
        /// Executes the command as a SQL statement, returning the number of rows affected
        /// </summary>
        public int AsNonQuery() => Execute.NonQuery();

        private Executor Execute => new Executor(_command);

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
            return reader.AsEnumerable().ToExpandoList();
        }

        /// <summary>
        /// Executes the query and returns the result as a list of [T] asynchronously
        /// This method is only supported if the underlying provider propertly implements async behaviour.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="selector">mapping function that transforms a datarecord (wrapped as a dynamic object) to an instance of type [T]</param>
        public async Task<IEnumerable<T>> AsEnumerableAsync<T>(Func<dynamic, T> selector)
        {
            var reader = await ExecuteAsync.Reader();
            return reader.AsEnumerable().ToDynamicDataRecord().Select(selector);
        }

        /// <summary>
        /// Executes the query and returns the result as a list of lists asynchronously
        /// This method is only supported if the underlying provider propertly implements async behaviour.
        /// </summary>
        public async Task<IEnumerable<IEnumerable<dynamic>>> AsMultiResultSetAsync()
        {
            using (var reader = await ExecuteAsync.Reader())
            {
                return reader.ToMultiResultSet().ToList();
            }
        }
        private AsyncExecutor ExecuteAsync => new AsyncExecutor((DbCommand)_command);

        /// <summary>
        /// Sets the command text
        /// </summary>
        /// <param name="text"></param>
        public CommandBuilder WithCommandText(string text)
        {
            _command.CommandText = text;
            return this;
        }

        /// <summary>
        /// Sets the command type
        /// </summary>
        /// <param name="type"></param>
        public CommandBuilder OfType(CommandType type)
        {
            _command.CommandType = type;
            return this;
        }

        /// <summary>
        /// Adds a parameter for each property of the given object, with the property name as the name 
        /// of the parameter and the property value as the corresponding parameter value
        /// </summary>
        /// <param name="parameters"></param>
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

        public CommandBuilder WithParameter<T>(T p) where T : DbParameter
        {
            Command.Parameters.Add(p);
            return this;
        }


        /// <summary>
        /// Executes the command as a datareader. Use this if you need best performance.
        /// </summary>
        public IDataReader AsReader() => Execute.Reader();

        public DataTable AsDataTable() => Execute.DataTable();

        public CommandBuilder InTransaction(IDbTransaction tx)
        {
            Command.Transaction = tx;
            return this;
        }

        public T Single<T>() => AsEnumerable<T>().Single();
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
    public static class EnumerableToDatatable
    {
        public static DataTable ToDataTable<T>(this IEnumerable<T> items)
        {
            var table = new DataTable(typeof(T).Name);
        
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                var propType = prop.PropertyType;
                
                if (propType.IsNullableType())
                    propType = new NullableConverter(propType).UnderlyingType;
                
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

    static class Dynamic
    {
        public static dynamic DataRow(DataRow row) => From(row, (r, s) => r[s]);
        public static dynamic DataRecord(IDataRecord record) => From(record, (r, s) => r[s]);
        public static dynamic Dictionary<TValue>(IReadOnlyDictionary<string, TValue> dictionary) => From(dictionary, (d, s) => d[s]);

        static dynamic From<T>(T item, Func<T, string, object> getter) => new DynamicIndexer<T>(item, getter);

        class DynamicIndexer<T> : DynamicObject
        {
            private readonly T _item;
            private readonly Func<T, string, object> _getter;

            public DynamicIndexer(T item, Func<T, string, object> getter)
            {
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

    public static class DataTableExtensions
    {
        static dynamic ToDynamic(this DataRow dr) => Dynamic.DataRow(dr);
        public static IEnumerable<dynamic> AsEnumerable(this DataTable dataTable) => dataTable.Rows.OfType<DataRow>().Select(ToDynamic);
        public static IEnumerable<T> Select<T>(this DataTable dt, Func<dynamic, T> selector) => dt.AsEnumerable().Select(selector);
        public static IEnumerable<dynamic> Where(this DataTable dt, Func<dynamic, bool> predicate) => dt.AsEnumerable().Where(predicate);
    }

    public static class DataRecordExtensions
    {
        public static T MapTo<T>(this IDataRecord record, MappingConvention convention, string provider) 
        {
            var setters = GetSettersForType<T>(p => convention.GetName(p), provider);
            var result = Activator.CreateInstance<T>();
            for (var i = 0; i < record.FieldCount; i++)
            {
                Action<T,object> setter;
                var columnName = convention.GetName(record, i);
                if (!setters.TryGetValue(columnName, out setter))
                    continue;
                var val = DBNullHelper.FromDb(record.GetValue(i));
                setter(result, val);
            }
            return result;
        }

        private static readonly ConcurrentDictionary<dynamic, object> Setters = new ConcurrentDictionary<dynamic, object>();
        private static IDictionary<string, Action<T, object>> GetSettersForType<T>(Func<PropertyInfo, string> getName, string provider) 
        {
            var setters = Setters.GetOrAdd(
                new {Type =  typeof (T), Provider = provider},
                d =>((Type)d.Type).GetProperties().ToDictionary(getName, p => p.GetSetDelegate<T>())
                );
            return (IDictionary<string, Action<T,object>>)setters;
        }

        static Action<T,object> GetSetDelegate<T>(this PropertyInfo p)
        {
            var method = p.GetSetMethod();
            var genericHelper = typeof(DataReaderExtensions).GetMethod("CreateSetterDelegateHelper", BindingFlags.Static | BindingFlags.NonPublic);
            var constructedHelper = genericHelper.MakeGenericMethod(typeof (T), method.GetParameters()[0].ParameterType);
            return (Action<T, object>)constructedHelper.Invoke(null, new object[] { method });
        }
        // ReSharper disable once UnusedMethodReturnValue.Local
        // ReSharper disable once UnusedMember.Local
        static object CreateSetterDelegateHelper<TTarget, TParam>(MethodInfo method) where TTarget : class
        {
            var action = (Action<TTarget, TParam>)Delegate.CreateDelegate(typeof(Action<TTarget, TParam>), method);
            Action<TTarget, object> ret = (target, param) => action(target, ConvertTo<TParam>.From(param));
            return ret;
        }

        /// <summary>
        /// Convert a datarecord into a dynamic object, so that properties can be simply accessed
        /// using standard C# syntax.
        /// </summary>
        /// <param name="rdr">the data record</param>
        /// <returns>A dynamic object with fields corresponding to the database columns</returns>
        public static dynamic ToExpando(this IDataRecord rdr)
        {
            var d = new Dictionary<string, object>();
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
        public static TResult Get<TResult>(this IDataRecord reader, string name) => reader.Get<TResult>(reader.GetOrdinal(name));

        /// <summary>
        /// Get a value from an IDataRecord by index. This method supports all types,
        /// as long as the DbType is convertible to the CLR Type passed as a generic argument.
        /// Also handles conversion from DbNull to null, including nullable types.
        /// </summary>
        public static TResult Get<TResult>(this IDataRecord reader, int c) => ConvertTo<TResult>.From(reader[c]);
    }

    /// <summary>
    /// Class for runtime type conversion, including DBNull.Value to/from null. Supports reference types,
    /// value types and nullable value types
    /// </summary>
    /// <typeparam name="T"></typeparam>
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
            if (type.IsNullableType())
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
            => DBNullHelper.IsNull(value) ? (TElem?)null : ConvertPrivate<TElem>(value);

        private static T ConvertRefType(object value) => DBNullHelper.IsNull(value) ? default(T) : ConvertPrivate<T>(value);

        private static T ConvertValueType(object value)
        {
            if (DBNullHelper.IsNull(value))
            {
                throw new NullReferenceException("Value is DbNull");
            }
            return ConvertPrivate<T>(value);
        }

        private static TElem ConvertPrivate<TElem>(object value) => (TElem)(Convert.ChangeType(value, typeof(TElem)));
    }

    public static class DBNullHelper
    {
        public static bool IsNullableType(this Type type) 
            => (type.IsGenericType && !type.IsGenericTypeDefinition) &&
               (typeof(Nullable<>) == type.GetGenericTypeDefinition());
        public static bool IsNull(object o) => o == null || DBNull.Value.Equals(o);
        public static object FromDb(object o) => IsNull(o) ? null : o;
        public static object ToDb(object o) => IsNull(o) ? DBNull.Value : o;
    }
}
