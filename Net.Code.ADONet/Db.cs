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

namespace Net.Code.ADONet
{
    public interface IConnectionFactory
    {
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
        void Execute(string command);
    }

    /// <summary>
    /// A class that wraps a database.
    /// </summary>
    public class Db : IDb
    {
        private class AdoNetProviderFactory : IConnectionFactory
        {
            private readonly string _providerInvariantName;

            public AdoNetProviderFactory(string providerInvariantName)
            {
                _providerInvariantName = providerInvariantName;
            }

            public IDbConnection CreateConnection(string connectionString)
            {
                var connection = DbProviderFactories.GetFactory(_providerInvariantName).CreateConnection();
                // ReSharper disable PossibleNullReferenceException
                connection.ConnectionString = connectionString;
                // ReSharper restore PossibleNullReferenceException
                return connection;
            }
        }

        /// <summary>
        /// The default DbProvider name is "System.Data.SqlClient" (for sql server).
        /// </summary>
        public static string DefaultProviderName = "System.Data.SqlClient";

        public Action<string> Log = s => Trace.WriteLine(s);

        private readonly string _connectionString;
        private Lazy<IDbConnection> _connection;
        private readonly IConnectionFactory _connectionFactory;
        private readonly IDbConnection _externalConnection;

        /// <summary>
        /// Instantiate Db with existing connection. The connection is only used for creating commands; it must be Open, and should be disposed by the caller when done.
        /// </summary>
        /// <param name="connection">The existing connection</param>
        public Db(IDbConnection connection)
        {
            _externalConnection = connection;
        }

        /// <summary>
        /// Instantiate Db with connectionString and DbProviderName
        /// </summary>
        /// <param name="connectionString">The connection string</param>
        /// <param name="providerName">The ADO .Net Provider name. When not specified, the default value is used (see DefaultProviderName)</param>
        public Db(string connectionString, string providerName = null)
            : this(connectionString, new AdoNetProviderFactory(providerName ?? DefaultProviderName))
        {
        }

        /// <summary>
        /// Instantiate Db with connectionString and a custom IConnectionFactory
        /// </summary>
        /// <param name="connectionString">the connection string</param>
        /// <param name="connectionFactory">the connection factory</param>
        public Db(string connectionString, IConnectionFactory connectionFactory)
        {
            _connectionString = connectionString;
            _connectionFactory = connectionFactory;
            _connection = new Lazy<IDbConnection>(CreateAndOpenConnection);
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

        private IDbConnection CreateAndOpenConnection()
        {
            var connection = _connectionFactory.CreateConnection(_connectionString);
            connection.Open();
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
            return new CommandBuilder(cmd) { LogFunc = Log }.OfType(commandType).WithCommandText(command);
        }

        /// <summary>
        /// Create a SQL command and execute it immediately (non query)
        /// </summary>
        /// <param name="command"></param>
        public void Execute(string command)
        {
            Sql(command).AsNonQuery();
        }
    }

    public static class DataReaderExtensions
    {
        public static IEnumerable<IDataRecord> AsEnumerable(this IDataReader reader)
        {
            using (reader) { while (reader.Read()) yield return reader; }
        }

        public static IEnumerable<dynamic> ToDynamic(this IEnumerable<IDataRecord> input)
        {
            return from item in input select item.ToExpando();
        }
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

        /// <summary>
        /// executes the query and returns the result as a list of dynamic objects
        /// </summary>
        /// <returns></returns>
        public IEnumerable<dynamic> AsEnumerable()
        {
            Log();
            OpenConnectionIfClosed();
            return Command.ExecuteReader().AsEnumerable().ToDynamic();
        }

        private void Log()
        {
            LogFunc(Command.CommandText);
            foreach (IDbDataParameter p in Command.Parameters)
            {
                LogFunc(string.Format("{0} = {1}", p.ParameterName, p.Value));
            }
        }

        public Action<string> LogFunc = s => { };

        /// <summary>
        /// executes the query and returns the result as a list of lists
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IList<dynamic>> AsMultiResultSet()
        {

            Log();
            OpenConnectionIfClosed();

            using (var reader = Command.ExecuteReader())
            {
                if (reader == null) throw new NullReferenceException("reader");
                do
                {
                    var list = new List<dynamic>();
                    while (reader.Read()) list.Add(reader.ToExpando());
                    yield return list;
                } while (reader.NextResult());
            }
        }

        /// <summary>
        /// Executes the command, returning the first column of the first result, converted to the type T
        /// </summary>
        /// <typeparam name="T">return type</typeparam>
        /// <returns></returns>
        public T AsScalar<T>()
        {
            Log();
            OpenConnectionIfClosed();
            return ConvertTo<T>.From(Command.ExecuteScalar());
        }

        /// <summary>
        /// Executes the command as a SQL statement, not returning any results
        /// </summary>
        public void AsNonQuery()
        {
            Log();
            OpenConnectionIfClosed();
            Command.ExecuteNonQuery();
        }

        private void OpenConnectionIfClosed()
        {
            if (Command.Connection.State == ConnectionState.Closed) Command.Connection.Open();
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
            p.Value = value.ToDb();
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

    public static class EnumerableToDatatable
    {

        public static DataTable ToDataTable<T>(this IEnumerable<T> items)
        {
            var table = new DataTable(typeof(T).Name);

            PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                Type propType = prop.PropertyType;

                if (propType.IsNullableType())
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
                d.Add(name, value.FromDb());
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
            if (type.IsNullableType())
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
            return value.IsNull() ? (TElem?)null : ConvertPrivate<TElem>(value);
        }

        // ReSharper restore UnusedMember.Local

        private static T ConvertRefType(object value)
        {
            return value.IsNull() ? default(T) : ConvertPrivate<T>(value);
        }

        private static T ConvertValueType(object value)
        {
            if (DBNull.Value.Equals(value))
            {
                throw new NullReferenceException("Value is DbNull"); // TODO or should we throw InvalidCastException here? Or a custom exception?
            }
            return ConvertPrivate<T>(value);
        }

        private static TElem ConvertPrivate<TElem>(object value)
        {
            // TODO decide on whether we should simply cast (current behaviour) or use the Convert.ChangeType function
            //      maybe this should be configurable?
            return (TElem)(value);
            //return (T)(Convert.ChangeType(value, typeof(T)));
        }

    }

    public static class ExtensionsForGettingRidOfDBNull
    {
        public static bool IsNullableType(this Type type)
        {
            return
                (type.IsGenericType && !type.IsGenericTypeDefinition) &&
                (typeof(Nullable<>) == type.GetGenericTypeDefinition());
        }

        public static bool IsNull(this object o)
        {
            return o == null || DBNull.Value.Equals(o);
        }

        public static object FromDb(this object o)
        {
            return o.IsNull() ? null : o;
        }

        public static object ToDb(this object o)
        {
            return o.IsNull() ? DBNull.Value : o;
        }

    }
}