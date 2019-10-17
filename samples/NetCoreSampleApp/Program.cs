using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using Net.Code.ADONet;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;

#pragma warning disable

namespace NetCoreSampleApp
{

    /*
     * This sample shows how the Db class can be configured with DI in a standard .Net Core
     * application that is based on the generic host.
     * 
     * The IDb service is added as a scoped service. The connection string is read from an 
     * appSettings.json file and the DbProviderFactory instance is passed in directly.
     * 
     */

    class Program
    {
        class DbSettings
        {
            public string ConnectionString { get; set; }
        }
        static void Main(string[] args)
        {
            Logger.Log = Console.WriteLine;
            var host = new HostBuilder()
                .ConfigureAppConfiguration((c, conf) =>
                {
                    conf
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appSettings.json")
                        .AddInMemoryCollection();
                })
                .ConfigureServices((context, services) =>
                {
                    services
                        .AddScoped<IDb, Db>(
                            serviceProvider => new Db(
                                context.Configuration.GetSection("Db").Get<DbSettings>().ConnectionString, 
                                SQLiteFactory.Instance)
                            );
                    services.AddTransient<SomeService>();
                    services.AddTransient<SomeDependency>();
                    services.AddHostedService<MyHostedService>();
                }).Build();
            host.Run();
        }
    }

   public class SomeDependency
    {
        IDb _db;
        public SomeDependency(IDb db)
        {
            Console.WriteLine("SomeDependency - ctor");
            _db = db;
            Console.WriteLine(db.Connection.State);
        }
    }

    class SomeService : IDisposable
    {
        IDb _db;

        public SomeService(IDb db, SomeDependency dependency)
        {
            Console.WriteLine("SomeService - ctor");
            _db = db;
            _db.Connect();
        }
        public void Dispose()
        {
            Console.WriteLine("SomeService - dispose");
        }
    }

    class MyHostedService : IHostedService
    {
        private Timer _timer;
        private readonly IServiceProvider services;

        public MyHostedService(IServiceProvider services)
        {
            Console.WriteLine("hosted svc ctor");

            this.services = services;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Starting");
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            using (var scope = services.CreateScope())
            {
                var svc = scope.ServiceProvider.GetRequiredService<SomeService>();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Stopping");
            return Task.CompletedTask;
        }
    }



}
