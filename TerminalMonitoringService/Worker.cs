using ProcessMonitoringService.Serialization;
using ProcessMonitoringService;
using TerminalMonitoringService.ProcessMonitoring;
using System.Diagnostics;

namespace TerminalMonitoringService
{
    public class Worker : BackgroundService
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var processList = GetProcessesToChecking();
            ProcessMonitoringManager manager = new ProcessMonitoringManager(processList);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000);
                using (var systemInfo = new SystemUsageInfo())
                {
                    await systemInfo.GetSystemUsageInfo(10);
                }
            }
        }



        private List<String> GetProcessesToChecking()
        {
            string filePath = "ProcessList.xml";
            string xmlContent = File.ReadAllText(filePath);
            Serializer serializer = new Serializer();
            ProcessToCheck processList = serializer.Deserialize<ProcessToCheck>(xmlContent);
            List<String> processes = new List<String>();
            foreach (string processName in processList.ProcessName)
            {
                processes.Add(processName);
            }

            return processes;
        }
    }
}