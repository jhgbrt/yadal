using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Net.Code.ADONet.Tests.Integration.Data;

using Npgsql;

namespace Net.Code.ADONet.Tests.Integration.Databases
{
    public class PostgreSqlDb : BaseDb<PostgreSqlDb>
    {
        public PostgreSqlDb() : base(NpgsqlFactory.Instance)
        {
        }
        public override string CreatePersonTable => $"""
            CREATE TABLE {GetTableName<Person>()} (
                {GetColumnName<Person>(nameof(Person.Id))} int not null,
                {GetColumnName<Person>(nameof(Person.OptionalNumber))} int,
                {GetColumnName<Person>(nameof(Person.RequiredNumber))} int not null,
                {GetColumnName<Person>(nameof(Person.BirthDate))} date not null,
                {GetColumnName<Person>(nameof(Person.RegisteredAt))} timestamp not null,
                {GetColumnName<Person>(nameof(Person.Name))} varchar(100) not null,
                {GetColumnName<Person>(nameof(Person.Email))} varchar(100)
            );
            """;

        public override IEnumerable<string> GetDropAndRecreateDdl()
        {
            var databaseName = Configuration.GetConnectionStringProperty(Name, "Database");

            yield return $"DROP DATABASE IF EXISTS {databaseName};";
            yield return $"CREATE DATABASE {databaseName};";
        }
       
    }
}