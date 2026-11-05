[![CI](https://github.com/jhgbrt/yadal/actions/workflows/buildandtest.yml/badge.svg)](https://github.com/jhgbrt/yadal/actions/workflows/buildandtest.yml)
[![NuGet](https://img.shields.io/nuget/v/Net.Code.ADONet.svg)](https://www.nuget.org/packages/Net.Code.ADONet)
[![Downloads](https://img.shields.io/nuget/dt/Net.Code.ADONet.svg)](https://www.nuget.org/packages/Net.Code.ADONet)

# YaDal - yet another data access layer

In a nutshell, this library is
- a light-weight Fluent API for ADO.Net
- small enough
- designed to help you get rid of the DBNull.Value mess, as well as the overall ADO.Net cruft
- NOT an ORM

Install it from NuGet: `dotnet add package Net.Code.ADONet`

## Quick start

```csharp
// from a connection string and provider factory...
await using var db = new Db(connectionString, SqlClientFactory.Instance);
// ...or (.NET 8+) from a DbDataSource; the Db does not own the data source, so dispose it yourself
// (typically it is a singleton): await using var dataSource = NpgsqlDataSource.Create(connectionString);
//                                 await using var db = new Db(dataSource);

// map rows to objects (by column name; [Column] overrides it)
var adults = await db.Sql("SELECT * FROM Person WHERE Age >= @age")
    .WithParameters(new { age = 18 })
    .AsEnumerableAsync<Person>(cancellationToken)
    .ToListAsync(cancellationToken);

// scalars, non-queries, explicit parameter types, timeouts
var count = await db.Sql("SELECT COUNT(*) FROM Person").AsScalarAsync<int>(cancellationToken);
await db.Sql("UPDATE Person SET Name = @name WHERE Id = @id")
    .WithParameter("name", "Jane", DbType.String, size: 100)
    .WithParameter("id", 42)
    .WithTimeout(TimeSpan.FromSeconds(30))
    .AsNonQueryAsync(cancellationToken);

// simple CRUD (table/column names follow the database's mapping convention);
// sent in batches when the provider supports DbBatch (e.g. SqlClient, Npgsql; not with Oracle)
await db.InsertAsync(people, cancellationToken);
var person = await db.SelectOneAsync<Person>(42, cancellationToken);
```

All async methods accept a `CancellationToken`. A `Db` wraps a single connection that is opened when the
first command executes; it is not thread-safe, so create one per unit of work and rely on connection pooling.

## Source generated mappers (AOT-friendly)

Mark a type with `[MapFromDataRecord]` to generate a mapper (`ProductMapper.ToProduct(IDataRecord)`) at compile time:

```csharp
[MapFromDataRecord]
public partial record Product(int Id, string Name, decimal Price);

var products = db.Sql("SELECT Id, Name, Price FROM Product").AsEnumerable(ProductMapper.ToProduct).ToList();
```

The reflection-based mapping (`AsEnumerable<T>()`, `WithParameters(object)`, the CRUD helpers) and the
`dynamic` APIs are annotated as not trimming/AOT compatible; the generated mappers are.

For more information and examples, see the [wiki](https://github.com/jhgbrt/yadal/wiki).
