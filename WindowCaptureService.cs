using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MonitorLauncher
{
    public class WindowCaptureService
    {
        private static readonly HashSet<string> IgnoredProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ApplicationFrameHost",
            "MonitorLauncher",
            "ShellExperienceHost",
            "StartMenuExperienceHost",
            "SystemSettings",
            "TextInputHost"
        };

        public List<CapturedWindowInfo> CaptureOpenWindows()
        {
            var windows = new List<CapturedWindowInfo>();

            Win32Api.EnumWindows((hWnd, lParam) =>
            {
                if (!TryCaptureWindow(hWnd, out var info) || info == null)
                {
                    return true;
                }

                windows.Add(info);
                return true;
            }, IntPtr.Zero);

            return windows
                .OrderBy(window => window.ProcessName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(window => window.Title, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        private static bool TryCaptureWindow(IntPtr hWnd, out CapturedWindowInfo? info)
        {
            info = null;

            if (!IsGeneralVisibleWindow(hWnd))
            {
                return false;
            }

            string title = GetWindowTitle(hWnd);
            if (string.IsNullOrWhiteSpace(title))
            {
                return false;
            }

            Win32Api.GetWindowThreadProcessId(hWnd, out uint processId);
            if (processId == 0)
            {
                return false;
            }

            Process process;
            try
            {
                process = Process.GetProcessById((int)processId);
            }
            catch
            {
                return false;
            }

            using var capturedProcess = process;
            string processName;
            try { processName = process.ProcessName; }
            catch { return false; }
            if (IgnoredProcessNames.Contains(processName))
            {
                return false;
            }

            if (!Win32Api.GetWindowRect(hWnd, out var rect) || rect.Width < 120 || rect.Height < 80)
            {
                return false;
            }

            string executablePath = GetExecutablePath(process);
            var center = new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
            var monitor = Screen.AllScreens.FirstOrDefault(screen => screen.Bounds.Contains(center)) ?? Screen.PrimaryScreen;

            info = new CapturedWindowInfo
            {
                Handle = hWnd,
                Title = title,
                ProcessName = processName,
                ExecutablePath = executablePath,
                MonitorDeviceName = monitor?.DeviceName ?? string.Empty,
                X = rect.Left,
                Y = rect.Top,
                Width = rect.Width,
                Height = rect.Height,
                IsMaximized = IsMaximized(hWnd)
            };
            return true;
        }

        public static bool IsGeneralVisibleWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || !Win32Api.IsWindowVisible(hWnd))
            {
                return false;
            }

            var className = new StringBuilder(256);
            Win32Api.GetClassName(hWnd, className, className.Capacity);
            if (className.ToString() is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd")
                return false;

            int exStyle = Win32Api.GetWindowLong32(hWnd, Win32Api.GWL_EXSTYLE);
            if ((exStyle & Win32Api.WS_EX_TOOLWINDOW) != 0)
            {
                return false;
            }

            if (!Win32Api.GetWindowRect(hWnd, out var rect))
            {
                return false;
            }

            return rect.Width > 0 && rect.Height > 0;
        }

        public static string GetWindowTitle(IntPtr hWnd)
        {
            int length = Win32Api.GetWindowTextLength(hWnd);
            if (length <= 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(length + 1);
            Win32Api.GetWindowText(hWnd, builder, builder.Capacity);
            return builder.ToString().Trim();
        }

        public static bool IsMaximized(IntPtr hWnd)
        {
            var placement = new Win32Api.WINDOWPLACEMENT
            {
                Length = System.Runtime.InteropServices.Marshal.SizeOf<Win32Api.WINDOWPLACEMENT>()
            };

            return Win32Api.GetWindowPlacement(hWnd, ref placement) &&
                placement.ShowCmd == Win32Api.SW_SHOWMAXIMIZED;
        }

        private static string GetExecutablePath(Process process)
        {
            try
            {
                return process.MainModule?.FileName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
