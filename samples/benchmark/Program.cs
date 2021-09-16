using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Dapper;
using Net.Code.ADONet;
using Microsoft.Data.Sqlite;
using System.Data.Common;

var summary = BenchmarkRunner.Run<Benchmarks>();
  
public class Benchmarks
{
    //private const int N = 10000;
    private readonly IDb _db;
    private readonly DbConnection  _connection;

    public Benchmarks()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _db = new Db(_connection, DbConfig.Default);
        _db.Connect();
        _db.Sql(@"create table Post
	                    (
		                    Id int identity primary key, 
		                    [Text] varchar(2000) not null, 
		                    CreationDate datetime not null, 
		                    LastChangeDate datetime not null,
		                    Counter1 int,
		                    Counter2 int,
		                    Counter3 int,
		                    Counter4 int,
		                    Counter5 int,
		                    Counter6 int,
		                    Counter7 int,
		                    Counter8 int,
		                    Counter9 int
	                    )").AsNonQuery();
        var text = new string(Enumerable.Repeat('x', 1999).ToArray());
        var items = Enumerable.Range(1, 5000).Select(i => new Post(i, text + i, DateTime.Now, DateTime.Now, null, null, null, i, null, null, null, null, null));
        //{
        //    Id = i,
        //    Text = text + i,
        //    CreationDate = DateTime.Now,
        //    LastChangeDate = DateTime.Now,
        //    Counter4 = i
        //}); 
        _db.Insert(items);
    }

    [Benchmark]
    public Post NetCodeAdoNet()
    {
        return _db.Sql("select Id, Text, CreationDate, LastChangeDate, Counter1, Counter2, Counter3, Counter4, Counter5, Counter6, Counter7, Counter8, Counter9 from Post where Id=@Id").WithParameters(new { Id = 42 }).Single<Post>();
    }

    [Benchmark]
    public PostD Dapper() 
    {
        return _connection.Query<PostD>("select * from Post where Id=@Id", new { Id = 42 }, buffered: false).First();
    }
}

public record Post(int Id, string Text, DateTime? CreationDate, DateTime? LastChangeDate,
    int? Counter1,
    int? Counter2,
    int? Counter3,
    int? Counter4,
    int? Counter5,
    int? Counter6,
    int? Counter7,
    int? Counter8,
    int? Counter9
    );

public class PostD
{
    public int Id { get; set; }
    public string Text { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime LastChangeDate { get; set; }
    public int? Counter1 { get; set; }
    public int? Counter2 { get; set; }
    public int? Counter3 { get; set; }
    public int? Counter4 { get; set; }
    public int? Counter5 { get; set; }
    public int? Counter6 { get; set; }
    public int? Counter7 { get; set; }
    public int? Counter8 { get; set; }
    public int? Counter9 { get; set; }
}
