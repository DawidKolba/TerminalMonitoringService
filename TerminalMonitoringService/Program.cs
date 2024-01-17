using TerminalMonitoringService;
using NLog;
using TerminalMonitoringService.Settings;

Logger _logger = NLog.LogManager.GetCurrentClassLogger();
Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
     .ConfigureAppConfiguration((hostingContext, config) =>
     {
         config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
     })
       .ConfigureServices((hostBuilderContext, services) =>
       {
           var configuration = hostBuilderContext.Configuration;
           services.Configure<ApplicationSettings>(configuration.GetSection("ApplicationSettings"));
           services.Configure<ProcessMonitoringSettings>(configuration.GetSection("ProcessMonitoring"));

           services.AddHostedService<Worker>();
    })
    .Build();

_logger.Info("Start application");
await host.RunAsync();
_logger.Info("Application shutdown");
