using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerminalMonitoringService.ProcessMonitoring
{
    public class ProcessMonitoringLogic
    {
        private readonly NLog.Logger _logger;
        Process? ProcessToCheck;

        public ProcessMonitoringLogic(string processName)
        {
            var errorMessage = "";
            try
            {
                ProcessToCheck = Process.GetProcessesByName(processName);
                _logger = NLog.LogManager.GetLogger($"Resources_{processName}_PID_{ProcessToCheck.Id}");
            }
            catch (Exception ex)
            {
                ProcessToCheck = null;
            }
    
            _logger.Info($"Start monitoring resources for process: {processName}");
        }

        public ProcessMonitoringLogic(int processID, NLog.Logger logger)
        {

        }
    }
}
