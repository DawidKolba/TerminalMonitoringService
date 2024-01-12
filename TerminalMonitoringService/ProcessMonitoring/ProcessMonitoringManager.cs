using ProcessMonitoringService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerminalMonitoringService.ProcessMonitoring
{
    internal class ProcessMonitoringManager
    {
        private List<ProcessMonitoringLogic> _listOfProcesesToMonitoring { get; set; }

        public ProcessMonitoringManager(string processName)
        {
            _listOfProcesesToMonitoring = ListOfProcesesToMonitoring;
          var  ProcessToCheck = Process.GetProcessesByName(processName);

        }
       
    }
}
