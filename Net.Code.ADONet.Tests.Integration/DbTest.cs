using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Net.Code.ADONet.Extensions.SqlClient;

namespace Net.Code.ADONet.Tests.Integration
{
    public class DbTest
    {
        private readonly IDatabaseImpl _target;

        public DbTest(IDatabaseImpl target)
        {
            _target = target;
        }

        public void DropRecreate()
        {
            _target.DropAndRecreate();
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

        public void Insert(IEnumerable<Person> items)
        {
            using (var db = CreateDb())
            {
                foreach (var item in items)
                {
                    db.Sql(_target.InsertPerson).WithParameters(item).AsNonQuery();
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
    }
}