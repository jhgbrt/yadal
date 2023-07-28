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

        public virtual CommandBuilder CreateMultiResultSetCommand(IDb db, string query1, string query2)
            => db.Sql($"{query1};\r\n{query2}");

        public virtual bool SupportsBulkInsert => false;
        public string Name => GetType().Name.Replace("Db", "");
        public virtual string CreatePersonTable => $"""
            CREATE TABLE {ToDb(nameof(Person))} (
                {ToDb(nameof(Person.Id))} int not null,
                {ToDb(nameof(Person.OptionalNumber))} int,
                {ToDb(nameof(Person.RequiredNumber))} int not null,
                {ToDb(nameof(Person.Name))} varchar(100) not null,
                {ToDb(nameof(Person.Email))} varchar(100)
            );
            """;

        public virtual string CreateProductTable => $"""
            CREATE TABLE {ToDb(nameof(Product))} (
               {ToDb(nameof(Product.Id))} int not null,
               {ToDb(nameof(Product.Name))} varchar(100),
               {ToDb(nameof(Product.Price))} decimal(16,2) not null
            );
            """;

        public virtual string CreateAddressTable => $"""
            CREATE TABLE {ToDb(nameof(Address))} (
                {ToDb(nameof(Address.Id))} int not null,
                {ToDb(nameof(Address.Street))} varchar(100),
                {ToDb(nameof(Address.ZipCode))} varchar(20),
                {ToDb(nameof(Address.City))} varchar(100) not null,
                {ToDb(nameof(Address.Country))} varchar(100)
            );
            """;
        public virtual string DropPersonTable => $"DROP TABLE {ToDb(nameof(Person))}";
        public virtual string DropProductTable => $"DROP TABLE {ToDb(nameof(Product))}";
        public virtual string DropAddressTable => $"DROP TABLE {ToDb(nameof(Address))}";

        private string ToDb(string name) => Config.MappingConvention.ToDb(name);

        public virtual bool SupportsMultipleResultSets => true;
        public virtual bool SupportsTableValuedParameters => false;

        public virtual IEnumerable<string> GetAfterInitSql() { return Array.Empty<string>(); }
        public virtual IEnumerable<string> GetDropAndRecreateDdl() { return Array.Empty<string>(); }
        public DbConfig Config { get; }
        public DbProviderFactory Factory { get; }

    }
}