using ProcessMonitoringService.Serialization;
using ProcessMonitoringService;
using TerminalMonitoringService.ProcessMonitoring;
using System.Timers;
using Timer = System.Timers.Timer;
using TerminalMonitoringService.Settings;
using Microsoft.Extensions.Options;

namespace TerminalMonitoringService
{
    public class Worker : BackgroundService
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        Timer GetResourcesTimer = new Timer();
        private readonly ApplicationSettings _settings;
        private readonly ProcessMonitoringSettings _processMonitoringSettings;

        public Worker(IOptions<ApplicationSettings> timerSettings, IOptions<ProcessMonitoringSettings> processMonitoringSettings)
        {
            _settings = timerSettings.Value;
            _processMonitoringSettings = processMonitoringSettings.Value;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
           // var processList = GetProcessesToChecking();
            var processList = _processMonitoringSettings.ProcessesToCheck;
            ProcessMonitoringManager manager = new ProcessMonitoringManager(
                processList, 
                _settings.ProcessCheckIntervalMs, 
                _settings.InternalCheckIntervalMs
                );

            GetResourcesTimer.Elapsed += new ElapsedEventHandler(GetResourcesTimerElapsedLogicAsync);
            GetResourcesTimer.Interval = _settings.SystemResourcesIntervalMs;
            GetResourcesTimer.Start();

            return Task.CompletedTask;
        }

        private async void GetResourcesTimerElapsedLogicAsync(object sender, EventArgs e)
        {
            using (var systemInfo = new SystemUsageInfo())
            {
               await systemInfo.GetSystemUsageInfo(_settings.TopProcessesCount);
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