using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Oracle.ManagedDataAccess.Client;

namespace Net.Code.ADONet.Tests.Integration
{
    public class DbTest
    {
        private readonly On _target;

        public DbTest(On target)
        {
            _target = target;
        }

        public void DropRecreate()
        {
            using (var db = MasterDb())
            {
                db.Sql(_target.DropRecreate).AsNonQuery();
            }

        }

        public void CreateTable()
        {
            using (var db = CreateDb())
            {

                var sqlQuery = _target.CreatePersonTable;

                db.Sql(sqlQuery).AsNonQuery();
            }
        }

        public void DropTable()
        {
            using (var db = CreateDb())
            {
                db.Sql($"DROP TABLE {nameof(Person)}");
            }
        }

        public bool TableExists()
        {
            using (var db = CreateDb())
            {
                return
                    db.Sql($"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {nameof(Person)}")
                        .AsScalar<bool>();
            }
        }

        public void Insert(Person[] items)
        {
            using (var db = CreateDb())
            {
                foreach (var item in items)
                {
                    db.Sql(_target.InsertPerson).WithParameters(item).AsNonQuery();
                }
            }
        }

        public List<Person> AsEnumerableOf()
        {
            using (var db = CreateDb())
            {
                return db
                    .Sql($"SELECT * FROM {nameof(Person)}")
                    .AsEnumerable<Person>()
                    .ToList();
            }
        }
        public List<List<Person>> AsMultiResultSet()
        {
            using (var db = CreateDb())
            {
                Tuple<List<Person>, List<Person>> result;
                if (_target.ProviderName.Contains("Oracle"))
                {
                    var sqlQuery = "BEGIN\r\n" +
                                   " OPEN :Cur1 FOR SELECT * FROM PERSON;" +
                                   " OPEN :Cur2 FOR SELECT * FROM PERSON;" +
                                   "END;";
                    result = db
                        .Sql(sqlQuery)
                        .WithParameter(new OracleParameter("Cur1", OracleDbType.RefCursor, ParameterDirection.Output))
                        .WithParameter(new OracleParameter("Cur2", OracleDbType.RefCursor, ParameterDirection.Output))
                        .AsMultiResultSet<Person, Person>();
                }
                else
                {
                    var sqlQuery = $"SELECT * FROM {nameof(Person)};" +
                                   $"SELECT * FROM {nameof(Person)}";
                    result = db
                        .Sql(sqlQuery)
                        .AsMultiResultSet<Person, Person>();
                }
                return new List<List<Person>>
                {
                    result.Item1,
                    result.Item2
                };
            }
        }

        private IDb CreateDb()
        {
            var db = new Db(_target.ConnectionString, _target.ProviderName);
            db.Configure().WithMappingConvention(_target.MappingConvention);
            return db;
        }

        private IDb MasterDb()
        {
            return new Db(_target.MasterConnectionString, _target.ProviderName);
        }

    }
}