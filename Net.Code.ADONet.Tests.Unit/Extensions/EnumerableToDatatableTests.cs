using System;
using System.Data;
using Xunit;

namespace Net.Code.ADONet.Tests.Unit.Extensions
{
    public class EnumerableToDatatableTests
    {
        private static readonly DataTable Datatable = new[] 
        {
            new Product {Id = 1, Name = "P1", Quantity = 10},
            new Product {Id = 2, Name = "P2", Quantity = null}
        }.ToDataTable();


        [Fact]
        public void result_contains_two_rows()
        {
            Assert.Equal(2, Datatable.Rows.Count);
        }
        [Fact]
        public void result_has_3_columns()
        {
            Assert.Equal(3, Datatable.Columns.Count);
        }
        [Fact]
        public void result_has_correct_name()
        {
            Assert.Equal("Product", Datatable.TableName);
        }
        [Fact]
        public void First_row_has_correct_values()
        {
            Assert.Equal(1, Datatable.Rows[0]["Id"]);
            Assert.Equal("P1", Datatable.Rows[0]["Name"]);
            Assert.Equal(10, Datatable.Rows[0]["Quantity"]);
        }
        [Fact]
        public void Second_row_has_correct_values()
        {
            Assert.Equal(2, Datatable.Rows[1]["Id"]);
            Assert.Equal("P2", Datatable.Rows[1]["Name"]);
            Assert.Equal(DBNull.Value, Datatable.Rows[1]["Quantity"]);
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
