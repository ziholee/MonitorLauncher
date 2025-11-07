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
                    // 창모드: 모니터 중앙에 적절한 크기로 배치 (모니터의 80% 크기, 최대 1200x800)
                    int windowWidth = Math.Min(bounds.Width * 80 / 100, 1200);
                    int windowHeight = Math.Min(bounds.Height * 80 / 100, 800);
                    int windowX = bounds.X + (bounds.Width - windowWidth) / 2;
                    int windowY = bounds.Y + (bounds.Height - windowHeight) / 2;
                    
                    // 먼저 창을 숨긴 상태로 위치 설정 후 표시
                    Win32Api.ShowWindow(mainWindowHandle, Win32Api.SW_HIDE);
                    await Task.Delay(50);
                    success = Win32Api.SetWindowPos(mainWindowHandle, Win32Api.HWND_TOP,
                        windowX, windowY, windowWidth, windowHeight,
                        Win32Api.SWP_SHOWWINDOW | Win32Api.SWP_NOACTIVATE);
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
                
                // 창 위치가 올바른 모니터에 있는지 확인하고 필요시 재설정 (프로그램이 위치를 변경하는 경우 대비)
                await EnsureWindowOnMonitor(mainWindowHandle, targetScreen, windowState);
            }

            return success;
        }

        private static async Task EnsureWindowOnMonitor(IntPtr hWnd, Screen targetScreen, AppWindowState windowState)
        {
            if (hWnd == IntPtr.Zero)
                return;

            var targetBounds = targetScreen.Bounds;
            const int maxRetries = 10;
            const int retryDelay = 200; // 200ms

            for (int i = 0; i < maxRetries; i++)
            {
                await Task.Delay(retryDelay);

                if (!Win32Api.GetWindowRect(hWnd, out var currentRect))
                    continue;

                // 창이 대상 모니터 영역에 있는지 확인 (창의 중심점이 모니터 내에 있는지 확인)
                int windowCenterX = (currentRect.Left + currentRect.Right) / 2;
                int windowCenterY = (currentRect.Top + currentRect.Bottom) / 2;
                
                bool isOnTargetMonitor = windowCenterX >= targetBounds.Left &&
                                        windowCenterX <= targetBounds.Right &&
                                        windowCenterY >= targetBounds.Top &&
                                        windowCenterY <= targetBounds.Bottom;

                // 창이 대상 모니터에 있으면 종료
                if (isOnTargetMonitor)
                    break;

                // 창이 다른 모니터에 있으면 다시 이동
                switch (windowState)
                {
                    case AppWindowState.Maximized:
                        Win32Api.SetWindowPos(hWnd, Win32Api.HWND_TOP,
                            targetBounds.X, targetBounds.Y, targetBounds.Width, targetBounds.Height,
                            Win32Api.SWP_SHOWWINDOW | Win32Api.SWP_NOACTIVATE);
                        await Task.Delay(50);
                        Win32Api.ShowWindow(hWnd, Win32Api.SW_SHOWMAXIMIZED);
                        break;

                    case AppWindowState.Normal:
                        // 창모드: 모니터 중앙에 적절한 크기로 배치
                        int normalWidth = Math.Min(targetBounds.Width * 80 / 100, 1200);
                        int normalHeight = Math.Min(targetBounds.Height * 80 / 100, 800);
                        int normalX = targetBounds.X + (targetBounds.Width - normalWidth) / 2;
                        int normalY = targetBounds.Y + (targetBounds.Height - normalHeight) / 2;
                        
                        Win32Api.SetWindowPos(hWnd, Win32Api.HWND_TOP,
                            normalX, normalY, normalWidth, normalHeight,
                            Win32Api.SWP_SHOWWINDOW | Win32Api.SWP_NOACTIVATE);
                        await Task.Delay(50);
                        Win32Api.ShowWindow(hWnd, Win32Api.SW_SHOWNORMAL);
                        break;

                    case AppWindowState.Restore:
                        Win32Api.ShowWindow(hWnd, Win32Api.SW_RESTORE);
                        Win32Api.SetWindowPos(hWnd, Win32Api.HWND_TOP,
                            targetBounds.X, targetBounds.Y, 0, 0,
                            Win32Api.SWP_NOSIZE | Win32Api.SWP_SHOWWINDOW | Win32Api.SWP_NOACTIVATE);
                        break;
                }
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
    }
}
