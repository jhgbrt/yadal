using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xunit;

namespace Net.Code.ADONet.Tests.Unit.Extensions
{
    public class EnumerableDataReaderTests
    {
        class SomeClass
        {
            public int Id { get; set; }
            public char[] CharArray { get; set; }
            public bool Bool { get; set; }
        }

        SomeClass[] Items = Enumerable.Range(2, 10).Select(i => new SomeClass
        {
            Id = i,
            CharArray = "0123456789".ToCharArray(),
            Bool = true,
        }).ToArray();

        [Fact]
        public void AsDataReader()
        {
            var reader = Items.AsDataReader();

            reader.Read();

            var id = reader.Get<int>("Id");
            Assert.Equal(2, id);

            var b = reader.Get<bool>("Bool");
            Assert.True(b);

            var bytes = reader.Get<char[]>("CharArray");
            Assert.Equal("0123456789", new string(bytes));

            char[] buffer = new char[5];
            reader.GetChars(reader.GetOrdinal("CharArray"), 2, buffer, 0, 5);

            Assert.Equal("23456", new string(buffer));
            
        }
    }
}
