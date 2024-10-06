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

        public async Task GetSystemUsageInfo(int topProcessesCount)
        {
            try
            {
                _logger.Info($"TOTAL SYSTEM USAGE: {_getSystemResourceUsage().Result}");

                var allProcesses = Process.GetProcesses();
                var cpuUsageTasks = allProcesses.Select(process => GetCpuUsageWithProcessInfo(process)).ToList();

                var completedTasks = await Task.WhenAll(cpuUsageTasks);
                var processInfos = completedTasks.ToList();

                var topMemoryProcesses = processInfos.OrderByDescending(p => p.MemoryUsage).Take(topProcessesCount);
                var topCpuProcesses = processInfos.OrderByDescending(p => p.CpuUsage).Take(topProcessesCount);
                var topHandleProcesses = processInfos.OrderByDescending(p => p.HandleCount).Take(topProcessesCount);

                var builder = new StringBuilder();

                AppendProcessInfo("Top Processes by Memory Usage:", topMemoryProcesses);
                AppendProcessInfo("\nTop Processes by CPU Usage:", topCpuProcesses);
                AppendProcessInfo("\nTop Processes by Handle Count:", topHandleProcesses);

                _logger.Info(builder);

                void AppendProcessInfo(string title, IEnumerable<ProcessInfo> processes)
                {
                    builder.AppendLine(title);
                    foreach (var info in processes)
                    {
                        try
                        {
                            builder.AppendLine(GetProcessUsageInfo(info.Process));
                        }
                        catch (AggregateException)
                        {
                            _logger.Warn($"Cannot get info for: {info.Process}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        public string GetProcessUsageInfo(Process process)
        {
            double workingMemoryMb = Math.Round(process.WorkingSet64 / 1024d / 1024d, 2);
            string workingMemoryMbFormatted = workingMemoryMb.ToString("N2").Replace('.', ',');
            var cpuUsageTask = _getCpuUsage(process);
            cpuUsageTask.Wait();

            var cpuUsage = cpuUsageTask.Result;
            string cpuUsageString = cpuUsage == 105.0 ? "Access Denied"
                                : cpuUsage == 101.0 ? "Error"
                                : cpuUsage.ToString("N2");

            int nameColumnWidth = 45; // Length for process name
            int cpuColumnWidth = 15; // Length for CPU usage
            int memoryColumnWidth = 15; // Length for working memory
            int handleColumnWidth = 10; // Length for number of handles


            string processNamePadded = process.ProcessName.PadRight(nameColumnWidth);
            string cpuUsagePadded = cpuUsageString.PadRight(cpuColumnWidth);
            string workingMemoryPadded = workingMemoryMbFormatted.PadRight(memoryColumnWidth);
            string handleCountPadded = process.HandleCount.ToString().PadRight(handleColumnWidth);

            return $"Process Name:;{processNamePadded}; " +
                   $"CPU:;\t{cpuUsagePadded};%;\t" +
                   $"Working Memory:;\t{workingMemoryPadded};MB;\t" +
                   $"Handle Count:;\t{handleCountPadded};";

        }       

        private async Task<ProcessInfo> GetCpuUsageWithProcessInfo(Process process)
        {
            try
            {
                var cpuUsage = await _getCpuUsage(process);
                return new ProcessInfo
                {
                    Process = process,
                    CpuUsage = cpuUsage,
                    MemoryUsage = process.WorkingSet64,
                    HandleCount = process.HandleCount
                };
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to get CPU usage for {process.ProcessName}: {ex.Message}");
                return null; 
            }
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                if (resource != null) resource.Dispose();
            }
        }
    }
}
