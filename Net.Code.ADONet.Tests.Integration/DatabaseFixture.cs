using System;
using System.Collections.Generic;

using Microsoft.Data.SqlClient;
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
            try
            {
                var masterDb = CreateDb(logger, master);

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

        IReadOnlyDictionary<string, string>? connectionStrings;
        private IDb CreateDb(ILogger logger, string name)
        {
            if (IsAvailable && connectionStrings is not null)
            {
                var db = new Db(connectionStrings[name], Target.Config, Target.Factory, logger);
                return db;
            }
            else
            {
                var cs = Target.AlternateConnectionStrings ?? Configuration.ConnectionStrings;

                var db = new Db(
                        cs[name], Target.Config, Target.Factory, logger
                        );

                try
                {
                    db.Connect();
                }
                catch when (Target.AlternateConnectionStrings is not null)
                {
                    db.Dispose();
                    cs = Configuration.ConnectionStrings;
                    db = new Db(
                            cs[name], Target.Config, Target.Factory, logger
                            );
                }

                db.Connect();
                connectionStrings = cs;
                IsAvailable = true;
                return db;

            }

        }

        public Exception? ConnectionFailureException { get; private set; }
    }
}

