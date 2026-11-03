using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

using Xunit;

namespace Net.Code.ADONet.Tests.Unit.Extensions
{
    public class EnumerableDataReaderChunkedReadTests
    {
        class Item
        {
            public byte[] Bytes { get; set; }
            public char[] Chars { get; set; }
            public string Text { get; set; }
        }

        private static DbDataReader ReaderFor(Item item)
        {
            var reader = new[] { item }.AsDataReader();
            Assert.True(reader.Read());
            return reader;
        }

        private static byte[] ReadAllBytes(DbDataReader reader, int ordinal, int chunkSize)
        {
            var result = new List<byte>();
            var buffer = new byte[chunkSize];
            long offset = 0;
            long read;
            while ((read = reader.GetBytes(ordinal, offset, buffer, 0, chunkSize)) > 0)
            {
                result.AddRange(buffer.Take((int)read));
                offset += read;
            }
            return result.ToArray();
        }

        [Theory]
        [InlineData(10, 4)]
        [InlineData(12, 4)]
        [InlineData(3, 4)]
        [InlineData(1, 1)]
        public void GetBytes_ReadInChunks_ReassemblesFullArray(int size, int chunkSize)
        {
            var data = Enumerable.Range(1, size).Select(i => (byte)i).ToArray();
            var reader = ReaderFor(new Item { Bytes = data });

            var result = ReadAllBytes(reader, reader.GetOrdinal("Bytes"), chunkSize);

            Assert.Equal(data, result);
        }

        [Fact]
        public void GetBytes_LastPartialChunk_ReturnsRemainingCount()
        {
            var data = Enumerable.Range(1, 10).Select(i => (byte)i).ToArray();
            var reader = ReaderFor(new Item { Bytes = data });
            var buffer = new byte[4];

            var read = reader.GetBytes(reader.GetOrdinal("Bytes"), 8, buffer, 0, 4);

            Assert.Equal(2, read);
            Assert.Equal(new byte[] { 9, 10 }, buffer.Take(2).ToArray());
        }

        [Theory]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(100)]
        public void GetBytes_DataOffsetAtOrPastEnd_ReturnsZero(long dataOffset)
        {
            var reader = ReaderFor(new Item { Bytes = new byte[10] });

            var read = reader.GetBytes(reader.GetOrdinal("Bytes"), dataOffset, new byte[4], 0, 4);

            Assert.Equal(0, read);
        }

        [Fact]
        public void GetBytes_NullBuffer_ReturnsTotalLength()
        {
            var reader = ReaderFor(new Item { Bytes = new byte[10] });

            var length = reader.GetBytes(reader.GetOrdinal("Bytes"), 0, null, 0, 0);

            Assert.Equal(10, length);
        }

        [Fact]
        public void GetChars_NullBuffer_ReturnsTotalLength()
        {
            var reader = ReaderFor(new Item { Text = "hello" });

            var length = reader.GetChars(reader.GetOrdinal("Text"), 0, null, 0, 0);

            Assert.Equal(5, length);
        }

        [Fact]
        public void GetBytes_NonZeroBufferOffset_CopiesIntoBufferAtOffset()
        {
            var data = new byte[] { 1, 2, 3, 4, 5 };
            var reader = ReaderFor(new Item { Bytes = data });
            var buffer = new byte[6];

            var read = reader.GetBytes(reader.GetOrdinal("Bytes"), 1, buffer, 2, 10);

            // limited by available buffer space (6 - 2 = 4)
            Assert.Equal(4, read);
            Assert.Equal(new byte[] { 0, 0, 2, 3, 4, 5 }, buffer);
        }

        [Fact]
        public void GetChars_StringProperty_ReadsCharsOfString()
        {
            var reader = ReaderFor(new Item { Text = "0123456789" });
            var buffer = new char[4];

            var read = reader.GetChars(reader.GetOrdinal("Text"), 8, buffer, 0, 4);

            Assert.Equal(2, read);
            Assert.Equal("89", new string(buffer, 0, 2));
        }

        [Fact]
        public void GetChars_StringProperty_ChunkedReadReassemblesString()
        {
            var reader = ReaderFor(new Item { Text = "The quick brown fox" });
            var ordinal = reader.GetOrdinal("Text");
            var buffer = new char[4];
            var result = new StringBuilder();
            long offset = 0;
            long read;
            while ((read = reader.GetChars(ordinal, offset, buffer, 0, buffer.Length)) > 0)
            {
                result.Append(buffer, 0, (int)read);
                offset += read;
            }

            Assert.Equal("The quick brown fox", result.ToString());
        }

        [Fact]
        public void GetChars_CharArrayProperty_ReadsChunk()
        {
            var reader = ReaderFor(new Item { Chars = "0123456789".ToCharArray() });
            var buffer = new char[4];

            var read = reader.GetChars(reader.GetOrdinal("Chars"), 7, buffer, 1, 3);

            Assert.Equal(3, read);
            Assert.Equal(new[] { '\0', '7', '8', '9' }, buffer);
        }

        [Fact]
        public void GetBytes_NullValue_ReturnsZero()
        {
            var reader = ReaderFor(new Item { Bytes = null });
            var ordinal = reader.GetOrdinal("Bytes");

            Assert.True(reader.IsDBNull(ordinal));
            Assert.Equal(0, reader.GetBytes(ordinal, 0, new byte[4], 0, 4));
            Assert.Equal(0, reader.GetBytes(ordinal, 0, null, 0, 0));
        }

        [Fact]
        public void GetChars_NullValue_ReturnsZero()
        {
            var reader = ReaderFor(new Item { Text = null, Chars = null });

            Assert.Equal(0, reader.GetChars(reader.GetOrdinal("Text"), 0, new char[4], 0, 4));
            Assert.Equal(0, reader.GetChars(reader.GetOrdinal("Chars"), 0, new char[4], 0, 4));
        }
    }
}
