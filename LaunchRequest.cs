namespace MonitorLauncher
{
    public class LaunchRequest
    {
        public string ExecutablePath { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string MonitorDeviceName { get; set; } = string.Empty;
        public AppWindowState WindowState { get; set; } = AppWindowState.Maximized;
        public string? ProfileName { get; set; }
    }
}
