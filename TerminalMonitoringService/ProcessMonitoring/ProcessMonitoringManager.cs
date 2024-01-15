using System.Diagnostics;
using System.Timers;
using Timer = System.Timers.Timer;

namespace TerminalMonitoringService.ProcessMonitoring
{
    internal class ProcessMonitoringManager
    {
        private List<ProcessMonitoringLogic> ListOfProcessesToMonitoring { get; set; }
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private List<string> NamesOfAllProcessesToMonitoring;
        private double _checkingIntervalInMs { get; set; } = 5000;
        private double _internalCheckingIntervalInMs { get; set; } = 5000;
        private Timer InternalTimerToManageExistingProcesses = new Timer();

        public ProcessMonitoringManager(List<string> namesOfAllProcessesToMonitoring, double CheckingIntervalInMs, double InternalCheckingIntervalInMs )
        {
            NamesOfAllProcessesToMonitoring = namesOfAllProcessesToMonitoring;
            ListOfProcessesToMonitoring = new List<ProcessMonitoringLogic>();
            _checkingIntervalInMs = CheckingIntervalInMs;
            _internalCheckingIntervalInMs = InternalCheckingIntervalInMs;

            foreach (var processName in NamesOfAllProcessesToMonitoring)
            {
                try
                {
                    var processesByName = Process.GetProcessesByName(processName);
                    foreach (var process in processesByName)
                    {
                        var loggerName = NLog.LogManager.GetLogger($"Resources_{process.ProcessName}_PID_{process.Id}");
                        ListOfProcessesToMonitoring.Add(new ProcessMonitoringLogic(process, loggerName, _checkingIntervalInMs));
                    }

                    InternalTimerToManageExistingProcesses.Elapsed += new ElapsedEventHandler(UpdateMonitoringList);
                    InternalTimerToManageExistingProcesses.Interval = _internalCheckingIntervalInMs;
                    InternalTimerToManageExistingProcesses.Start();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Cannot add process {processName}");
                }
            }
        }

        private void UpdateMonitoringList(object sender, EventArgs e)
        {
            UpdateProcessList();
        }

        private void SearchForInactiveProcesses()
        {
            ListOfProcessesToMonitoring.RemoveAll(processToCheck => processToCheck.ProcessToCheck is null);
        }

        private void AddNewProcessesToList()
        {
            var runningProcesses = Process.GetProcesses();

            foreach (var processName in NamesOfAllProcessesToMonitoring)
            {
                var matchingProcesses = runningProcesses.Where(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));

                foreach (var process in matchingProcesses)
                {
                    if (!ListOfProcessesToMonitoring.Any(pml => pml.ProcessToCheck?.Id == process.Id))
                    {
                        var loggerName = NLog.LogManager.GetLogger($"Resources_{process.ProcessName}_PID_{process.Id}");
                        ListOfProcessesToMonitoring.Add(new ProcessMonitoringLogic(process, loggerName, _checkingIntervalInMs));
                    }
                }
            }
        }

        private void UpdateProcessList()
        {
            SearchForInactiveProcesses();
            AddNewProcessesToList();
        }
    }
}