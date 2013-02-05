using System;
using System.Configuration;
using System.Data;
using Moq;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit.DbTests
{
    [TestFixture]
    public class DbConstructorTests
    {

        [Test]
        public void GivenDbWithExternalConnection_WhenDisposed_ConnectionIsNotDisposed()
        {
            var fakeConnection = new Mock<IDbConnection>();

            fakeConnection
                .Setup(c => c.Dispose())
                .Throws(new Exception("External connection should not be disposed with the Db"));

            var db = new Db(fakeConnection.Object);

            db.Dispose();
        }

        [Test]
        public void GivenDbWithConnectionString_WhenDisposed_ConnectionIsDisposed()
        {
            var fakeConnection = new Mock<IDbConnection>();
            fakeConnection.Setup(c => c.Dispose()).Verifiable();

            var fakeFactory = new Mock<IConnectionFactory>();
            fakeFactory.Setup(p => p.CreateConnection("")).Returns(fakeConnection.Object);

            using (var db = new Db("", fakeFactory.Object))
            {
                // ensure the connection is actually instantiated
                // ReSharper disable UnusedVariable
                var connection = db.Connection;
                // ReSharper restore UnusedVariable
            }

            fakeConnection.VerifyAll();
        }

        [Test]
        public void FromConfig_ReturnsDbWithFirstConfigurationSetting()
        {
            var db = Db.FromConfig();
            Assert.AreEqual(ConfigurationManager.ConnectionStrings["firstConnectionString"].ConnectionString, db.ConnectionString);
        }
        [Test]
        public void FromConfig_ReturnsDbWithNamedConfigurationSetting()
        {
            var db = Db.FromConfig("secondConnectionString");
            Assert.AreEqual(ConfigurationManager.ConnectionStrings["secondConnectionString"].ConnectionString, db.ConnectionString);
        }
    }
}
