using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace TerminalMonitoringService.ProcessMonitoring
{
    internal class SystemUsageInfo : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll")]
        public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private Dictionary<int, (DateTime lastCheck, TimeSpan lastTotalProcessorTime)> cpuUsageInfo = new Dictionary<int, (DateTime, TimeSpan)>();

        private static IContainer components = null;
        private static SafeHandle resource;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                if (resource != null) resource.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        public async Task GetSystemUsageInfo(int topProcessesCount)
        {
            try
            {
                await Task.Run(async () =>
                {
                    await Task.Run(() =>
                    {
                        _logger.Info($"TOTAL SYSTEM USAGE: {_getSystemResourceUsage().Result}");
                    });

                    var allProcesses = Process.GetProcesses();
                    var topMemoryProcesses = allProcesses.OrderByDescending(p => p.WorkingSet64).Take(topProcessesCount);
                    var cpuUsageTasks = allProcesses.Select(process => _getCpuUsage(process)).ToList();

                    await Task.WhenAll(cpuUsageTasks);

                    var cpuUsages = cpuUsageTasks.Select(task => task.Result).ToList();

                    var topCpuProcesses = allProcesses
                        .Select((process, index) => new { Process = process, CpuUsage = cpuUsages[index] })
                        .OrderByDescending(x => x.CpuUsage)
                        .Take(topProcessesCount)
                        .Select(x => x.Process);
                    var topHandleProcesses = allProcesses.OrderByDescending(p => p.HandleCount).Take(topProcessesCount);
                    var builder = new StringBuilder();

                    builder.AppendLine("Top Processes by Memory Usage:");

                    foreach (var process in topMemoryProcesses)
                    {
                        builder.AppendLine(GetProcessUsageInfo(process).Result);
                    }

                    builder.AppendLine("\nTop Processes by CPU Usage:");
                    foreach (var process in topCpuProcesses)
                    {
                        try
                        {
                            builder.AppendLine(GetProcessUsageInfo(process).Result);
                        }
                        catch (AggregateException)
                        {
                            _logger.Warn($"Cannot get info for: {process}");
                        }
                    }

                    builder.AppendLine("\nTop Processes by Handle Count:");
                    foreach (var process in topHandleProcesses)
                    {
                        builder.AppendLine(GetProcessUsageInfo(process).Result);
                    }
                    _logger.Info(builder);
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        public async Task<string> GetProcessUsageInfo(Process process)
        {
            double workingMemoryMb = Math.Round(process.WorkingSet64 / 1024d / 1024d, 2);
            string workingMemoryMbFormatted = workingMemoryMb.ToString("N2").Replace('.', ',');
            var cpuUsageTask = _getCpuUsage(process);
            cpuUsageTask.Wait();

            var cpuUsage = cpuUsageTask.Result;
            string cpuUsageString = cpuUsage == 105.0 ? "Access Denied"
                                : cpuUsage == 101.0 ? "Error"
                                : cpuUsage.ToString("N2");

            int nameColumnWidth = 45; // Długość dla nazwy procesu
            int cpuColumnWidth = 15; // Długość dla użycia CPU
            int memoryColumnWidth = 15; // Długość dla pamięci roboczej
            int handleColumnWidth = 10; // Długość dla liczby uchwytów

            string processNamePadded = process.ProcessName.PadRight(nameColumnWidth);
            string cpuUsagePadded = cpuUsageString.PadRight(cpuColumnWidth);
            string workingMemoryPadded = workingMemoryMbFormatted.PadRight(memoryColumnWidth);
            string handleCountPadded = process.HandleCount.ToString().PadRight(handleColumnWidth);

            return $"Process Name:;{processNamePadded}; " +
                   $"CPU:;\t{cpuUsagePadded};%;\t" +
                   $"Working Memory:;\t{workingMemoryPadded};MB;\t" +
                   $"Handle Count:;\t{handleCountPadded};";

        }

        private async Task<string> _getSystemResourceUsage()
        {
            // CPU usage
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue();
            Thread.Sleep(1000);
            var cpuUsage = cpuCounter.NextValue();

            // RAM usage
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            GlobalMemoryStatusEx(ref memStatus);

            ulong totalMemoryMb = memStatus.ullTotalPhys / (1024 * 1024);
            ulong usedMemoryMb = totalMemoryMb - memStatus.ullAvailPhys / (1024 * 1024);

            // Disk usage
            var diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
            var diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            diskWriteCounter.NextValue();
            diskReadCounter.NextValue();
            Thread.Sleep(1000);
            var diskWriteUsage = diskWriteCounter.NextValue() / (1024 * 1024); // Convert to MB/s
            var diskReadUsage = diskReadCounter.NextValue() / (1024 * 1024); // Convert to MB/s

            return $"CPU Usage: {cpuUsage}% " +
                   $"RAM Usage: {usedMemoryMb} MB / {totalMemoryMb} MB " +
                   $"Disk Usage: write: {diskWriteUsage:N2} MB/s read: {diskReadUsage:N2} MB/s";

        }

        private async Task<double> _getCpuUsage(Process process)
        {
            try
            {
                double cpuUsage = 0.0;

                if (cpuUsageInfo.TryGetValue(process.Id, out var lastCheck))
                {
                    var newCheckTime = DateTime.UtcNow;
                    var newTotalProcessorTime = process.TotalProcessorTime;
                    var oldCheckTime = lastCheck.lastCheck;
                    var oldTotalProcessorTime = lastCheck.lastTotalProcessorTime;

                    double totalUsedTime = (newTotalProcessorTime - oldTotalProcessorTime).TotalMilliseconds;
                    double timeInterval = (newCheckTime - oldCheckTime).TotalMilliseconds;

                    cpuUsage = totalUsedTime / timeInterval / Environment.ProcessorCount * 100;
                }
                else
                {
                    cpuUsageInfo[process.Id] = (DateTime.UtcNow, process.TotalProcessorTime);
                    await Task.Delay(500);
                    return await _getCpuUsage(process);
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
