using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Net.Code.ADONet.Extensions.SqlClient;
using Net.Code.ADONet.Tests.Integration.Data;
using Net.Code.ADONet.Tests.Integration.Databases;
using Xunit.Abstractions;
using Xunit;
using Microsoft.VisualBasic;

namespace Net.Code.ADONet.Tests.Integration.TestSupport
{
    public class DbTestHelper<T> where T : IDatabaseImpl, new()
    {
        private readonly IDatabaseImpl _target;
        private readonly IDb _db;
        private readonly DbConfig _config;
        private Query _personQuery;
        private Query _addressQuery;
        private Query _productQuery;
        public DbTestHelper(T target, IDb db)
        {
            _target = target;
            _db = db;
            _config = _target.Config;
            _personQuery = QueryFactory<Person>.Create(_config.MappingConvention);
            _productQuery = QueryFactory<Product>.Create(_config.MappingConvention);
            _addressQuery = QueryFactory<Address>.Create(_config.MappingConvention);
        }

        public void Initialize()
        {
            _db.Connect();
            _db.Sql(_target.CreatePersonTable).AsNonQuery();
            _db.Sql(_target.CreateAddressTable).AsNonQuery();
            _db.Sql(_target.CreateProductTable).AsNonQuery();
        }

        public void Cleanup()
        {
            _db.Execute(_target.DropPersonTable);
            _db.Execute(_target.DropAddressTable);
            _db.Execute(_target.DropProductTable);
            _db.Disconnect();
        }

        public void Insert(
            IEnumerable<Person> people = null,
            IEnumerable<Address> addresses = null,
            IEnumerable<Product> products = null
            )
        {
            _db.Insert(people ?? Enumerable.Empty<Person>());
            _db.Insert(addresses ?? Enumerable.Empty<Address>());
            _db.Insert(products ?? Enumerable.Empty<Product>());
        }
        public async Task InsertAsync(
            IEnumerable<Person> people = null,
            IEnumerable<Address> addresses = null,
            IEnumerable<Product> products = null
            )
        {
            await Task.WhenAll(
                _db.InsertAsync(people ?? Enumerable.Empty<Person>()),
                _db.InsertAsync(addresses ?? Enumerable.Empty<Address>()),
                _db.InsertAsync(products ?? Enumerable.Empty<Product>())
                );
        }

        public void Update(IEnumerable<Person> items)
        {
            _db.Update(items);
        }

        public async Task InsertAsync(IEnumerable<Person> items)
        {
            foreach (var item in items)
            {
                await _db.Sql(_personQuery.Insert).WithParameters(item).AsNonQueryAsync();
            }
        }

        public List<Person> GetAllPeopleGeneric()
        {
            return _db
                .Sql(_personQuery.SelectAll)
                .AsEnumerable<Person>()
                .ToList();
        }
        public List<Person> GetAllPeopleGenericLegacy()
        {
            return _db
                .Sql(_personQuery.SelectAll)
                .AsEnumerableLegacy<Person>(((Db)_db).Config)
                .ToList();
        }

        public DataTable GetSchemaTable()
        {
            using var dataReader = _db
                .Sql(_personQuery.SelectAll)
                .AsReader();
            var dt = dataReader.GetSchemaTable();
            return dt;
        }

        public List<Person> GetAllPeopleAsDynamic()
        {
            return _db
                .Sql(_personQuery.SelectAll)
                .AsEnumerable()
                .Select(d => (Person)Project(d))
                .ToList();
        }

        public List<Person> GetAllPeopleWithDataReaderMapper()
        {
            return _db
                .Sql(_personQuery.SelectAll)
                .AsEnumerable((IDataRecord d) => d.ToPerson())
                .ToList();
        }

        Person Project(dynamic d) => new()
        {
            Id = d[GetColumnName(nameof(Person.Id))],
            Email = d[GetColumnName(nameof(Person.Email))],
            Name = d[GetColumnName(nameof(Person.Name))],
            OptionalNumber = d[GetColumnName(nameof(Person.OptionalNumber))],
            RequiredNumber = d[GetColumnName(nameof(Person.RequiredNumber))]
        };

        public async Task<List<Person>> GetAllPeopleAsDynamicAsync()
        {
            return await _db.Sql(_personQuery.SelectAll)
                        .AsEnumerableAsync()
                        .Select(d => (Person)Project(d))
                        .ToListAsync();
        }

        public List<Product> GetAllProducts()
        {
            return _db
                .Sql(_productQuery.SelectAll)
                .AsEnumerable<Product>()
                .ToList();
        }

        public DataTable PeopleAsDataTable()
        {
            return _db
                .Sql(_personQuery.SelectAll)
                .AsDataTable();
        }

        public (IReadOnlyCollection<Person>, IReadOnlyCollection<Address>) AsMultiResultSet()
        {
            Skip.IfNot(_target.SupportsMultipleResultSets, $"{_target.GetType().Name} does not support multiple result sets");
            var result = _target
                .CreateMultiResultSetCommand(_db, _personQuery.SelectAll, _addressQuery.SelectAll)
                .AsMultiResultSet<Person, Address>();
            return result;
        }

        public Person Get(int id)
        {
            return _db
                .Sql(_personQuery.Select)
                .WithParameters(new { Id = id })
                .Single<Person>();
        }

        public async Task<Person> GetAsync(int id)
        {
            return await _db
                .Sql(_personQuery.Select)
                .WithParameters(new { Id = id })
                .SingleAsync<Person>();
        }

        public (int[], Person[]) GetByIdList()
        {
            var ids = _db
                .Sql($"SELECT Id FROM {nameof(Person)}")
                .Select(d => (int)d.Id)
                .Take(3)
                .ToArray();

            if (_target.SupportsTableValuedParameters)
            {
                return (ids, _db
                    .Sql("SELECT * FROM Person JOIN @IDs IdSet ON Person.Id = IdSet.Id")
                    .WithParameter("@IDs", ids.Select(id => new { Id = id }), "IdSet")
                    .AsEnumerable<Person>()
                    .ToArray());
            }
            else
            {
                return (ids, _db
                    .Sql($"SELECT * FROM Person WHERE Id in ({string.Join(',', ids)})")
                    .AsEnumerable<Person>()
                    .ToArray());
            }
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
            if (_target.SupportsBulkInsert)
                _db.BulkCopy(list);
            else
                _db.Insert(list);
        }

        public string GetColumnName(string propertyName)
        {
            return _config.MappingConvention.ToDb(propertyName);
        }
    }
}

