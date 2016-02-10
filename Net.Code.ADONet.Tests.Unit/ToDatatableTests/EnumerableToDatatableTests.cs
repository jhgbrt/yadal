using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Code.ADONet.Tests.Unit.ToDatatableTests
{
    [TestClass]
    public class EnumerableToDatatableTests
    {
        private static readonly DataTable Datatable = new[] 
        {
            new Product {Id = 1, Name = "P1", Quantity = 10},
            new Product {Id = 2, Name = "P2", Quantity = null}
        }.ToDataTable();


        [TestMethod]
        public void result_contains_two_rows()
        {
            Assert.AreEqual(2, Datatable.Rows.Count);
        }
        [TestMethod]
        public void result_has_3_columns()
        {
            Assert.AreEqual(3, Datatable.Columns.Count);
        }
        [TestMethod]
        public void result_has_correct_name()
        {
            Assert.AreEqual("Product", Datatable.TableName);
        }
        [TestMethod]
        public void First_row_has_correct_values()
        {
            Assert.AreEqual(1, Datatable.Rows[0]["Id"]);
            Assert.AreEqual("P1", Datatable.Rows[0]["Name"]);
            Assert.AreEqual(10, Datatable.Rows[0]["Quantity"]);
        }
        [TestMethod]
        public void Second_row_has_correct_values()
        {
            Assert.AreEqual(2, Datatable.Rows[1]["Id"]);
            Assert.AreEqual("P2", Datatable.Rows[1]["Name"]);
            Assert.AreEqual(DBNull.Value, Datatable.Rows[1]["Quantity"]);
        }
    }

    internal class Product
    {
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        public int Id { get; set; }
        public string Name { get; set; }
        public int? Quantity { get; set; }
        // ReSharper restore UnusedAutoPropertyAccessor.Global
    }
}
