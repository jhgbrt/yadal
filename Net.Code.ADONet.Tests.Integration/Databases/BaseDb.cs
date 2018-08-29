using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using Net.Code.ADONet.Extensions.Experimental;
using Net.Code.ADONet.Tests.Integration.Data;

namespace Net.Code.ADONet.Tests.Integration.Databases
{
    public abstract class BaseDb : IDatabaseImpl
    {
        private string DefaultCreatePersonTable() => 
            $"CREATE TABLE {ToDb(nameof(Person))} (" +
            $"   {ToDb(nameof(Person.Id))} int not null, " +
            $"   {ToDb(nameof(Person.OptionalNumber))} int, " +
            $"   {ToDb(nameof(Person.RequiredNumber))} int not null, " +
            $"   {ToDb(nameof(Person.Name))} varchar(100) not null, " +
            $"   {ToDb(nameof(Person.Email))} varchar(100)" +
            ");";

        private string DefaultCreateAddressTable() => 
            $"CREATE TABLE {ToDb(nameof(Address))} (" +
            $"   {ToDb(nameof(Address.Id))} int not null, " +
            $"   {ToDb(nameof(Address.Street))} varchar(100), " +
            $"   {ToDb(nameof(Address.ZipCode))} varchar(20), " +
            $"   {ToDb(nameof(Address.City))} varchar(100) not null, " +
            $"   {ToDb(nameof(Address.Country))} varchar(100)" +
            ");";

        public virtual MultiResultSet<Person, Address> SelectPersonAndAddress(IDb db) 
            => db.Sql($"{SelectPeople};\r\n{SelectAddresses}").AsMultiResultSet<Person, Address>();
        protected string SelectPeople => Query<Person>().SelectAll;
        protected string SelectAddresses => Query<Address>().SelectAll;
        public virtual void BulkInsert(IDb db, IEnumerable<Person> list) => db.Insert(list);
        protected string Name => GetType().Name.Replace("Db", "");
        protected string MasterName => $"{Name}Master";
        public virtual string CreatePersonTable => DefaultCreatePersonTable();
        public virtual string DropPersonTable => $"DROP TABLE {ToDb(nameof(Person))}";

        private string ToDb(string name)
        {
            return Config.MappingConvention.ToDb(name);
        }

        public virtual string CreateAddressTable => DefaultCreateAddressTable();
        public virtual string DropAddressTable => $"DROP TABLE {ToDb(nameof(Address))}";
        public virtual string InsertPerson => Query<Person>().Insert;
        public virtual bool SupportsMultipleResultSets => true;
        public string ProviderName => ConfigurationManager.ConnectionStrings[Name].ProviderName;
        public virtual bool SupportsTableValuedParameters => false;
        public abstract void DropAndRecreate();
        public string ConnectionString => ConfigurationManager.ConnectionStrings[Name].ConnectionString;
        string MasterConnectionString => ConfigurationManager.ConnectionStrings[MasterName].ConnectionString;
        public IDb CreateDb() => new Db(ConnectionString, Config, Factory);
        public virtual Person Project(dynamic d) => new Person
        {
            Id = d[ToDb(nameof(Person.Id))],
            Email = d[ToDb(nameof(Person.Email))],
            Name = d[ToDb(nameof(Person.Name))],
            OptionalNumber = d[ToDb(nameof(Person.OptionalNumber))],
            RequiredNumber = d[ToDb(nameof(Person.RequiredNumber))]
        };
        public IQuery Query<T>() => Extensions.Experimental.Query<T>.Create(Config.MappingConvention);
        private DbConfig Config => DbConfig.FromProviderName(ProviderName);
        private DbProviderFactory Factory => DbProviderFactories.GetFactory(ProviderName);
        public bool EstablishConnection()
        {
            using (var db = new Db(MasterConnectionString, Config, Factory))
            {
                db.Connect();
            }

            return true;
        }
    }
}