using System;
using System.Collections.Generic;
using System.Data;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit.ToDatatableTests
{
    [TestFixture]
    public class EnumerableToDatatableTests
    {
        private IEnumerable<Product> products = new[]
                                                    {
                                                        new Product {Id = 1, Name = "P1", Quantity = 10},
                                                        new Product {Id = 2, Name = "P2", Quantity = null}
                                                    };

        private DataTable _datatable;

        [TestFixtureSetUp]
        public void ConvertToDataTable()
        {
            _datatable = products.ToDataTable();
        }

        [Test]
        public void result_contains_two_rows()
        {
            Assert.AreEqual(2, _datatable.Rows.Count);
        }
        [Test]
        public void result_has_3_columns()
        {
            Assert.AreEqual(3, _datatable.Columns.Count);
        }
        [Test]
        public void result_has_correct_name()
        {
            Assert.AreEqual("Product", _datatable.TableName);
        }
        [Test]
        public void First_row_has_correct_values()
        {
            Assert.AreEqual(1, _datatable.Rows[0]["Id"]);
            Assert.AreEqual("P1", _datatable.Rows[0]["Name"]);
            Assert.AreEqual(10, _datatable.Rows[0]["Quantity"]);
        }
        [Test]
        public void Second_row_has_correct_values()
        {
            Assert.AreEqual(2, _datatable.Rows[1]["Id"]);
            Assert.AreEqual("P2", _datatable.Rows[1]["Name"]);
            Assert.AreEqual(DBNull.Value, _datatable.Rows[1]["Quantity"]);
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
