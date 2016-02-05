using System;
using System.Data.SqlServerCe;
using System.Data.SQLite;
using System.IO;

namespace Net.Code.ADONet.Tests.Integration
{
    public class On
    {
        private static string _dbname = "TESTDB";

        public static On SqLite()
        {
            var sqliteAssembly = typeof(SQLiteConnection).Assembly;
            Console.WriteLine(sqliteAssembly.ToString());
            var initSqlStatement = ""; // a database is always created from scratch (since mstest always executes in a new folder)

            var providerName = "System.Data.SqLite";
            var server = $@"{_dbname}.db";
            var database = _dbname;
            var masterConnectionString = $@"Data Source={server};";
            string connectionString = $@"Data Source={server};";

            if (File.Exists(server)) File.Delete(server);

            return new On("SqLite", server, database, providerName, masterConnectionString, connectionString, initSqlStatement, MappingConvention.Strict, DefaultCreatePersonTable, DefaultInsertPerson);

        }

        public static On SqlServer()
        {
            var initSqlStatement = "if exists (SELECT * FROM sys.databases WHERE Name = \'{0}\') \r\n" +
                                   "begin\r\n" +
                                   "\texec msdb.dbo.sp_delete_database_backuphistory \'{0}\'\r\n" +
                                   "\talter database {0} SET  SINGLE_USER WITH ROLLBACK IMMEDIATE\r\n" +
                                   "\tdrop database {0}\r\n" +
                                   "end\r\n" +
                                   "create database {0}\r\n";

            var server = @"localhost";
            var database = _dbname;
            var masterConnectionString = $@"Data Source={server};Initial Catalog=master;Integrated Security=True";
            string connectionString = $@"Data Source={server};Initial Catalog={database};Integrated Security=True";
            return new On("SqlServer", server, database, "System.Data.SqlClient", masterConnectionString, connectionString, initSqlStatement, MappingConvention.Strict, DefaultCreatePersonTable, DefaultInsertPerson);
        }

        public static On Oracle()
        {
            var initSqlStatement = "DECLARE\r\n" +
                                   "\r\n" +
                                   "    c INTEGER := 0;\r\n" +
                                   "\r\n" +
                                   "BEGIN\r\n" +
                                   "    SELECT count(*) INTO c FROM sys.dba_users WHERE USERNAME = \'{0}\';\r\n" +
                                   "    IF c = 1 THEN\r\n" +
                                   "            execute immediate (\'drop user {0} cascade\');\r\n" +
                                   "            execute immediate (\'drop tablespace {0}_TS\');\r\n" +
                                   "            execute immediate (\'drop tablespace {0}_TS_TMP\');\r\n" +
                                   "    END IF;\r\n" +
                                   "    \r\n" +
                                   "    execute immediate (\'create tablespace {0}_TS datafile \'\'{0}.dat\'\' size 10M reuse autoextend on\');\r\n" +
                                   "    execute immediate (\'create temporary tablespace {0}_TS_TMP tempfile \'\'{0}_TMP.dat\'\' size 10M reuse autoextend on\');\r\n" +
                                   "    execute immediate (\'create user {0} identified by pass default tablespace {0}_TS temporary tablespace {0}_TS_TMP\');\r\n" +
                                   "    execute immediate (\'grant create session to {0}\');\r\n" +
                                   "    execute immediate (\'grant create table to {0}\');\r\n" +
                                   "    execute immediate (\'GRANT UNLIMITED TABLESPACE TO {0}\');\r\n" +
                                   "\r\n" +
                                   "END;";

            var server = @"localhost:1521/XE";
            var database = _dbname;
            var masterConnectionString = $@"Data Source={server};DBA Privilege=SYSDBA;User Id=sys;Password=sys";
            var connectionString = $@"Data Source={server};User Id={database};Password=pass";
            var mappingConvention = MappingConvention.Loose;
            var createPersonTable = "CREATE TABLE PERSON (" +
                               "    ID INT NOT NULL" +
                               ",   OPTIONAL_NUMBER INT NOT NULL" +
                               ",   REQUIRED_NUMBER INT NOT NULL" +
                               ",   NAME VARCHAR2(100) NOT NULL" +
                               ",   EMAIL VARCHAR2(100) NOT NULL" +
                               ")";
            var insertPerson =
                        "INSERT INTO PERSON (ID,OPTIONAL_NUMBER,REQUIRED_NUMBER,NAME,EMAIL) VALUES (:Id,:OptionalNumber,:RequiredNumber,:Name,:Email)";
            return new On("Oracle", server, database, "Oracle.ManagedDataAccess.Client", masterConnectionString, connectionString, initSqlStatement, mappingConvention, createPersonTable, insertPerson);
        }

