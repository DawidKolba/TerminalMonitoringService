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

        public Worker(IOptions<ApplicationSettings> appSettings, IOptions<ProcessMonitoringSettings> processMonitoringSettings)
        {
            _settings = appSettings.Value;
            _processMonitoringSettings = processMonitoringSettings.Value;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var processList = GetProcessesToChecking();
            PrintMonitoringProcessesToLogs(processList);

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

        private void PrintMonitoringProcessesToLogs(List<string> processList)
        {
            _logger.Info("Dump of processes to monitoring  START ----------------------->");

            foreach (var item in processList)
            {
                _logger.Info($"Process: {item}");
            }

            _logger.Info("<----------------------- END Dump of processes to monitoring");
        }

        private async void GetResourcesTimerElapsedLogicAsync(object sender, EventArgs e)
        {
            using (var systemInfo = new SystemUsageInfo())
            {
                await systemInfo.GetSystemUsageInfo(_settings.TopProcessesCount);
            }
        }

        public List<string> FindExeFilesInDirectory(string directory)
        {
            List<string> exeFiles = new List<string>();
            try
            {
                if (Directory.Exists(directory))
                {
                    var files = Directory.EnumerateFiles(directory, "*.exe", SearchOption.AllDirectories);
                    exeFiles.AddRange(files);
                }
                else
                {
                    _logger.Error($"Directory not found: {directory}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error accessing directory {directory}");
            }
            return exeFiles;
        }


        private List<string> GetProcessesFromConfig()
        {
            var list = new List<string>(_processMonitoringSettings.ProcessesToCheck);

            if (_processMonitoringSettings.DirectoriesToCheck != null &&
                _processMonitoringSettings.DirectoriesToCheck.Count > 0)
            {
                foreach (var directory in _processMonitoringSettings.DirectoriesToCheck)
                {
                    list.AddRange(FindExeFilesInDirectory(directory));
                }
            }

            return list.Distinct().ToList();
        }

        private List<string> GetProcessesToChecking()
        {
            var list = new List<string>();
            foreach (var process in GetProcessesFromConfig())
            {
                var fileName = Path.GetFileNameWithoutExtension(process);
                if (!list.Contains(fileName))
                {
                    list.Add(fileName);
                }
            }
            return list;
        }

    }
}