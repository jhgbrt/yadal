using System.Configuration;
using System.Data;
using NSubstitute;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit.DbTests
{
    [TestFixture]
    public class DbConstructorTests
    {

        [Test]
        public void GivenDbWithExternalConnection_WhenDisposed_ConnectionIsNotDisposed()
        {
            var fakeConnection = Substitute.For<IDbConnection>();

            var db = new Db(fakeConnection);
            db.Dispose();

            // External connection should not be disposed with the Db
            fakeConnection.DidNotReceive().Dispose();
        }

        [Test]
        public void GivenDbWithConnectionString_WhenDisposed_ConnectionIsDisposed()
        {
            var fakeConnection = Substitute.For<IDbConnection>();

            var fakeFactory = Substitute.For<IConnectionFactory>();

            fakeFactory.CreateConnection("").Returns(fakeConnection);

            using (var db = new Db("", fakeFactory))
            {
                // ensure the connection is actually instantiated
                // ReSharper disable UnusedVariable
                var connection = db.Connection;
                // ReSharper restore UnusedVariable
            }

            fakeConnection.Received().Dispose();
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
