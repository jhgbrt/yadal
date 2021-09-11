using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using System;

#pragma warning disable

namespace NetCoreSampleApp;

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
