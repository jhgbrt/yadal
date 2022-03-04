using Net.Code.ADONet.Tests.Integration.Data;

using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Net.Code.ADONet.Tests.Integration.Databases
{
    public abstract class BaseDb<T> : IDatabaseImpl where T : BaseDb<T>, new()
    {
        public BaseDb(DbProviderFactory factory)
        {
            Factory = factory;
        }
        static readonly Lazy<bool> _available;
        static BaseDb()
        {
            _available = new Lazy<bool>(() => new T().CanConnect());
            if (_available.Value)
            {
                new T().DropAndRecreate();
            }
        }
        public bool IsAvailable() => _available.Value;

        private string DefaultCreatePersonTable() =>
           $"""
            CREATE TABLE {ToDb(nameof(Person))} (
                {ToDb(nameof(Person.Id))} int not null,
                {ToDb(nameof(Person.OptionalNumber))} int,
                {ToDb(nameof(Person.RequiredNumber))} int not null,
                {ToDb(nameof(Person.Name))} varchar(100) not null,
                {ToDb(nameof(Person.Email))} varchar(100)
            );
            """;

        private string DefaultCreateAddressTable() =>
           $"""
            CREATE TABLE {ToDb(nameof(Address))} (
                {ToDb(nameof(Address.Id))} int not null,
                {ToDb(nameof(Address.Street))} varchar(100),
                {ToDb(nameof(Address.ZipCode))} varchar(20),
                {ToDb(nameof(Address.City))} varchar(100) not null,
                {ToDb(nameof(Address.Country))} varchar(100)
            );
            """;

        private string DefaultCreateProductTable() =>
           $"""
            CREATE TABLE {ToDb(nameof(Product))} (
               {ToDb(nameof(Product.Id))} int not null,
               {ToDb(nameof(Product.Name))} nvarchar(100),
               {ToDb(nameof(Product.Price))} decimal not null
            );
            """;

        public virtual (IReadOnlyCollection<Person>, IReadOnlyCollection<Address>) SelectPersonAndAddress(IDb db)
            => db.Sql($"{SelectPeople};\r\n{SelectAddresses}").AsMultiResultSet<Person, Address>();
        protected string SelectPeople => CreateQuery<Person>().SelectAll;
        protected string SelectAddresses => CreateQuery<Address>().SelectAll;
        public virtual void BulkInsert(IDb db, IEnumerable<Person> list) => db.Insert(list);
        protected string Name => GetType().Name.Replace("Db", "");
        protected string MasterName => $"{Name}Master";
        public virtual string CreatePersonTable => DefaultCreatePersonTable();
        public virtual string CreateProductTable => DefaultCreateProductTable();
        public virtual string DropPersonTable => $"DROP TABLE {ToDb(nameof(Person))}";
        public virtual string DropProductTable => $"DROP TABLE {ToDb(nameof(Product))}";
        private string ToDb(string name) => Config.MappingConvention.ToDb(name);
        public virtual string CreateAddressTable => DefaultCreateAddressTable();
        public virtual string DropAddressTable => $"DROP TABLE {ToDb(nameof(Address))}";
        public virtual string InsertPerson => CreateQuery<Person>().Insert;
        public virtual bool SupportsMultipleResultSets => true;
        public virtual bool SupportsTableValuedParameters => false;
        public abstract void DropAndRecreate();
        public string ConnectionString => Configuration.ConnectionStrings[Name];
        string MasterConnectionString => Configuration.ConnectionStrings[MasterName];
        public IDb CreateDb() => new Db(ConnectionString, Config, Factory);
        public IDb MasterDb() => new Db(MasterConnectionString, Config, Factory);
        public virtual Person Project(dynamic d) => new()
        {
            Id = d[ToDb(nameof(Person.Id))],
            Email = d[ToDb(nameof(Person.Email))],
            Name = d[ToDb(nameof(Person.Name))],
            OptionalNumber = d[ToDb(nameof(Person.OptionalNumber))],
            RequiredNumber = d[ToDb(nameof(Person.RequiredNumber))]
        };
        public Query CreateQuery<TItem>() => QueryFactory<TItem>.Create(Config.MappingConvention);
        public DbConfig Config => DbConfig.FromProviderFactory(Factory);
        protected DbProviderFactory Factory { get; }

        protected bool CanConnect()
        {
            try
            {
                using var connection = Factory.CreateConnection();
                connection.ConnectionString = MasterConnectionString;
                connection.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected string GetConnectionStringProperty(string keyword)
        {
            var connectionString = ConnectionString;
            var connectionStringBuilder = Factory.CreateConnectionStringBuilder();
            connectionStringBuilder.ConnectionString = connectionString;
            var databaseName = (string)connectionStringBuilder[keyword];
            return databaseName;
        }
    }
}