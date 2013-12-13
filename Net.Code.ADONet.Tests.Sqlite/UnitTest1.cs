using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Code.ADONet.Tests.Sqlite
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {

            using (var db = Db.FromConfig())
            {
                db.Execute("DROP TABLE IF EXISTS MyTable");
                db.Sql("CREATE TABLE IF NOT EXISTS MyTable (" +
                       "id int not null, " +
                       "name nvarchar(25)," +
                       "NullableUniqueId uniqueidentifier null," +
                       "NonNullableUniqueId uniqueidentifier not null" +
                       ")"
                    ).AsNonQuery();

                var newGuid = Guid.NewGuid();
                
                db.Sql("INSERT INTO MyTable(id, name, NullableUniqueId, NonNullableUniqueId) VALUES (@id, @name, @NullableUniqueId, @NonNullableUniqueId)")
                    .WithParameters(new { id = 1, name = "Jeroen", NullableUniqueId = (Guid?)null, NonNullableUniqueId = newGuid }).AsNonQuery();
                
                var selected = db.Sql("SELECT * FROM MyTable")
                    .AsEnumerable()
                    .Select(d => new
                                 {
                                     Id = (int)d.id, 
                                     Name = (string)d.name, 
                                     NullableUniqueId = (Guid?)d.NullableUniqueId, 
                                     NonNullableUniqueId = (Guid)d.NonNullableUniqueId
                                 })
                    .First();

                Assert.AreEqual(1, selected.Id);
                Assert.AreEqual("Jeroen", selected.Name);
                Assert.IsNull(selected.NullableUniqueId);
                Assert.AreEqual(newGuid, selected.NonNullableUniqueId);

            }
        }
    }
}
