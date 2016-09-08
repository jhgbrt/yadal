using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net.Code.ADONet.Extensions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Net.Code.ADONet.Tests.Unit.Extensions.Experimental
{

    class MyEntityWithGeneratedId
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    class MyEntity
    {
        public int MyEntityId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
    class MyEntityWithCompositeKey
    {
        [Key]
        public string Parent { get; set; }
        [Key]
        public string Name { get; set; }
        public string Description { get; set; }
    }

    [TestClass]
    public class QueryGeneratorTestsForEntityWithDatabaseGeneratedId
    {
        IQueryGenerator generator = Query<MyEntityWithGeneratedId>.Create("SqlClient");

        [TestMethod]
        public void Insert()
        {
            var sql = generator.Insert;
            Assert.AreEqual("INSERT INTO MyEntityWithGeneratedId (Name, Description) VALUES (@Name, @Description)", sql);
        }
        [TestMethod]
        public void Delete()
        {
            var sql = generator.Delete;
            Assert.AreEqual("DELETE FROM MyEntityWithGeneratedId WHERE Id = @Id", sql);
        }
        [TestMethod]
        public void Update()
        {
            var sql = generator.Update;
            Assert.AreEqual("UPDATE MyEntityWithGeneratedId SET Name = @Name, Description = @Description WHERE Id = @Id", sql);
        }
        [TestMethod]
        public void Select()
        {
            var sql = generator.Select;
            Assert.AreEqual("SELECT Id, Name, Description FROM MyEntityWithGeneratedId WHERE Id = @Id", sql);
        }
        [TestMethod]
        public void SelectAll()
        {
            var sql = generator.SelectAll;
            Assert.AreEqual("SELECT Id, Name, Description FROM MyEntityWithGeneratedId", sql);
        }
        [TestMethod]
        public void Count()
        {
            var sql = generator.Count;
            Assert.AreEqual("SELECT COUNT(*) FROM MyEntityWithGeneratedId", sql);
        }
    }

    [TestClass]
    public class QueryGeneratorTestsForDefaultEntity
    {
        IQueryGenerator generator = Query<MyEntity>.Create(MappingConvention.UnderScores);

        [TestMethod]
        public void Insert()
        {
            var sql = generator.Insert;
            Assert.AreEqual("INSERT INTO my_entity (my_entity_id, name, description) VALUES (@MyEntityId, @Name, @Description)", sql);
        }
        [TestMethod]
        public void Delete()
        {
            var sql = generator.Delete;
            Assert.AreEqual("DELETE FROM my_entity WHERE my_entity_id = @MyEntityId", sql);
        }
        [TestMethod]
        public void Update()
        {
            var sql = generator.Update;
            Assert.AreEqual("UPDATE my_entity SET name = @Name, description = @Description WHERE my_entity_id = @MyEntityId", sql);
        }
        [TestMethod]
        public void Select()
        {
            var sql = generator.Select;
            Assert.AreEqual("SELECT my_entity_id, name, description FROM my_entity WHERE my_entity_id = @MyEntityId", sql);
        }
        [TestMethod]
        public void SelectAll()
        {
            var sql = generator.SelectAll;
            Assert.AreEqual("SELECT my_entity_id, name, description FROM my_entity", sql);
        }
        [TestMethod]
        public void Count()
        {
            var sql = generator.Count;
            Assert.AreEqual("SELECT COUNT(*) FROM my_entity", sql);
        }
    }
    [TestClass]
    public class QueryGeneratorTestsForEntityWithCompositeKey
    {
        IQueryGenerator generator = Query<MyEntityWithCompositeKey>.Create("Oracle");

        [TestMethod]
        public void Insert()
        {
            var sql = generator.Insert;
            Assert.AreEqual("INSERT INTO MY_ENTITY_WITH_COMPOSITE_KEY (PARENT, NAME, DESCRIPTION) VALUES (:Parent, :Name, :Description)", sql);
        }
        [TestMethod]
        public void Delete()
        {
            var sql = generator.Delete;
            Assert.AreEqual("DELETE FROM MY_ENTITY_WITH_COMPOSITE_KEY WHERE PARENT = :Parent AND NAME = :Name", sql);
        }
        [TestMethod]
        public void Update()
        {
            var sql = generator.Update;
            Assert.AreEqual("UPDATE MY_ENTITY_WITH_COMPOSITE_KEY SET DESCRIPTION = :Description WHERE PARENT = :Parent AND NAME = :Name", sql);
        }
        [TestMethod]
        public void Select()
        {
            var sql = generator.Select;
            Assert.AreEqual("SELECT PARENT, NAME, DESCRIPTION FROM MY_ENTITY_WITH_COMPOSITE_KEY WHERE PARENT = :Parent AND NAME = :Name", sql);
        }
        [TestMethod]
        public void SelectAll()
        {
            var sql = generator.SelectAll;
            Assert.AreEqual("SELECT PARENT, NAME, DESCRIPTION FROM MY_ENTITY_WITH_COMPOSITE_KEY", sql);
        }
        [TestMethod]
        public void Count()
        {
            var sql = generator.Count;
            Assert.AreEqual("SELECT COUNT(*) FROM MY_ENTITY_WITH_COMPOSITE_KEY", sql);
        }
    }
}
