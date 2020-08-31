using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using Net.Code.ADONet;
using System.Data.SQLite;
using System.IO;
using NetCoreSampleApp;

#pragma warning disable

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
                )
            .AddTransient<SomeService>()
            .AddTransient<SomeDependency>()
            .AddHostedService<MyHostedService>();
    }).Build();

host.Run();
