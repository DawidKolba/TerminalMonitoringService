using TerminalMonitoringService;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
