using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Net.Code.ADONet.SqlClient;

namespace Net.Code.ADONet.Tests.Integration
{
    public class DbTest
    {
        private readonly IDatabaseImpl _target;

        public DbTest(IDatabaseImpl target)
        {
            _target = target;
        }

        private void DropRecreate()
        {
            _target.DropAndRecreate();
        }

        public void CreateTable()
        {
            using (var db = CreateDb())
            {
                db.Sql(_target.CreatePersonTable).AsNonQuery();
                db.Sql(_target.CreateAddressTable).AsNonQuery();
            }
        }

        public void DropTable()
        {
            using (var db = CreateDb())
            {
                db.Execute($"DROP TABLE {nameof(Person)}");
                db.Execute($"DROP TABLE {nameof(Address)}");
            }
        }

        public void Insert(IEnumerable<Person> items, IEnumerable<Address> addresses)
        {
            using (var db = CreateDb())
            {
                foreach (var item in items)
                {
                    db.Sql(_target.InsertPerson).WithParameters(item).AsNonQuery();
                }
                foreach (var item in addresses)
                {
                    db.Sql(_target.InsertAddress).WithParameters(item).AsNonQuery();
                }
            }
        }
        public async Task InsertAsync(IEnumerable<Person> items)
        {
            using (var db = CreateDb())
            {
                foreach (var item in items)
                {
                    await db.Sql(_target.InsertPerson).WithParameters(item).AsNonQueryAsync();
                }
            }
        }

        public List<Person> GetAllPeopleGeneric()
        {
            using (var db = CreateDb())
            {
                return db
                    .Sql($"SELECT * FROM {nameof(Person)}")
                    .AsEnumerable<Person>()
                    .ToList();
            }
        }

        public DataTable GetSchemaTable()
        {
            using (var db = CreateDb())
            {
                var dataReader = db
                    .Sql($"SELECT * FROM {nameof(Person)}")
                    .AsReader();
                using (dataReader)
                {
                    var dt = dataReader.GetSchemaTable();
                    return dt;
                }
            }
        }

        public List<Person> GetAllPeopleAsDynamic()
        {
            using (var db = CreateDb())
            {
                return db
                    .Sql($"SELECT * FROM {nameof(Person)}")
                    .AsEnumerable()
                    .Select(d => (Person)_target.Project(d))
                    .ToList();
            }
        }
        public async Task<List<Person>> GetAllPeopleAsDynamicAsync()
        {
            using (var db = CreateDb())
            {
                var people = await db
                    .Sql($"SELECT * FROM {nameof(Person)}")
                    .AsEnumerableAsync();
                return people
                    .Select(d => (Person)_target.Project(d))
                    .ToList();
            }
        }

        public DataTable AsDataTable()
        {
            using (var db = CreateDb())
            {
                return db
                    .Sql($"SELECT * FROM {nameof(Person)}")
                    .AsDataTable();
            }
        }

        public MultiResultSet<Person, Address> AsMultiResultSet()
        {
            using (var db = CreateDb())
            {
                MultiResultSet<Person, Address> result;
                if (_target.ProviderName.Contains("Oracle"))
                {
                    var sqlQuery = "BEGIN\r\n" +
                                   " OPEN :Cur1 FOR SELECT * FROM PERSON;" +
                                   " OPEN :Cur2 FOR SELECT * FROM ADDRESS;" +
                                   "END;";
                    result = db
                        .Sql(sqlQuery)
                        .WithParameter(new OracleParameter("Cur1", OracleDbType.RefCursor, ParameterDirection.Output))
                        .WithParameter(new OracleParameter("Cur2", OracleDbType.RefCursor, ParameterDirection.Output))
                        .AsMultiResultSet<Person, Address>();
                }
                else
                {
                    var sqlQuery = $"SELECT * FROM {nameof(Person)};" +
                                   $"SELECT * FROM {nameof(Address)}";
                    result = db
                        .Sql(sqlQuery)
                        .AsMultiResultSet<Person, Address>();
                }
                return result;
            }
        }

        private IDb CreateDb()
        {
            return _target.CreateDb();
        }

        public Person Get(int id)
        {
            using (var db = CreateDb())
            {
                return db
                    .Sql($"SELECT * FROM {nameof(Person)} WHERE Id = {_target.EscapeChar}Id")
                    .WithParameters(new {Id = id})
                    .Single<Person>();
            }
        }

        public Person[] GetPeopleById(IEnumerable<int> ids)
        {
            using (var db = CreateDb())
            {
                return db
                    .Sql("SELECT * FROM Person JOIN @IDs IdSet ON Person.Id = IdSet.Id")
                    .WithParameter("@IDs", ids.Select(id => new {Id = id}), "IdSet")
                    .AsEnumerable<Person>()
                    .ToArray();
            }
        }

        public List<int> GetSomeIds(int n)
        {
            using (var db = CreateDb())
            {
                return db.Sql($"SELECT TOP {n} Id FROM {nameof(Person)}").Select(d => (int) d.Id).ToList();
            }
        }

        public int GetCountOfPeople()
        {
            using (var db = CreateDb())
            {
                return db.Sql($"SELECT count(*) Id FROM {nameof(Person)}").AsScalar<int>();
            }
        }
        public async Task<int> GetCountOfPeopleAsync()
        {
            using (var db = CreateDb())
            {
                var result = await db.Sql($"SELECT count(*) Id FROM {nameof(Person)}").AsScalarAsync<int>();
                return result;
            }
        }

        public void BulkInsert(Person[] list)
        {
            using (var db = CreateDb())
            {
                db.BulkInsert(list);
            }
        }
    }
}