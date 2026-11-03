using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Net.Code.ADONet.Tests.Unit.Extensions.Experimental
{
    public class QueryFactoryToKeyNonPrimitiveTests
    {
        record Product([property: Key] string Code, string Name);
        record Document(Guid Id, string Title);
        record Price(decimal Id, string Label);
        record Event(DateTime Id, string Label);
        enum Color { Red, Green }
        record Paint(Color Id, string Label);
        record NullableKey(int? Id, string Label);
        class Line
        {
            [Key] public string OrderCode { get; set; }
            [Key] public int LineNumber { get; set; }
            public string Description { get; set; }
        }

        [Fact]
        public void StringKeyValue()
        {
            var key = QueryFactory<Product>.ToKey("ABC");
            Assert.True(key.values.Equals(new[] { ("Code", (object)"ABC") }));
        }

        [Fact]
        public void GuidKeyValue()
        {
            var id = Guid.NewGuid();
            var key = QueryFactory<Document>.ToKey(id);
            Assert.True(key.values.Equals(new[] { ("Id", (object)id) }));
        }

        [Fact]
        public void DecimalKeyValue()
        {
            var key = QueryFactory<Price>.ToKey(12.5m);
            Assert.True(key.values.Equals(new[] { ("Id", (object)12.5m) }));
        }

        [Fact]
        public void DateTimeKeyValue()
        {
            var d = new DateTime(2024, 1, 2);
            var key = QueryFactory<Event>.ToKey(d);
            Assert.True(key.values.Equals(new[] { ("Id", (object)d) }));
        }

        [Fact]
        public void EnumKeyValue()
        {
            var key = QueryFactory<Paint>.ToKey(Color.Green);
            Assert.True(key.values.Equals(new[] { ("Id", (object)Color.Green) }));
        }

        [Fact]
        public void NullableKeyWithUnderlyingValue()
        {
            var key = QueryFactory<NullableKey>.ToKey(5);
            Assert.True(key.values.Equals(new[] { ("Id", (object)5) }));
        }

        [Fact]
        public void StringKeyPropertyValue()
        {
            var key = QueryFactory<Product>.ToKey(new { Code = "ABC" });
            Assert.True(key.values.Equals(new[] { ("Code", (object)"ABC") }));
        }

        [Fact]
        public void EnumKeyPropertyValue()
        {
            var key = QueryFactory<Paint>.ToKey(new { Id = Color.Red });
            Assert.True(key.values.Equals(new[] { ("Id", (object)Color.Red) }));
        }

        [Fact]
        public void CompositeKeyWithStringStillWorks()
        {
            var key = QueryFactory<Line>.ToKey(new { OrderCode = "X", LineNumber = 3 });
            Assert.Equal(2, key.values.Count);
            Assert.Contains(("OrderCode", (object)"X"), key.values);
            Assert.Contains(("LineNumber", (object)3), key.values);
        }

        [Fact]
        public void IncompatibleBareValue_Throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => QueryFactory<Document>.ToKey(5));
            Assert.Contains("Id [Guid]", ex.Message);
            Assert.Contains("Int32", ex.Message);
        }

        [Fact]
        public void BareValueForCompositeKey_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => QueryFactory<Line>.ToKey("X"));
        }

        [Fact]
        public void SelectOneWithStringKey_AddsKeyParameter()
        {
            var command = new FakeCommand(new FakeConnection());
            var sql = QueryFactory<Product>.SELECTONE(MappingConvention.Default);
            var cb = new CommandBuilder(command, DbConfig.Default)
                .WithCommandText(sql)
                .WithKey(QueryFactory<Product>.ToKey("ABC"));

            Assert.Equal("SELECT Code, Name FROM Product WHERE Code = @Code", cb.Command.CommandText);
            var p = (IDbDataParameter)Assert.Single(cb.Command.Parameters.Cast<IDbDataParameter>());
            Assert.Equal("Code", p.ParameterName);
            Assert.Equal("ABC", p.Value);
        }
    }

    public class QueryFactoryConcurrencyTests
    {
        class ConcurrencyEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Fact]
        public void Get_ConcurrentFirstUseWithDistinctConventions_IsThreadSafe()
        {
            const int n = 500;
            var results = new ConcurrentDictionary<int, Query>();
            var conventions = Enumerable.Range(0, n)
                .Select(i =>
                {
                    var prefix = $"p{i}_";
                    return new MappingConvention(s => prefix + s, s => s, '@');
                })
                .ToArray();

            Parallel.For(0, n, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, i =>
            {
                results[i] = QueryFactory<ConcurrencyEntity>.Get(conventions[i]);
            });

            for (int i = 0; i < n; i++)
            {
                Assert.Equal($"SELECT p{i}_Id, p{i}_Name FROM p{i}_ConcurrencyEntity", results[i].SelectAll);
                Assert.Same(results[i], QueryFactory<ConcurrencyEntity>.Get(conventions[i]));
            }
        }
    }
}
