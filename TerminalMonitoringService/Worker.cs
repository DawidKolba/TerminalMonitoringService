using ProcessMonitoringService.Serialization;
using ProcessMonitoringService;
using TerminalMonitoringService.ProcessMonitoring;
using System.Diagnostics;

namespace TerminalMonitoringService
{
    public class Worker : BackgroundService
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private List<ProcessMonitor> _processList = new List<ProcessMonitor>();

        //public Worker(ILogger<Worker> logger)
        //{
        //    _logger = logger;
        //}

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // var pm = new ProcessMonitor("test");
            _processList = GetProcessesToChecking();

            while (!stoppingToken.IsCancellationRequested)
            {
                // _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                _logger.Info("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000);
                using (var systemInfo = new SystemUsageInfo())
                {
                    await systemInfo.GetSystemUsageInfo(10);
                }
            }
        }





        private List<ProcessMonitor> GetProcessesToChecking()
        {
            string filePath = "ProcessList.xml";
            string xmlContent = File.ReadAllText(filePath);
            Serializer serializer = new Serializer();
            ProcessToCheck processList = serializer.Deserialize<ProcessToCheck>(xmlContent);
            List<ProcessMonitor> processes = new List<ProcessMonitor>();
            foreach (string processName in processList.ProcessName)
            {
                processes.Add(new ProcessMonitor(processName));
            }

            return processes;
        }
    }
}