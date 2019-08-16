using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using Net.Code.ADONet;
using System.Data.SqlClient;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace NetCoreSampleApp
{
    class SomeService : IDisposable
    {

        public SomeService()
        {
            Console.WriteLine("SomeService - ctor");
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

    class Program
    {
        
        static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration((c, conf) => conf.AddInMemoryCollection())
                .ConfigureServices((c, s) =>
                {
                    s.AddScoped<IDb, Db>();
                    s.AddSingleton<DbProviderFactory>(SqlClientFactory.Instance);
                    s.AddHostedService<MyHostedService>();
                }).Build();
            host.Run();
        }
    }

    
}
