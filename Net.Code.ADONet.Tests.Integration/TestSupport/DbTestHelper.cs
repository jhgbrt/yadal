using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Net.Code.ADONet.Extensions.Experimental;
using Net.Code.ADONet.Extensions.SqlClient;
using Net.Code.ADONet.Tests.Integration.Data;
using Net.Code.ADONet.Tests.Integration.Databases;
using Xunit.Abstractions;

namespace Net.Code.ADONet.Tests.Integration.TestSupport
{
    public class DbTestHelper
    {
        private readonly IDatabaseImpl _target;
        private readonly IDb _db;
        private readonly ITestOutputHelper _output;
        public DbTestHelper(IDatabaseImpl target, ITestOutputHelper output)
        {
            _target = target;
            _output = output;
            Logger.Log = _output.WriteLine;
            _db = _target.CreateDb();
        }

        public void Initialize()
        {
            _db.Connect();
            _db.Sql(_target.CreatePersonTable).AsNonQuery();
            _db.Sql(_target.CreateAddressTable).AsNonQuery();
        }

        public void Cleanup()
        {
            _db.Execute(_target.DropPersonTable);
            _db.Execute(_target.DropAddressTable);
            Logger.Log = s => { };
        }

        public void Insert(IEnumerable<Person> people, IEnumerable<Address> addresses)
        {
            _db.Insert(people);
            _db.Insert(addresses);
        }

        public void Update(IEnumerable<Person> items)
        {
            _db.Update(items);
        }

        public async Task InsertAsync(IEnumerable<Person> items)
        {
            foreach (var item in items)
            {
                await _db.Sql(_target.InsertPerson).WithParameters(item).AsNonQueryAsync();
            }
        }

        public List<Person> GetAllPeopleGeneric()
        {
            return _db
                .Sql(_target.Query<Person>().SelectAll)
                .AsEnumerable<Person>()
                .ToList();
        }
        public List<Person> GetAllPeopleGenericLegacy()
        {
            return _db
                .Sql(_target.Query<Person>().SelectAll)
                .AsEnumerableLegacy<Person>(((Db)_db).Config)
                .ToList();
        }

        public DataTable GetSchemaTable()
        {
            var dataReader = _db
                .Sql(_target.Query<Person>().SelectAll)
                .AsReader();
            using (dataReader)
            {
                var dt = dataReader.GetSchemaTable();
                return dt;
            }
        }

        public List<Person> GetAllPeopleAsDynamic()
        {
            return _db
                .Sql(_target.Query<Person>().SelectAll)
                .AsEnumerable()
                .Select(d => (Person) _target.Project(d))
                .ToList();
        }

        public async Task<List<Person>> GetAllPeopleAsDynamicAsync()
        {
            var people = await _db
                .Sql(_target.Query<Person>().SelectAll)
                .AsEnumerableAsync();
            return people
                .Select(d => (Person) _target.Project(d))
                .ToList();
        }

        public DataTable PeopleAsDataTable()
        {
            return _db
                .Sql(_target.Query<Person>().SelectAll)
                .AsDataTable();
        }

        public (IReadOnlyCollection<Person>, IReadOnlyCollection<Address>) AsMultiResultSet()
        {
            var result = _target.SelectPersonAndAddress(_db);
            return result;
        }

        public Person Get(int id)
        {
            return _db
                .Sql(_target.Query<Person>().Select)
                .WithParameters(new {Id = id})
                .Single<Person>();
        }

        public Person[] GetPeopleById(IEnumerable<int> ids)
        {
            return _db
                .Sql("SELECT * FROM Person JOIN @IDs IdSet ON Person.Id = IdSet.Id")
                .WithParameter("@IDs", ids.Select(id => new {Id = id}), "IdSet")
                .AsEnumerable<Person>()
                .ToArray();
        }

        public List<int> GetSomeIds(int n)
        {
            return _db.Sql($"SELECT TOP {n} Id FROM {nameof(Person)}").Select(d => (int) d.Id).ToList();
        }

        public int GetCountOfPeople()
        {
            return _db.Sql($"SELECT count(*) FROM {nameof(Person)}").AsScalar<int>();
        }

        public async Task<int> GetCountOfPeopleAsync()
        {
            var result = await _db.Sql($"SELECT count(*) FROM {nameof(Person)}").AsScalarAsync<int>();
            return result;
        }

        public void BulkInsert(IEnumerable<Person> list)
        {
            _target.BulkInsert(_db, list);
        }

        public string GetColumnName(string propertyName)
        {
            return ((Db)(_db)).Config.MappingConvention.ToDb(propertyName);
        }
    }
}
