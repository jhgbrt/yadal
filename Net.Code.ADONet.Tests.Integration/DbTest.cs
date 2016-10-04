using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Net.Code.ADONet.Extensions.Experimental;
using Net.Code.ADONet.Extensions.SqlClient;

namespace Net.Code.ADONet.Tests.Integration
{
    public class DbTest
    {
        private readonly IDatabaseImpl _target;
        private readonly IDb _db;
        public DbTest(IDatabaseImpl target)
        {
            _target = target;
            _db = CreateDb();
        }

        public void CreateTables()
        {
            _db.Sql(_target.CreatePersonTable).AsNonQuery();
            _db.Sql(_target.CreateAddressTable).AsNonQuery();
        }

        public void DropTables()
        {
            _db.Execute(_target.DropPersonTable);
            _db.Execute(_target.DropAddressTable);
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

        static Dictionary<Type, Exception> _exceptions = new Dictionary<Type, Exception>();
        internal void Connect()
        {
            if (!_exceptions.ContainsKey(_target.GetType()))
            {
                try
                {
                    _db.Connect();
                }
                catch (Exception e)
                {
                    _exceptions[_target.GetType()] = e;
                }
            }
            if (_exceptions.ContainsKey(_target.GetType()))
                throw _exceptions[_target.GetType()];
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

        public DataTable AsDataTable()
        {
            return _db
                .Sql(_target.Query<Person>().SelectAll)
                .AsDataTable();
        }

        public MultiResultSet<Person, Address> AsMultiResultSet()
        {
            var result = _target.SelectPersonAndAddress(_db);
            return result;
        }

        private IDb CreateDb()
        {
            return _target.CreateDb();
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
    }
}