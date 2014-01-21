# YaDal - yet another data access layer

In a nutshell, this library is
- a light-weight Fluent API for ADO.Net
- small enough
- designed to help you get rid of the DBNull.Value mess, as well as the overall ADO.Net cruft
- NOT an ORM

To use it, you can use nuget (install-package Net.Code.AdoNet), or just drop the Db.cs file in your project. The second option allows you to more easily customize the code to your own needs.

This tool aims to be a simple, light-weight helper library for making ADO.Net a little bit easier to work with in the most common scenarios, without attempting to completely abstracting away the underlying technology. Although YaDal embraces similar ideas as Massive (the light-weight ORM by @robconery), it is a little bit different in it's goals: it does not try to be an ORM at all. All you can do with it is execute some inline SQL statements against a database.

So please don't use this as the full-fledged ORM; use NHibernate or Entity Framework or <insert your favorite framework here> for that. YaDal is aimed for those situations where you simply want to quickly throw some SQL or DDL at the database through ADO.Net.

At the same time, it helps you get rid of the mess that is DBNull.Value, or at least that's one of it's principle goals. And this one is kind of critical to me, so if you run into any issues regarding DBNull, please let me know.

# Examples

## Classic ADO.Net 

A simple example says it all - I hope.
 
In classic ADO.Net, we typically all write code like this:

      string connectionString = ConfigurationManager.ConnectionStrings[0].ConnectionString; 
      IDbConnection connection = new SqlConnection(connectionString);
      using (connection) 
      {
          IDbCommand cmd = new SqlCommand();
          cmd.CommandType = CommandType.StoredProcedure;
          IDbParameter p;
          p = new SqlParameter("myParameter1", myValue1);
          cmd.Parameters.Add(p);
          p = new SqlParameter("myParameter2", myValue2);
          cmd.Parameters.Add(p);
          // add more parameters as needed
          object dbReturnValue = cmd.ExecuteScalar();
          int? result = DBNull.Value == dbReturnValue ? (int?)null : (int) dbReturnValue;
      }

## YaDal

With YaDal, the same code looks like this:

      using (var db = Db.FromConfig()) 
      {
         var sproc = db.StoredProcedure("MySproc")
           .WithParameter("myParameter1", myValue1)
           .WithParameter("myParameter2", myValue2);
         int? result = sproc.AsScalar<int?>();
      }

That's all there is to it really. There's some stuff to aid in mapping to objects, and doing some type conversions, but the API should be pretty self explanatory.

## Running an inline parametrized SQL statement

     using (var db = Db.FromConfig())
     {
         var query = db.Sql("INSERT INTO Products (Name) VALUES (@Name)")
                       .WithParameters(new {Name = "Nikon D60"});
         var result = query.AsNonQuery();
     }

## Querying a collection, projecting into an anonymous object

     using (var db = Db.FromConfig())
     {
         var query = db.Sql("SELECT Id, Name FROM Products WHERE Name LIKE @SearchString")
                       .WithParameters(new {SearchString = "Nikon%"});
         var list = query.AsEnumerable().Select(row => new {row.Id, row.Name}).ToList();
     }

## Running a stored procedure

Running a stored procedure is very similar to running an inline sql statement. One simply replaces the `.Sql()` call with `.StoredProcedure()`.
