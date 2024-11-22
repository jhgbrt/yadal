using System.Collections.Generic;
using System.Data;

using Net.Code.ADONet.Tests.Integration.Data;

using Oracle.ManagedDataAccess.Client;

namespace Net.Code.ADONet.Tests.Integration.Databases
{
    public class OracleDb : BaseDb<OracleDb>
    {
        public OracleDb() : base(OracleClientFactory.Instance)
        {
        }

        public override string CreatePersonTable
            => $"""
               CREATE TABLE {GetTableName<Person>()} (
                   {GetColumnName<Person>(nameof(Person.Id))} NUMBER(8,0) NOT NULL
               ,   {GetColumnName<Person>(nameof(Person.OptionalNumber))} NUMBER(8,0) NULL
               ,   {GetColumnName<Person>(nameof(Person.RequiredNumber))} NUMBER(8,0) NOT NULL
               ,   {GetColumnName<Person>(nameof(Person.BirthDate))} DATE NOT NULL
               ,   {GetColumnName<Person>(nameof(Person.RegisteredAt))} TIMESTAMP NOT NULL
               ,   {GetColumnName<Person>(nameof(Person.Name))} VARCHAR2(100) NOT NULL
               ,   {GetColumnName<Person>(nameof(Person.Email))} VARCHAR2(100) NOT NULL
               )
               """;
        public override string CreateAddressTable
            => $"""
               CREATE TABLE {GetTableName<Address>()} (
                   {GetColumnName<Address>(nameof(Address.Id)       )} NUMBER(8,0) NOT NULL
               ,   {GetColumnName<Address>(nameof(Address.Street)   )} VARCHAR2(100) NOT NULL
               ,   {GetColumnName<Address>(nameof(Address.ZipCode)  )} VARCHAR2(20) NOT NULL
               ,   {GetColumnName<Address>(nameof(Address.City)     )} VARCHAR2(100) NOT NULL
               ,   {GetColumnName<Address>(nameof(Address.Country)  )} VARCHAR2(100) NOT NULL
               )
               """;

        public override string CreateProductTable
            => $"""
               CREATE TABLE {GetTableName<Product>()} (
                   {GetColumnName<Product>(nameof(Product.Id   ))} NUMBER(8,0) NOT NULL
               ,   {GetColumnName<Product>(nameof(Product.Name ))} VARCHAR2(100) NOT NULL
               ,   {GetColumnName<Product>(nameof(Product.Price))} NUMBER(16,2) NOT NULL
               )
               """;

        public override CommandBuilder CreateMultiResultSetCommand(IDb db, string query1, string query2)
        {
            var query = $"""
                        BEGIN
                          OPEN :Cur1 FOR {query1};
                          OPEN :Cur2 FOR {query2};
                        END;
                        """;
            return db.Sql(query)
                .WithParameter(new OracleParameter("Cur1", OracleDbType.RefCursor, ParameterDirection.Output))
                .WithParameter(new OracleParameter("Cur2", OracleDbType.RefCursor, ParameterDirection.Output));
        }
        public override IEnumerable<string> GetDropAndRecreateDdl()
        {
            var user = Configuration.GetConnectionStringProperty(Name, "User ID");
            var tmpts = $"{user}_TS_TMP";
            var ts = $"{user}_TS";
            var password = Configuration.GetConnectionStringProperty(Name, "Password");

            yield return "ALTER SESSION SET \"_ORACLE_SCRIPT\"=TRUE";
            yield return $"""
            DECLARE 
               c INTEGER := 0;
            BEGIN
               SELECT COUNT(*) INTO c FROM SYS.DBA_USERS WHERE USERNAME = '{user}';
               IF c > 0 THEN
                    EXECUTE IMMEDIATE 'DROP USER {user} CASCADE';
               END IF;
               SELECT COUNT(*) into c FROM SYS.DBA_TABLESPACES WHERE TABLESPACE_NAME = '{ts}';
               IF c > 0 THEN
                    EXECUTE IMMEDIATE 'DROP TABLESPACE {ts}';
               END IF;
               SELECT COUNT(*) into c FROM SYS.DBA_TABLESPACES WHERE TABLESPACE_NAME = '{tmpts}';
               IF c > 0 THEN
                    EXECUTE IMMEDIATE 'DROP TABLESPACE {tmpts}';
               END IF;
            END;
            """;
            yield return $"CREATE TABLESPACE {ts} DATAFILE '{ts}.DAT' SIZE 10M REUSE AUTOEXTEND ON";
            yield return $"CREATE TEMPORARY TABLESPACE {tmpts} TEMPFILE '{tmpts}.DAT' SIZE 10M REUSE AUTOEXTEND ON";
            yield return $"CREATE USER {user} IDENTIFIED BY {password} DEFAULT TABLESPACE {ts} TEMPORARY TABLESPACE {tmpts}";
            yield return $"GRANT CREATE SESSION TO {user}";
            yield return $"GRANT CREATE TABLE TO {user}";
            yield return $"GRANT UNLIMITED TABLESPACE TO {user}";

        }
    }
}