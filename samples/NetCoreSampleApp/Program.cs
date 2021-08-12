/*
 * This sample shows how the Db class can be configured with DI in a standard .Net Core
 * application that is based on the generic host.
 * 
 * The IDb service is added as a scoped service. The connection string is read from an 
 * appSettings.json file and the DbProviderFactory instance is passed in directly.
 * 
 */

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Net.Code.ADONet;
using System.Data.SQLite;
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
