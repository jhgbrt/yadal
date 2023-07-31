using System;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Net.Code.ADONet;
using Net.Code.ADONet.Tests.Integration;
using Net.Code.ADONet.Tests.Integration.Databases;

using Xunit.Abstractions;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
namespace IntegrationTests
{
    public class DatabaseFixture<T> : IDisposable where T : IDatabaseImpl, new()
    {
        public DatabaseFixture(IMessageSink sink)
        {

            Target = new T();

            var master = $"{Target.Name}Master";
#if DEBUG
            var logger = XUnitLogger.CreateLogger(sink);
#else
            var logger = NullLogger.Instance;
#endif
            var masterDb = CreateDb(logger, master);
            try
            {
                masterDb.Connect();
                IsAvailable = true;

                foreach (var statement in Target.GetDropAndRecreateDdl())
                {
                    masterDb.Execute(statement);
                }

                var db = CreateDb(logger);
                foreach (var statement in Target.GetAfterInitSql())
                {
                    db.Execute(statement);
                }
            }
            catch (Exception e)
            { 
                ConnectionFailureException = e;
                IsAvailable = false;
            }
        }
        public void Dispose()
        {

        }
        public bool IsAvailable { get; private set; }
        public T Target { get; private set; }
        public IDb CreateDb(ILogger logger)
        => CreateDb(logger, Target.Name);
        private IDb CreateDb(ILogger logger, string name)
        => new Db(
            Configuration.ConnectionStrings[name], Target.Config, Target.Factory, logger
            );
        public Exception ConnectionFailureException { get; private set; }
    }
}

