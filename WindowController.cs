using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MonitorLauncher
{
    public class WindowController
    {
        public static HashSet<IntPtr> CaptureVisibleWindows()
        {
            var windows = new HashSet<IntPtr>();

            Win32Api.EnumWindows((hWnd, lParam) =>
            {
                if (IsCandidateWindow(hWnd))
                {
                    windows.Add(hWnd);
                }

                return true;
            }, IntPtr.Zero);

            return windows;
        }

        public static async Task<bool> MoveWindowToMonitor(Process process, Screen targetScreen, AppWindowState windowState = AppWindowState.Maximized)
        {
            if (process == null || process.HasExited)
                return false;

            IntPtr mainWindowHandle = IntPtr.Zero;
            int attempts = 0;
            const int maxAttempts = 50; // 5초간 재시도 (100ms * 50)

            // 메인 윈도우 핸들 획득 (최대 5초간 폴링)
            while (attempts < maxAttempts && (mainWindowHandle == IntPtr.Zero || mainWindowHandle == process.MainWindowHandle))
            {
                if (process.HasExited)
                    return false;

                try
                {
                    // 필요한 경우에만 Refresh 호출 (최적화)
                    if (mainWindowHandle == IntPtr.Zero)
                    {
                        process.Refresh();
                    }
                    mainWindowHandle = process.MainWindowHandle;

                    if (mainWindowHandle != IntPtr.Zero)
                    {
                        // 프로세스 ID로 윈도우 찾기 (더 정확한 방법)
                        mainWindowHandle = FindMainWindowByProcessId(process.Id);
                        if (mainWindowHandle != IntPtr.Zero)
                            break;
                    }
                }
                catch { }

                await Task.Delay(100);
                attempts++;
            }

            if (mainWindowHandle == IntPtr.Zero)
                return false;

            bool success = await MoveWindowHandleToMonitorAsync(mainWindowHandle, targetScreen, windowState, hideBeforeMove: true);

            if (success)
            {
                await Task.Delay(50);
                Win32Api.BringWindowToTop(mainWindowHandle);
                
                // 창 위치가 올바른 모니터에 있는지 확인하고 필요시 재설정 (프로그램이 위치를 변경하는 경우 대비)
                return await EnsureWindowOnMonitor(mainWindowHandle, targetScreen, windowState);
            }

            return false;
        }

        public static async Task<bool> MoveNewWindowToMonitorAsync(HashSet<IntPtr> existingWindows, Screen targetScreen, AppWindowState windowState, int? preferredProcessId = null)
        {
            const int maxAttempts = 50;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                await Task.Delay(100);

                var candidate = FindBestNewWindow(existingWindows, preferredProcessId);
                if (candidate == IntPtr.Zero)
                {
                    continue;
                }

                if (await EnsureWindowOnMonitor(candidate, targetScreen, windowState))
                {
                    return true;
                }
            }

            return false;
        }

        public static async Task<bool> EnsureWindowOnMonitor(Process process, Screen targetScreen, AppWindowState windowState)
        {
            if (process == null || process.HasExited)
                return false;

            IntPtr hWnd = IntPtr.Zero;
            int attempts = 0;
            const int maxAttempts = 20; // 2초간 재시도

            // 윈도우 핸들 획득
            while (attempts < maxAttempts && hWnd == IntPtr.Zero)
            {
                if (process.HasExited)
                    return false;

                try
                {
                    // 필요한 경우에만 Refresh 호출 (최적화)
                    if (hWnd == IntPtr.Zero)
                    {
                        process.Refresh();
                    }
                    hWnd = process.MainWindowHandle;
                    if (hWnd == IntPtr.Zero)
                    {
                        hWnd = FindMainWindowByProcessId(process.Id);
                    }
                }
                catch { }

                if (hWnd != IntPtr.Zero)
                    break;

                await Task.Delay(100);
                attempts++;
            }

            if (hWnd != IntPtr.Zero)
            {
                return await EnsureWindowOnMonitor(hWnd, targetScreen, windowState);
            }

            return false;
        }

        private static async Task<bool> EnsureWindowOnMonitor(IntPtr hWnd, Screen targetScreen, AppWindowState windowState)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            var targetBounds = targetScreen.Bounds;
            const int maxRetries = 20; // 2초간 재시도 (100ms * 20)
            const int retryDelay = 100; // 100ms
            int consecutiveSuccess = 0; // 연속 성공 횟수

            for (int i = 0; i < maxRetries; i++)
            {
                await Task.Delay(retryDelay);

                if (!Win32Api.GetWindowRect(hWnd, out var currentRect))
                {
                    consecutiveSuccess = 0; // 실패 시 리셋
                    continue;
                }

                // 창이 대상 모니터 영역에 있는지 확인 (창의 중심점이 모니터 내에 있는지 확인)
                int windowCenterX = (currentRect.Left + currentRect.Right) / 2;
                int windowCenterY = (currentRect.Top + currentRect.Bottom) / 2;
                
                bool isOnTargetMonitor = windowCenterX >= targetBounds.Left &&
                                        windowCenterX <= targetBounds.Right &&
                                        windowCenterY >= targetBounds.Top &&
                                        windowCenterY <= targetBounds.Bottom;

                // 창이 대상 모니터에 있으면 연속 확인
                if (isOnTargetMonitor)
                {
                    consecutiveSuccess++;
                    // 3회 연속 확인 시 조기 종료
                    if (consecutiveSuccess >= 3)
                        return true;

                    continue;
                }

                consecutiveSuccess = 0; // 실패 시 리셋

                // 창이 다른 모니터에 있으면 다시 이동
                await MoveWindowHandleToMonitorAsync(hWnd, targetScreen, windowState, hideBeforeMove: false);
            }

            return false;
        }

        private static async Task<bool> MoveWindowHandleToMonitorAsync(IntPtr hWnd, Screen targetScreen, AppWindowState windowState, bool hideBeforeMove)
        {
            var bounds = targetScreen.Bounds;

            switch (windowState)
            {
                case AppWindowState.Maximized:
                    bool maximized = Win32Api.SetWindowPos(hWnd, Win32Api.HWND_TOP,
                        bounds.X, bounds.Y, bounds.Width, bounds.Height,
                        Win32Api.SWP_SHOWWINDOW | Win32Api.SWP_NOACTIVATE);
                    if (maximized)
                    {
                        await Task.Delay(50);
                        Win32Api.ShowWindow(hWnd, Win32Api.SW_SHOWMAXIMIZED);
                    }

                    return maximized;

                case AppWindowState.Normal:
                    int windowWidth = Math.Min(bounds.Width * 80 / 100, 1200);
                    int windowHeight = Math.Min(bounds.Height * 80 / 100, 800);
                    int windowX = bounds.X + (bounds.Width - windowWidth) / 2;
                    int windowY = bounds.Y + (bounds.Height - windowHeight) / 2;

                    if (hideBeforeMove)
                    {
                        Win32Api.ShowWindow(hWnd, Win32Api.SW_HIDE);
                        await Task.Delay(50);
                    }

                    bool normal = Win32Api.SetWindowPos(hWnd, Win32Api.HWND_TOP,
                        windowX, windowY, windowWidth, windowHeight,
                        Win32Api.SWP_SHOWWINDOW | Win32Api.SWP_NOACTIVATE);
                    if (normal)
                    {
                        await Task.Delay(50);
                        Win32Api.ShowWindow(hWnd, Win32Api.SW_SHOWNORMAL);
                    }

                    return normal;

                case AppWindowState.Restore:
                    Win32Api.ShowWindow(hWnd, Win32Api.SW_RESTORE);
                    return Win32Api.SetWindowPos(hWnd, Win32Api.HWND_TOP,
                        bounds.X, bounds.Y, 0, 0,
                        Win32Api.SWP_NOSIZE | Win32Api.SWP_SHOWWINDOW | Win32Api.SWP_NOACTIVATE);

                default:
                    return false;
            }
        }

        private static IntPtr FindMainWindowByProcessId(int processId)
        {
            IntPtr foundWindow = IntPtr.Zero;
            List<IntPtr> windows = new List<IntPtr>();

            Win32Api.EnumWindows((hWnd, lParam) =>
            {
                Win32Api.GetWindowThreadProcessId(hWnd, out uint windowProcessId);
                if (windowProcessId == processId && Win32Api.IsWindowVisible(hWnd))
                {
                    windows.Add(hWnd);
                }
                return true;
            }, IntPtr.Zero);

            // 가장 큰 윈도우를 메인 윈도우로 간주
            if (windows.Count > 0)
            {
                foundWindow = windows.OrderByDescending(hwnd =>
                {
                    if (Win32Api.GetWindowRect(hwnd, out var rect))
                        return (rect.Width * rect.Height);
                    return 0;
                }).FirstOrDefault();

                if (foundWindow == IntPtr.Zero)
                    foundWindow = windows[0];
            }

            return foundWindow;
        }

        private static IntPtr FindBestNewWindow(HashSet<IntPtr> existingWindows, int? preferredProcessId)
        {
            var candidates = new List<(IntPtr Handle, bool PreferredProcess, int Area)>();

            Win32Api.EnumWindows((hWnd, lParam) =>
            {
                if (existingWindows.Contains(hWnd) || !IsCandidateWindow(hWnd))
                {
                    return true;
                }

                Win32Api.GetWindowThreadProcessId(hWnd, out uint pid);
                bool preferred = preferredProcessId.HasValue && pid == preferredProcessId.Value;

                if (!Win32Api.GetWindowRect(hWnd, out var rect))
                {
                    return true;
                }

                candidates.Add((hWnd, preferred, rect.Width * rect.Height));
                return true;
            }, IntPtr.Zero);

            return candidates
                .OrderByDescending(candidate => candidate.PreferredProcess)
                .ThenByDescending(candidate => candidate.Area)
                .Select(candidate => candidate.Handle)
                .FirstOrDefault();
        }

        private static bool IsCandidateWindow(IntPtr hWnd)
        {
            if (!Win32Api.IsWindowVisible(hWnd))
            {
                return false;
            }

            int exStyle = Win32Api.GetWindowLong32(hWnd, Win32Api.GWL_EXSTYLE);
            if ((exStyle & Win32Api.WS_EX_TOOLWINDOW) != 0)
            {
                return false;
            }

            if (!Win32Api.GetWindowRect(hWnd, out var rect))
            {
                return false;
            }

            return rect.Width >= 200 && rect.Height >= 150;
        }
    }
}
