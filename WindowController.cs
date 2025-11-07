using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MonitorLauncher
{
    public class WindowController
    {
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
                    process.Refresh();
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

            // 윈도우를 대상 모니터로 이동
            var bounds = targetScreen.Bounds;
            bool success = false;

            switch (windowState)
            {
                case AppWindowState.Maximized:
                    // 먼저 모니터로 이동한 후 최대화
                    success = Win32Api.SetWindowPos(mainWindowHandle, Win32Api.HWND_TOP,
                        bounds.X, bounds.Y, bounds.Width, bounds.Height,
                        Win32Api.SWP_SHOWWINDOW);
                    if (success)
                    {
                        await Task.Delay(50);
                        Win32Api.ShowWindow(mainWindowHandle, Win32Api.SW_SHOWMAXIMIZED);
                    }
                    break;

                case AppWindowState.Normal:
                    success = Win32Api.SetWindowPos(mainWindowHandle, Win32Api.HWND_TOP,
                        bounds.X, bounds.Y, bounds.Width, bounds.Height,
                        Win32Api.SWP_SHOWWINDOW);
                    if (success)
                    {
                        await Task.Delay(50);
                        Win32Api.ShowWindow(mainWindowHandle, Win32Api.SW_SHOWNORMAL);
                    }
                    break;

                case AppWindowState.Restore:
                    Win32Api.ShowWindow(mainWindowHandle, Win32Api.SW_RESTORE);
                    success = Win32Api.SetWindowPos(mainWindowHandle, Win32Api.HWND_TOP,
                        bounds.X, bounds.Y, 0, 0,
                        Win32Api.SWP_NOSIZE | Win32Api.SWP_SHOWWINDOW);
                    break;
            }

            if (success)
            {
                await Task.Delay(50);
                Win32Api.BringWindowToTop(mainWindowHandle);
            }

            return success;
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
    }

    public enum AppWindowState
    {
        Normal,
        Maximized,
        Restore
    }
}
