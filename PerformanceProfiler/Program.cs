// See https://aka.ms/new-console-template for more information

using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

using Net.Code.ADONet;

using var db = new Db("Data Source=:memory:", SqliteFactory.Instance);
db.Connect();

db.Sql(@"create table Post
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
var items = Enumerable.Range(1, 50).Select(i => new Post
{
    Id = i,
    Text = text + i,
    CreationDate = DateTime.Now,
    LastChangeDate = DateTime.Now,
    Counter4 = i
});
db.Insert(items);


List<Post> posts = new List<Post>();   
for (int i = 1; i < 5000; i++)
{
    var post = db.Sql("select * from Post where Id = @Id").WithParameters(new { Id = i % 50 + 1 }).Single<Post>();
    posts.Add(post);
}

GC.KeepAlive(posts);

public class Post
{
    public int Id { get; set; }
    public string? Text { get; set; }
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
