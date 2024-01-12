using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerminalMonitoringService.ProcessMonitoring
{
    internal class ProcessMonitoringManager
    {
        private List<ProcessMonitoringLogic> _listOfProcesesToMonitoring { get; set; }

        public ProcessMonitoringManager(List <ProcessMonitoringLogic> ListOfProcesesToMonitoring)
        {
            _listOfProcesesToMonitoring = ListOfProcesesToMonitoring;

        }
    }
}
