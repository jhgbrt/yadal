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
using System.Linq.Expressions;
using System;

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
            _personQuery = QueryFactory<Person>.Get(_config.MappingConvention);
            _productQuery = QueryFactory<Product>.Get(_config.MappingConvention);
            _addressQuery = QueryFactory<Address>.Get(_config.MappingConvention);
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
            IEnumerable<Person>? people = null,
            IEnumerable<Address>? addresses = null,
            IEnumerable<Product>? products = null
            )
        {
            _db.Insert(people ?? []);
            _db.Insert(addresses ?? []);
            _db.Insert(products ?? []);
        }
        public async Task InsertAsync(
            IEnumerable<Person>? people = null,
            IEnumerable<Address>? addresses = null,
            IEnumerable<Product>? products = null
            )
        {
            await _db.InsertAsync(people ?? []);
            await _db.InsertAsync(addresses ?? []);
            await _db.InsertAsync(products ?? []);
        }

        public void Update(IEnumerable<Person> items)
        {
            _db.Update(items);
        }

        public void Delete(IEnumerable<Person> items)
        {
            _db.Delete(items);
        }

        public Person? SelectOne(int Id)
        {
            return _db.SelectOne<Person>(Id);
        }
        public ValueTask<Person?> SelectOneAsync(int Id)
        {
            return _db.SelectOneAsync<Person>(Id);
        }

        public async Task DeleteAsync(IEnumerable<Person> items)
        {
            await _db.DeleteAsync(items);
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

        public DataTable? GetSchemaTable()
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
                .AsEnumerable(PersonMapper.ToPerson)
                .ToList();
        }

        Person Project(dynamic d) => new()
        {
            Id = d[GetColumnName<Person>(nameof(Person.Id))],
            Email = d[GetColumnName<Person>(nameof(Person.Email))],
            Name = d[GetColumnName<Person>(nameof(Person.Name))],
            OptionalNumber = d[GetColumnName<Person>(nameof(Person.OptionalNumber))],
            RequiredNumber = d[GetColumnName<Person>(nameof(Person.RequiredNumber))]
        };

        public async Task<List<Person>> GetAllPeopleAsDynamicAsync()
        {
            return await _db.Sql(_personQuery.SelectAll)
                        .AsEnumerableAsync()
                        .Select(d => (Person)Project(d))
                        .ToListAsync();
        }

        public List<Address> GetAllAddresses()
        {
            return _db.Sql(_addressQuery.SelectAll).AsEnumerable<Address>().ToList();
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
                .Sql($"SELECT Id FROM {GetTableName<Person>()}")
                .Select(d => (int)d.Id)
                .Take(3)
                .ToArray();

            if (_target.SupportsTableValuedParameters)
            {
                return (ids, _db
                    .Sql($"SELECT * FROM {GetTableName<Person>()} JOIN @IDs IdSet ON {GetTableName<Person>()}.Id = IdSet.Id")
                    .WithParameter("@IDs", ids.Select(id => new { Id = id }), "IdSet")
                    .AsEnumerable<Person>()
                    .ToArray());
            }
            else
            {
                return (ids, _db
                    .Sql($"SELECT * FROM {GetTableName<Person>()} WHERE Id in ({string.Join(',', ids)})")
                    .AsEnumerable<Person>()
                    .ToArray());
            }
        }

        public int GetCountOfPeople() => _db.Count<Person>();

        public Task<int> GetCountOfPeopleAsync() => _db.CountAsync<Person>();

        public void BulkInsert(IEnumerable<Person> list)
        {
            if (_target.SupportsBulkInsert)
                _db.BulkCopy(list);
            else
                _db.Insert(list);
        }
        public async Task BulkInsertAsync(IEnumerable<Person> list)
        {
            if (_target.SupportsBulkInsert)
                await _db.BulkCopyAsync(list);
            else
                await  _db.InsertAsync(list);
        }

        public string GetTableName<TEntity>() => typeof(TEntity).GetTableName(_config.MappingConvention);
        public string GetColumnName<TInner>(string propertyName)
        {
            return typeof(TInner).GetProperty(propertyName)?.GetColumnName(_config.MappingConvention) ?? propertyName;
        }
    }
}

