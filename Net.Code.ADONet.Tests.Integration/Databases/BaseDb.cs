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
            Config = DbConfig.FromProviderFactory(factory);
        }

        public virtual IReadOnlyDictionary<string, string>? AlternateConnectionStrings => null;

        public virtual CommandBuilder CreateMultiResultSetCommand(IDb db, string query1, string query2)
            => db.Sql($"{query1};\r\n{query2}");

        public virtual bool SupportsBulkInsert => false;
        public string Name => GetType().Name.Replace("Db", "");

        protected string GetTableName<TInner>() => typeof(TInner).GetTableName(Config.MappingConvention);
        protected string GetColumnName<TInner>(string propertyName) => typeof(TInner).GetProperty(propertyName)?.GetColumnName(Config.MappingConvention) ?? propertyName;
        public virtual string CreatePersonTable => $"""
            CREATE TABLE {GetTableName<Person>()} (
                {GetColumnName<Person>(nameof(Person.Id))} int not null,
                {GetColumnName<Person>(nameof(Person.OptionalNumber))} int,
                {GetColumnName<Person>(nameof(Person.RequiredNumber))} int not null,
                {GetColumnName<Person>(nameof(Person.BirthDate))} datetime not null,
                {GetColumnName<Person>(nameof(Person.RegisteredAt))} datetime not null,
                {GetColumnName<Person>(nameof(Person.Name))} varchar(100) not null,
                {GetColumnName<Person>(nameof(Person.Email))} varchar(100)
            );
            """;

        public virtual string CreateProductTable => $"""
            CREATE TABLE {typeof(Product).GetTableName(Config.MappingConvention)} (
               {GetColumnName<Product>(nameof(Product.Id))} int not null,
               {GetColumnName<Product>(nameof(Product.Name))} varchar(100),
               {GetColumnName<Product>(nameof(Product.Price))} decimal(16,2) not null
            );
            """;

        public virtual string CreateAddressTable => $"""
            CREATE TABLE {typeof(Address).GetTableName(Config.MappingConvention)} (
                {GetColumnName<Address>(nameof(Address.Id     ))} int not null,
                {GetColumnName<Address>(nameof(Address.Street ))} varchar(100),
                {GetColumnName<Address>(nameof(Address.ZipCode))} varchar(20),
                {GetColumnName<Address>(nameof(Address.City   ))} varchar(100) not null,
                {GetColumnName<Address>(nameof(Address.Country))} varchar(100)
            );
            """;
        public virtual string DropPersonTable => $"DROP TABLE {typeof(Person).GetTableName(Config.MappingConvention)}";
        public virtual string DropProductTable => $"DROP TABLE {typeof(Product).GetTableName(Config.MappingConvention)}";
        public virtual string DropAddressTable => $"DROP TABLE {typeof(Address).GetTableName(Config.MappingConvention)}";

        private string ToDb(string name) => Config.MappingConvention.ToDb(name);

        public virtual bool SupportsMultipleResultSets => true;
        public virtual bool SupportsTableValuedParameters => false;

        public virtual IEnumerable<string> GetAfterInitSql() { return Array.Empty<string>(); }
        public virtual IEnumerable<string> GetDropAndRecreateDdl() { return Array.Empty<string>(); }
        public DbConfig Config { get; }
        public DbProviderFactory Factory { get; }

    }
}