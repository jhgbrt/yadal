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
            => """
               CREATE TABLE PERSON (
                   ID NUMBER(8,0) NOT NULL
               ,   OPTIONAL_NUMBER NUMBER(8,0) NULL
               ,   REQUIRED_NUMBER NUMBER(8,0) NOT NULL
               ,   NAME VARCHAR2(100) NOT NULL
               ,   EMAIL VARCHAR2(100) NOT NULL
               )
               """;
        public override string CreateAddressTable
            => """
               CREATE TABLE ADDRESS (
                   ID NUMBER(8,0) NOT NULL
               ,   STREET VARCHAR2(100) NOT NULL
               ,   ZIP_CODE VARCHAR2(20) NOT NULL
               ,   CITY VARCHAR2(100) NOT NULL
               ,   COUNTRY VARCHAR2(100) NOT NULL
               )
               """;

        public override string CreateProductTable
            => """
               CREATE TABLE PRODUCT ("
                   ID NUMBER(8,0) NOT NULL
               ,   NAME VARCHAR2(100) NOT NULL
               ,   PRICE NUMBER(8,2) NOT NULL
               )
               """;

        public override (IReadOnlyCollection<Person>, IReadOnlyCollection<Address>) SelectPersonAndAddress(IDb db)
        {
            var query = $"""
                        BEGIN
                          OPEN :Cur1 FOR {SelectPeople};
                          OPEN :Cur2 FOR {SelectAddresses};
                        END;
                        """;
            return db.Sql(query)
                .WithParameter(new OracleParameter("Cur1", OracleDbType.RefCursor, ParameterDirection.Output))
                .WithParameter(new OracleParameter("Cur2", OracleDbType.RefCursor, ParameterDirection.Output))
                .AsMultiResultSet<Person, Address>();
        }

        public override void DropAndRecreate()
        {
            var user = GetConnectionStringProperty("User ID");
            var tmpts = $"{user}_TS_TMP";
            var ts = $"{user}_TS";
            var password = GetConnectionStringProperty("Password");

            using var db = MasterDb();
            db.Execute(@"ALTER SESSION SET ""_ORACLE_SCRIPT""=TRUE");
            if (db.Sql($"SELECT COUNT(*) FROM SYS.DBA_USERS WHERE USERNAME = '{user}'").AsScalar<bool>())
                db.Execute($"DROP USER {user}");
            if (db.Sql($"SELECT COUNT(*) FROM SYS.DBA_TABLESPACES WHERE TABLESPACE_NAME = '{ts}'").AsScalar<bool>())
                db.Execute($"DROP TABLESPACE {ts}");
            if (db.Sql($"SELECT COUNT(*) FROM SYS.DBA_TABLESPACES WHERE TABLESPACE_NAME = '{tmpts}'").AsScalar<bool>())
                db.Execute($"DROP TABLESPACE {tmpts}");
            db.Execute($"CREATE TABLESPACE {ts} DATAFILE '{ts}.DAT' SIZE 10M REUSE AUTOEXTEND ON");
            db.Execute($"CREATE TEMPORARY TABLESPACE {tmpts} TEMPFILE '{tmpts}.DAT' SIZE 10M REUSE AUTOEXTEND ON");
            db.Execute($"CREATE USER {user} IDENTIFIED BY {password} DEFAULT TABLESPACE {ts} TEMPORARY TABLESPACE {tmpts}");
            db.Execute($"GRANT CREATE SESSION TO {user}");
            db.Execute($"GRANT CREATE TABLE TO {user}");
            db.Execute($"GRANT UNLIMITED TABLESPACE TO {user}");
        }

        public override Person Project(dynamic d)
        {
            return new Person
            {
                Id = d.ID,
                Email = d.EMAIL,
                Name = d.NAME,
                OptionalNumber = d.OPTIONAL_NUMBER,
                RequiredNumber = d.REQUIRED_NUMBER
            };
        }
    }
}