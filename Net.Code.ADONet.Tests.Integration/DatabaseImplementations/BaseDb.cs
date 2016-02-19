using System;
using System.Configuration;
using System.Linq;
using Net.Code.ADONet.Extensions;
using Net.Code.ADONet.Extensions.Experimental;

namespace Net.Code.ADONet.Tests.Integration
{
    public abstract class BaseDb : IDatabaseImpl
    {
        
        protected BaseDb()
        {
            Console.WriteLine(ProviderType);
        }

        private static readonly string DefaultCreatePersonTable = $"CREATE TABLE {nameof(Person)} (" +
                                                 $"   {nameof(Person.Id)} int not null, " +
                                                 $"   {nameof(Person.OptionalNumber)} int, " +
                                                 $"   {nameof(Person.RequiredNumber)} int not null, " +
                                                 $"   {nameof(Person.Name)} nvarchar(100) not null, " +
                                                 $"   {nameof(Person.Email)} nvarchar(100)" +
                                                 ");";
        private static readonly string DefaultCreateAddressTable = $"CREATE TABLE {nameof(Address)} (" +
                                                 $"   {nameof(Address.Id)} int not null, " +
                                                 $"   {nameof(Address.Street)} nvarchar(100), " +
                                                 $"   {nameof(Address.ZipCode)} nvarchar(20), " +
                                                 $"   {nameof(Address.City)} nvarchar(100) not null, " +
                                                 $"   {nameof(Address.Country)} nvarchar(100)" +
                                                 ");";

        private string FormatAsNamedParameters<T>()
        {
            var type = typeof(T);
            var names = type.GetProperties().Select(p => $"{EscapeChar}{p.Name}");
            return string.Join(",", names);
        }
        private string FormatAsColumnNames<T>()
        {
            var type = typeof(T);
            var names = type.GetProperties().Select(p => p.Name);
            return string.Join(",", names);
        }

        public virtual string EscapeChar => "@";

        public virtual MultiResultSet<Person, Address> SelectPersonAndAddress(IDb db)
        {
            var query = $"{$"SELECT * FROM {nameof(Person)};\r\n"}{$"SELECT * FROM {nameof(Address)}"}";
            return db.Sql(query).AsMultiResultSet<Person, Address>();
        }

        public virtual void BulkInsert(IDb db, Person[] list)
        {
            db.Insert(list);
        }

        protected string Name => GetType().Name;
        protected string MasterName => $"{Name}Master";

        public virtual string CreatePersonTable => DefaultCreatePersonTable;
        public virtual string CreateAddressTable => DefaultCreateAddressTable;
        public virtual string InsertPerson => GenerateInsertStatement<Person>();
        public virtual string InsertAddress => GenerateInsertStatement<Address>();

        protected virtual string GenerateInsertStatement<T>()
        {
            return $"INSERT INTO {typeof (T).Name} ({FormatAsColumnNames<T>()}) VALUES ({FormatAsNamedParameters<T>()})";
        }

        public virtual bool SupportsMultipleResultSets => true;
        public string ProviderName => ConfigurationManager.ConnectionStrings[Name].ProviderName;
        public virtual bool SupportsTableValuedParameters => false;

        public abstract void DropAndRecreate();

        protected abstract Type ProviderType { get; }

        public IDb CreateDb()
        {
            return Db.FromConfig(Name);
        }

        public virtual Person Project(dynamic d)
        {
            return new Person
            {
                Id = d.Id,
                Email = d.Email,
                Name = d.Name,
                OptionalNumber = d.OptionalNumber,
                RequiredNumber = d.RequiredNumber
            };
        }
    }
}