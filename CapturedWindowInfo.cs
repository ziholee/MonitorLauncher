using System;

namespace MonitorLauncher
{
    public class CapturedWindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string MonitorDeviceName { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsMaximized { get; set; }

        public string DisplayText => $"{Title} - {ProcessName}";

        public AppWindowProfile ToAppWindowProfile()
        {
            return new AppWindowProfile
            {
                DisplayName = string.IsNullOrWhiteSpace(Title) ? ProcessName : Title,
                ProcessName = ProcessName,
                ExecutablePath = ExecutablePath,
                MonitorDeviceName = MonitorDeviceName,
                X = X,
                Y = Y,
                Width = Width,
                Height = Height,
                IsMaximized = IsMaximized,
                LaunchIfNotRunning = true
            };
        }

        public override string ToString()
        {
            string monitor = string.IsNullOrWhiteSpace(MonitorDeviceName) ? "알 수 없는 모니터" : MonitorDeviceName;
            return $"{DisplayText}  [{monitor}, {Width}x{Height}]";
        }
    }
}
