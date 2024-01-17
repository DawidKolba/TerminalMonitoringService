using System.Diagnostics;

namespace TerminalMonitoringService.ProcessMonitoring
{
    class ProcessInfo
    {
        public Process Process { get; set; }
        public double CpuUsage { get; set; }
        public long MemoryUsage { get; set; }
        public int HandleCount { get; set; }
    }
}
