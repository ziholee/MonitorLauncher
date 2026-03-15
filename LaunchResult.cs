namespace MonitorLauncher
{
    public class LaunchResult
    {
        public bool Succeeded { get; set; }
        public bool WindowMoved { get; set; }
        public bool FileMissing { get; set; }
        public bool MonitorMissing { get; set; }
        public bool UsedMonitorFallback { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
    }
}
