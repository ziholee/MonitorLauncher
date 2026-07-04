using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MonitorLauncher
{
    public class WorkspaceRestoreService
    {
        public async Task<WorkspaceRestoreResult> RestoreAsync(WorkspaceProfile workspace)
        {
            var result = new WorkspaceRestoreResult
            {
                TotalApps = workspace.Apps.Count
            };

            foreach (var app in workspace.Apps)
            {
                if (await RestoreAppWindowAsync(app, result))
                {
                    result.RestoredWindows++;
                }
                else
                {
                    result.FailedApps++;
                }
            }

            return result;
        }

        public int GatherWindowsToPrimaryMonitor()
        {
            var primaryScreen = Screen.PrimaryScreen;
            if (primaryScreen == null)
            {
                return 0;
            }

            int movedCount = 0;
            var allScreenBounds = GetAllScreenBounds();

            Win32Api.EnumWindows((hWnd, lParam) =>
            {
                if (!WindowCaptureService.IsGeneralVisibleWindow(hWnd) ||
                    string.IsNullOrWhiteSpace(WindowCaptureService.GetWindowTitle(hWnd)))
                {
                    return true;
                }

                if (!Win32Api.GetWindowRect(hWnd, out var rect))
                {
                    return true;
                }

                var center = new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
                bool isOutsideScreens = !allScreenBounds.Contains(center);
                bool hasInvalidSize = rect.Width < 120 || rect.Height < 80;

                if (isOutsideScreens || hasInvalidSize)
                {
                    MoveWindowToSafePrimaryBounds(hWnd, primaryScreen, Math.Max(rect.Width, 800), Math.Max(rect.Height, 600), false);
                    movedCount++;
                }

                return true;
            }, IntPtr.Zero);

            return movedCount;
        }

        private async Task<bool> RestoreAppWindowAsync(AppWindowProfile app, WorkspaceRestoreResult result)
        {
            IntPtr hWnd = FindRunningWindow(app);
            bool launched = false;

            if (hWnd == IntPtr.Zero && app.LaunchIfNotRunning)
            {
                launched = TryLaunchApp(app, result);
                if (launched)
                {
                    result.LaunchedApps++;
                    hWnd = await WaitForWindowAsync(app);
                }
            }

            if (hWnd == IntPtr.Zero)
            {
                result.Messages.Add($"{app.DisplayName}: 실행 중인 창을 찾지 못했습니다.");
                return false;
            }

            MoveWindowToSavedBounds(hWnd, app);
            result.Messages.Add($"{app.DisplayName}: 창 위치 복원 완료");
            return true;
        }

        private static bool TryLaunchApp(AppWindowProfile app, WorkspaceRestoreResult result)
        {
            if (string.IsNullOrWhiteSpace(app.ExecutablePath) || !File.Exists(app.ExecutablePath))
            {
                result.Messages.Add($"{app.DisplayName}: 실행 파일을 찾을 수 없습니다.");
                return false;
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = app.ExecutablePath,
                    UseShellExecute = Path.GetExtension(app.ExecutablePath).Equals(".exe", StringComparison.OrdinalIgnoreCase) ? false : true
                };

                if (!string.IsNullOrWhiteSpace(app.Arguments))
                {
                    startInfo.Arguments = app.Arguments;
                }

                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                result.Messages.Add($"{app.DisplayName}: 실행 실패 - {ex.Message}");
                return false;
            }
        }

        private static async Task<IntPtr> WaitForWindowAsync(AppWindowProfile app)
        {
            const int maxAttempts = 50;

            for (int i = 0; i < maxAttempts; i++)
            {
                await Task.Delay(100);
                IntPtr hWnd = FindRunningWindow(app);
                if (hWnd != IntPtr.Zero)
                {
                    return hWnd;
                }
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindRunningWindow(AppWindowProfile app)
        {
            var candidates = new List<(IntPtr Handle, int Area)>();
            string expectedProcessName = Path.GetFileNameWithoutExtension(app.ProcessName);

            Win32Api.EnumWindows((hWnd, lParam) =>
            {
                if (!WindowCaptureService.IsGeneralVisibleWindow(hWnd))
                {
                    return true;
                }

                Win32Api.GetWindowThreadProcessId(hWnd, out uint processId);
                if (processId == 0)
                {
                    return true;
                }

                try
                {
                    using var process = Process.GetProcessById((int)processId);
                    if (!string.Equals(process.ProcessName, expectedProcessName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch
                {
                    return true;
                }

                if (!Win32Api.GetWindowRect(hWnd, out var rect))
                {
                    return true;
                }

                candidates.Add((hWnd, rect.Width * rect.Height));
                return true;
            }, IntPtr.Zero);

            return candidates
                .OrderByDescending(candidate => candidate.Area)
                .Select(candidate => candidate.Handle)
                .FirstOrDefault();
        }

        private static void MoveWindowToSavedBounds(IntPtr hWnd, AppWindowProfile app)
        {
            Screen? targetScreen = FindTargetScreen(app);
            if (targetScreen == null)
            {
                var primaryScreen = Screen.PrimaryScreen;
                if (primaryScreen == null)
                {
                    return;
                }

                MoveWindowToSafePrimaryBounds(hWnd, primaryScreen, app.Width, app.Height, app.IsMaximized);
                return;
            }

            Rectangle targetBounds = targetScreen.Bounds;
            int width = Math.Max(app.Width, 300);
            int height = Math.Max(app.Height, 200);
            int x = Clamp(app.X, targetBounds.Left, Math.Max(targetBounds.Right - width, targetBounds.Left));
            int y = Clamp(app.Y, targetBounds.Top, Math.Max(targetBounds.Bottom - height, targetBounds.Top));

            MoveWindow(hWnd, x, y, width, height, app.IsMaximized);
        }

        private static void MoveWindowToSafePrimaryBounds(IntPtr hWnd, Screen primaryScreen, int requestedWidth, int requestedHeight, bool isMaximized)
        {
            Rectangle bounds = primaryScreen.WorkingArea;
            int width = Math.Min(Math.Max(requestedWidth, 800), bounds.Width);
            int height = Math.Min(Math.Max(requestedHeight, 600), bounds.Height);
            int x = bounds.Left + (bounds.Width - width) / 2;
            int y = bounds.Top + (bounds.Height - height) / 2;

            MoveWindow(hWnd, x, y, width, height, isMaximized);
        }

        private static void MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool isMaximized)
        {
            Win32Api.ShowWindow(hWnd, Win32Api.SW_RESTORE);
            Win32Api.SetWindowPos(hWnd, Win32Api.HWND_TOP, x, y, width, height,
                Win32Api.SWP_SHOWWINDOW | Win32Api.SWP_NOACTIVATE);

            if (isMaximized)
            {
                Win32Api.ShowWindow(hWnd, Win32Api.SW_SHOWMAXIMIZED);
            }
            else
            {
                Win32Api.ShowWindow(hWnd, Win32Api.SW_SHOWNORMAL);
            }
        }

        private static Screen? FindTargetScreen(AppWindowProfile app)
        {
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.DeviceName == app.MonitorDeviceName)
                {
                    return screen;
                }
            }

            return null;
        }

        private static Rectangle GetAllScreenBounds()
        {
            Rectangle bounds = Rectangle.Empty;
            foreach (var screen in Screen.AllScreens)
            {
                bounds = bounds == Rectangle.Empty ? screen.Bounds : Rectangle.Union(bounds, screen.Bounds);
            }

            return bounds;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
