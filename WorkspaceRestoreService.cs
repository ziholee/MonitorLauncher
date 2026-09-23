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

            var usedWindows = new HashSet<IntPtr>();
            foreach (var app in workspace.Apps)
            {
                if (await RestoreAppWindowAsync(app, result, usedWindows))
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
            var screens = Screen.AllScreens;

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

                // A bounding union also contains gaps between physical displays.
                var titleBounds = new Rectangle(rect.Left, rect.Top, rect.Width, Math.Min(32, rect.Height));
                bool isOutsideScreens = !screens.Any(screen =>
                {
                    var visible = Rectangle.Intersect(screen.WorkingArea, titleBounds);
                    return visible.Width >= Math.Min(120, rect.Width) && visible.Height >= 16;
                });
                bool hasInvalidSize = rect.Width < 120 || rect.Height < 80;

                if (isOutsideScreens || hasInvalidSize)
                {
                    if (MoveWindowToSafePrimaryBounds(hWnd, primaryScreen, Math.Max(rect.Width, 800), Math.Max(rect.Height, 600), false))
                        movedCount++;
                }

                return true;
            }, IntPtr.Zero);

            return movedCount;
        }

        private async Task<bool> RestoreAppWindowAsync(AppWindowProfile app, WorkspaceRestoreResult result, HashSet<IntPtr> usedWindows)
        {
            IntPtr hWnd = FindRunningWindow(app, usedWindows);
            bool launched = false;

            if (hWnd == IntPtr.Zero && app.LaunchIfNotRunning)
            {
                launched = TryLaunchApp(app, result);
                if (launched)
                {
                    result.LaunchedApps++;
                    hWnd = await WaitForWindowAsync(app, usedWindows);
                }
            }

            if (hWnd == IntPtr.Zero)
            {
                result.Messages.Add($"{app.DisplayName}: 실행 중인 창을 찾지 못했습니다.");
                return false;
            }

            usedWindows.Add(hWnd);
            if (!MoveWindowToSavedBounds(hWnd, app, out bool usedFallback))
            {
                result.Messages.Add($"{app.DisplayName}: 창 위치 이동에 실패했습니다. 창 종료 여부와 권한을 확인해주세요.");
                return false;
            }
            if (usedFallback) result.FallbackWindows++;
            result.Messages.Add(usedFallback
                ? $"{app.DisplayName}: 저장된 모니터가 없어 주 모니터로 복원했습니다."
                : $"{app.DisplayName}: 창 위치 복원 완료");
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

        private static async Task<IntPtr> WaitForWindowAsync(AppWindowProfile app, HashSet<IntPtr> usedWindows)
        {
            const int maxAttempts = 50;

            for (int i = 0; i < maxAttempts; i++)
            {
                await Task.Delay(100);
                IntPtr hWnd = FindRunningWindow(app, usedWindows);
                if (hWnd != IntPtr.Zero)
                {
                    return hWnd;
                }
            }

            return IntPtr.Zero;
        }

        private static IntPtr FindRunningWindow(AppWindowProfile app, HashSet<IntPtr> usedWindows)
        {
            var candidates = new List<(IntPtr Handle, bool TitleMatches, long Area)>();
            string expectedProcessName = Path.GetFileNameWithoutExtension(app.ProcessName);

            Win32Api.EnumWindows((hWnd, lParam) =>
            {
                if (usedWindows.Contains(hWnd) || !WindowCaptureService.IsGeneralVisibleWindow(hWnd))
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
                    if (!string.IsNullOrWhiteSpace(app.ExecutablePath) &&
                        !string.Equals(process.MainModule?.FileName, app.ExecutablePath, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                catch
                {
                    return true;
                }

                if (!Win32Api.GetWindowRect(hWnd, out var rect))
                {
                    return true;
                }

                candidates.Add((hWnd, string.Equals(WindowCaptureService.GetWindowTitle(hWnd), app.DisplayName,
                    StringComparison.Ordinal), (long)rect.Width * rect.Height));
                return true;
            }, IntPtr.Zero);

            return candidates
                .OrderByDescending(candidate => candidate.TitleMatches)
                .ThenByDescending(candidate => candidate.Area)
                .Select(candidate => candidate.Handle)
                .FirstOrDefault();
        }

        private static bool MoveWindowToSavedBounds(IntPtr hWnd, AppWindowProfile app, out bool usedFallback)
        {
            Screen? targetScreen = FindTargetScreen(app);
            usedFallback = targetScreen == null;
            if (targetScreen == null)
            {
                var primaryScreen = Screen.PrimaryScreen;
                if (primaryScreen == null)
                {
                    return false;
                }

                return MoveWindowToSafePrimaryBounds(hWnd, primaryScreen, app.Width, app.Height, app.IsMaximized);
            }

            Rectangle targetBounds = targetScreen.WorkingArea;
            int width = Math.Min(Math.Max(app.Width, 300), targetBounds.Width);
            int height = Math.Min(Math.Max(app.Height, 200), targetBounds.Height);
            int x = Clamp(app.X, targetBounds.Left, Math.Max(targetBounds.Right - width, targetBounds.Left));
            int y = Clamp(app.Y, targetBounds.Top, Math.Max(targetBounds.Bottom - height, targetBounds.Top));

            return MoveWindow(hWnd, x, y, width, height, app.IsMaximized);
        }

        private static bool MoveWindowToSafePrimaryBounds(IntPtr hWnd, Screen primaryScreen, int requestedWidth, int requestedHeight, bool isMaximized)
        {
            Rectangle bounds = primaryScreen.WorkingArea;
            int width = Math.Min(Math.Max(requestedWidth, 800), bounds.Width);
            int height = Math.Min(Math.Max(requestedHeight, 600), bounds.Height);
            int x = bounds.Left + (bounds.Width - width) / 2;
            int y = bounds.Top + (bounds.Height - height) / 2;

            return MoveWindow(hWnd, x, y, width, height, isMaximized);
        }

        private static bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool isMaximized)
        {
            Win32Api.ShowWindow(hWnd, Win32Api.SW_RESTORE);
            if (!Win32Api.SetWindowPos(hWnd, Win32Api.HWND_TOP, x, y, width, height,
                Win32Api.SWP_SHOWWINDOW | Win32Api.SWP_NOACTIVATE)) return false;

            if (isMaximized)
            {
                Win32Api.ShowWindow(hWnd, Win32Api.SW_SHOWMAXIMIZED);
            }
            else
            {
                Win32Api.ShowWindow(hWnd, Win32Api.SW_SHOWNORMAL);
            }
            return true;
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
