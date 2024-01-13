using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Timers;

namespace TerminalMonitoringService.ProcessMonitoring
{
    public class ProcessMonitoringLogic : IDisposable
    {
        private readonly NLog.Logger _logger;
        private string _processName;
        private static System.ComponentModel.IContainer _components = null;
        private static SafeHandle _resource;
        public Process? ProcessToCheck { get; set; }
        private System.Timers.Timer _processCheckingTimer = new System.Timers.Timer();
        private Dictionary<int, (DateTime lastCheck, TimeSpan lastTotalProcessorTime)> cpuUsageInfo = new Dictionary<int, (DateTime, TimeSpan)>();

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && (_components != null))
            {
                if (_resource != null) _resource.Dispose();
            }
        }

        public void Dispose()
        {
            _processCheckingTimer.Stop();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public ProcessMonitoringLogic(Process process, NLog.Logger logger, double checkingIntervalInMs)
        {
            _logger = logger;
            try
            {
                ProcessToCheck = process;
                _processName = process.ProcessName;
                _logger.Info($"Start monitoring resources for process: {process.ProcessName}.");

                _processCheckingTimer.Elapsed += new ElapsedEventHandler(ProcessMonitoring);
                _processCheckingTimer.Interval = checkingIntervalInMs;
                _processCheckingTimer.Start();
            }
            catch (Exception ex)
            {
                ProcessToCheck = null;
                _logger.Error(ex);
            }
        }

        public void StopMonitoringProcess()
        {
            _logger.Info($"Stop monitoring resources for process {_processName}");

            Dispose();
        }

        private void ProcessMonitoring(object sender, EventArgs e)
        {
            try
            {
                if (ProcessToCheck is null || ProcessToCheck.HasExited)
                {
                    ProcessToCheck = null;
                    return;
                }
                var cpuUsageTask = _getCpuUsage();
                cpuUsageTask.Wait();

                var cpuUsage = cpuUsageTask.Result;
                string cpuUsageString = cpuUsage == 105.0 ? "Access Denied"
                                    : cpuUsage == 101.0 ? "Error"
                                    : cpuUsage.ToString("N2");

                int nameColumnWidth = 35; // Długość dla nazwy procesu
                int cpuColumnWidth = 5; // Długość dla użycia CPU
                int memoryColumnWidth = 10; // Długość dla pamięci roboczej
                int handleColumnWidth = 5; // Długość dla liczby uchwytów

                double workingMemoryMb = Math.Round(ProcessToCheck.WorkingSet64 / 1024d / 1024d, 2);
                string workingMemoryMbFormatted = workingMemoryMb.ToString("N2").Replace('.', ',');

                string processNamePadded = ProcessToCheck.ProcessName.PadRight(nameColumnWidth);
                string cpuUsagePadded = cpuUsageString.PadRight(cpuColumnWidth);
                string workingMemoryPadded = workingMemoryMbFormatted.PadRight(memoryColumnWidth);
                string handleCountPadded = ProcessToCheck.HandleCount.ToString().PadRight(handleColumnWidth);

                var output = $"Process Name:;{processNamePadded}; " +
                                $"CPU:;\t{cpuUsagePadded};%;\t" +
                                $"Working Memory:;\t{workingMemoryPadded};MB;\t" +
                                $"Handle Count:;\t{handleCountPadded};";
                _logger.Info(output);
            }
            catch (Exception ex)
            {
                _processCheckingTimer.Stop();
                _logger.Error(ex);
                ProcessToCheck = null;
            }
        }

        private async Task<double> _getCpuUsage()
        {
            try
            {
                double cpuUsage = 0.0;

                if (cpuUsageInfo.TryGetValue(ProcessToCheck.Id, out var lastCheck))
                {
                    var newCheckTime = DateTime.UtcNow;
                    var newTotalProcessorTime = ProcessToCheck.TotalProcessorTime;
                    var oldCheckTime = lastCheck.lastCheck;
                    var oldTotalProcessorTime = lastCheck.lastTotalProcessorTime;

                    double totalUsedTime = (newTotalProcessorTime - oldTotalProcessorTime).TotalMilliseconds;
                    double timeInterval = (newCheckTime - oldCheckTime).TotalMilliseconds;

                    cpuUsage = totalUsedTime / timeInterval / Environment.ProcessorCount * 100;
                }
                else
                {
                    cpuUsageInfo[ProcessToCheck.Id] = (DateTime.UtcNow, ProcessToCheck.TotalProcessorTime);
                    await Task.Delay(500);
                    return await _getCpuUsage();
                }

                return Math.Round(cpuUsage, 2);
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 5)  // 5 = Access Denied
            {
                return 105.0;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return 101.0;
            }
        }
    }
}
