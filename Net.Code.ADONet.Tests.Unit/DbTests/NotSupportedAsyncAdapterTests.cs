using System;
using System.Data;
using Moq;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit.DbTests
{
    [TestFixture]
    public class NotSupportedAsyncAdapterTests
    {
        private IAsyncAdapter _sut = new NotSupportedAsyncAdapter();

        [ExpectedException(typeof(NotSupportedException))]
        [Test]
        public void ExecuteNonQueryAsync_WhenCalled_ThrowsNotSupportedException()
        {
            _sut.ExecuteNonQueryAsync(new Mock<IDbCommand>().Object);
        }
        [ExpectedException(typeof(NotSupportedException))]
        [Test]
        public void ExecuteScalarAsync_WhenCalled_ThrowsNotSupportedException()
        {
            _sut.ExecuteScalarAsync(new Mock<IDbCommand>().Object);
        }
        [ExpectedException(typeof(NotSupportedException))]
        [Test]
        public void ExecuteReaderAsync_WhenCalled_ThrowsNotSupportedException()
        {
            _sut.ExecuteReaderAsync(new Mock<IDbCommand>().Object);
        }
        [ExpectedException(typeof(NotSupportedException))]
        [Test]
        public void OpenConnectionAsync_WhenCalled_ThrowsNotSupportedException()
        {
            _sut.OpenConnectionAsync(new Mock<IDbConnection>().Object);
        }
    }
}