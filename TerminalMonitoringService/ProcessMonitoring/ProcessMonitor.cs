using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerminalMonitoringService.ProcessMonitoring
{
    public class ProcessMonitor
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        public ProcessMonitor (string ProcessName)
        {
            _logger.Info($"Start monitoring process: {ProcessName}");
        }

    }
}
