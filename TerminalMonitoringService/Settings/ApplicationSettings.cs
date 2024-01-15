namespace TerminalMonitoringService.Settings
{
    public class ApplicationSettings
    {
        public double SystemResourcesIntervalMs { get; set; }
        public double ProcessCheckIntervalMs { get; set; }
        public double InternalCheckIntervalMs { get; set; }
        public int TopProcessesCount { get; set; }
    }
}
