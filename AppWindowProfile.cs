namespace MonitorLauncher
{
    public class AppWindowProfile
    {
        public string DisplayName { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string MonitorDeviceName { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsMaximized { get; set; }
        public bool LaunchIfNotRunning { get; set; } = true;
    }
}
