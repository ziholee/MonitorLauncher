namespace MonitorLauncher
{
    public class LaunchRequest
    {
        public string ExecutablePath { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string MonitorDeviceName { get; set; } = string.Empty;
        public bool MonitorWasPrimary { get; set; }
        public int MonitorBoundsX { get; set; }
        public int MonitorBoundsY { get; set; }
        public int MonitorBoundsWidth { get; set; }
        public int MonitorBoundsHeight { get; set; }
        public AppWindowState WindowState { get; set; } = AppWindowState.Maximized;
        public string? ProfileName { get; set; }
    }
}
