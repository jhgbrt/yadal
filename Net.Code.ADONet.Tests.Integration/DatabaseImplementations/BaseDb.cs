using System;
using System.Collections.Generic;
using System.Configuration;
using Net.Code.ADONet.Extensions.Experimental;

namespace Net.Code.ADONet.Tests.Integration
{
    public abstract class BaseDb : IDatabaseImpl
    {
        private static readonly string DefaultCreatePersonTable = 
            $"CREATE TABLE {nameof(Person)} (" +
            $"   {nameof(Person.Id)} int not null, " +
            $"   {nameof(Person.OptionalNumber)} int, " +
            $"   {nameof(Person.RequiredNumber)} int not null, " +
            $"   {nameof(Person.Name)} nvarchar(100) not null, " +
            $"   {nameof(Person.Email)} nvarchar(100)" +
            ");";

        private static readonly string DefaultCreateAddressTable = 
            $"CREATE TABLE {nameof(Address)} (" +
            $"   {nameof(Address.Id)} int not null, " +
            $"   {nameof(Address.Street)} nvarchar(100), " +
            $"   {nameof(Address.ZipCode)} nvarchar(20), " +
            $"   {nameof(Address.City)} nvarchar(100) not null, " +
            $"   {nameof(Address.Country)} nvarchar(100)" +
            ");";

        public virtual string EscapeChar => "@";
        public virtual MultiResultSet<Person, Address> SelectPersonAndAddress(IDb db) 
            => db.Sql($"{SelectPeople};\r\n{SelectAddresses}").AsMultiResultSet<Person, Address>();
        protected string SelectPeople => Query<Person>().SelectAll;
        protected string SelectAddresses => Query<Address>().SelectAll;
        public virtual void BulkInsert(IDb db, IEnumerable<Person> list) => db.Insert(list);
        protected string Name => GetType().Name;
        protected string MasterName => $"{Name}Master";
        public virtual string CreatePersonTable => DefaultCreatePersonTable;
        public virtual string DropPersonTable => $"DROP TABLE {nameof(Person)}";
        public virtual string CreateAddressTable => DefaultCreateAddressTable;
        public virtual string DropAddressTable => $"DROP TABLE {nameof(Address)}";
        public virtual string InsertPerson => GenerateInsertStatement<Person>();
        public virtual string InsertAddress => GenerateInsertStatement<Address>();
        protected string GenerateInsertStatement<T>() => Query<T>().Insert;
        public virtual bool SupportsMultipleResultSets => true;
        public string ProviderName => ConfigurationManager.ConnectionStrings[Name].ProviderName;
        public virtual bool SupportsTableValuedParameters => false;
        public abstract void DropAndRecreate();
        protected abstract Type ProviderType { get; }
        public string ConnectionString => ConfigurationManager.ConnectionStrings[Name].ConnectionString;
        public IDb CreateDb() => new Db(ConnectionString, Config);
        public virtual Person Project(dynamic d) => new Person { Id = d.Id, Email = d.Email, Name = d.Name, OptionalNumber = d.OptionalNumber, RequiredNumber = d.RequiredNumber };
        public IQuery Query<T>() => Extensions.Experimental.Query<T>.Create(Config.MappingConvention);
        public IMappingConvention MappingConvention => Config.MappingConvention;
        private DbConfig Config => DbConfig.FromProviderName(ProviderName);
    }
}