        public static On SqlServerCe()
        {
            
            var assembly = typeof(SqlCeConnection).Assembly;
            Console.WriteLine(assembly.ToString());
            var initSqlStatement = "SELECT 1 AS Result"; // a database is always created from scratch (since mstest always executes in a new folder)

            var providerName = "System.Data.SqlServerCe.4.0";
            var server = Path.GetFullPath($"{_dbname}.sdf");
            var database = _dbname;
            var masterConnectionString = $@"Data Source={server};";
            string connectionString = $@"Data Source={server};";
            
            if (File.Exists(server)) File.Delete(server);
            var en = new SqlCeEngine(masterConnectionString);
            en.CreateDatabase();

            return new On("SqlServerCe", server, database, providerName, masterConnectionString, connectionString, initSqlStatement, MappingConvention.Strict, DefaultCreatePersonTable, DefaultInsertPerson, false);
        }


        private On(string name, string server, string database, string providerName, string masterConnectionString, string connectionString, string initSql, MappingConvention mappingConvention, string createPersonTable, string insertPerson, bool supportsMultiResultSet = true)
        {
            Name = name;
            Server = server;
            ProviderName = providerName;
            MasterConnectionString = masterConnectionString;
            ConnectionString = connectionString;
            MappingConvention = mappingConvention;
            DropRecreate = string.Format(initSql, database);
            CreatePersonTable = createPersonTable;
            InsertPerson = insertPerson;
            SupportsMultiResultSet = supportsMultiResultSet;
        }

        public string InsertPerson { get; }

        private static readonly string DefaultCreatePersonTable = $"CREATE TABLE {nameof(Person)} (" +
                                                         $"   {nameof(Person.Id)}            int not null, " +
                                                         $"   {nameof(Person.OptionalNumber)}       int, " +
                                                         $"   {nameof(Person.RequiredNumber)}    int not null, " +
                                                         $"   {nameof(Person.Name)}    nvarchar(100) not null, " +
                                                         $"   {nameof(Person.Email)} nvarchar(100), " +
                                                         $"   {nameof(Person.UniqueId)}      uniqueidentifier" +
                                                         ");";

        private static readonly string DefaultInsertPerson = $"INSERT INTO {nameof(Person)} (" +
                                                             $"   {nameof(Person.Id)}, " +
                                                             $"   {nameof(Person.OptionalNumber)}, " +
                                                             $"   {nameof(Person.RequiredNumber)}, " +
                                                             $"   {nameof(Person.Name)}, " +
                                                             $"   {nameof(Person.Email)}, " +
                                                             $"   {nameof(Person.UniqueId)}" +
                                                             ") VALUES (" +
                                                             $"   @{nameof(Person.Id)}, " +
                                                             $"   @{nameof(Person.OptionalNumber)}, " +
                                                             $"   @{nameof(Person.RequiredNumber)}, " +
                                                             $"   @{nameof(Person.Name)}, " +
                                                             $"   @{nameof(Person.Email)}, " +
                                                             $"   @{nameof(Person.UniqueId)}" +
                                                             ")";

        public string Server { get; }


        public string ProviderName { get; }

        public string MasterConnectionString { get; }

        public string ConnectionString { get; }
        public MappingConvention MappingConvention { get; }

        public string DropRecreate { get; }

        public string Name { get; }
        public string CreatePersonTable { get; }
        public bool SupportsMultiResultSet { get; set; }
    }
}