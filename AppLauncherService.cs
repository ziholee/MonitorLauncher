using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MonitorLauncher
{
    public class AppLauncherService
    {
        public async Task<LaunchResult> LaunchAsync(LaunchRequest request)
        {
            if (!CanLaunch(request.ExecutablePath))
            {
                return new LaunchResult
                {
                    FileMissing = true,
                    StatusMessage = "선택한 파일이 존재하지 않습니다."
                };
            }

            if (!TryResolveTargetScreen(request, out var targetScreen, out var usedFallbackMonitor))
            {
                return new LaunchResult
                {
                    MonitorMissing = true,
                    StatusMessage = "저장된 모니터를 찾을 수 없습니다."
                };
            }

            var startInfo = BuildStartInfo(request);
            var existingWindows = startInfo.UseShellExecute ? null : WindowController.CaptureVisibleWindows();
            Process? process;
            bool launchStarted = false;

            try
            {
                process = Process.Start(startInfo);
                launchStarted = process != null || startInfo.UseShellExecute;
            }
            catch
            {
                process = null;
            }

            if (!launchStarted)
            {
                return new LaunchResult
                {
                    StatusMessage = "프로그램 실행 실패"
                };
            }

            await Task.Delay(100);
            bool success = false;
            bool skippedWindowControl = startInfo.UseShellExecute;
            bool canControlWindow = process != null && !startInfo.UseShellExecute;

            if (canControlWindow)
            {
                success = await WindowController.MoveWindowToMonitor(process, targetScreen!, request.WindowState);
            }

            if (success)
            {
                await Task.Delay(500);
                if (process != null)
                {
                    success = await WindowController.EnsureWindowOnMonitor(process, targetScreen!, request.WindowState);
                }
            }
            else if (canControlWindow && existingWindows != null)
            {
                success = await WindowController.MoveNewWindowToMonitorAsync(existingWindows, targetScreen!, request.WindowState, process?.Id);
            }

            return new LaunchResult
            {
                Succeeded = launchStarted,
                WindowMoved = success,
                UsedMonitorFallback = usedFallbackMonitor,
                StatusMessage = BuildStatusMessage(request, targetScreen!, success, usedFallbackMonitor, skippedWindowControl)
            };
        }

        public bool TryResolveTargetScreen(LaunchRequest request, out Screen? screen, out bool usedFallback)
        {
            var screenMatch = FindTargetScreen(request);
            screen = screenMatch?.Screen;
            usedFallback = screenMatch?.UsedFallback ?? false;
            return screen != null;
        }

        private static bool CanLaunch(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return false;
            }

            if (Uri.TryCreate(executablePath, UriKind.Absolute, out var uri) && !uri.IsFile)
            {
                return true;
            }

            return File.Exists(executablePath);
        }

        private static ProcessStartInfo BuildStartInfo(LaunchRequest request)
        {
            bool useShellExecute = ShouldUseShellExecute(request.ExecutablePath);
            var startInfo = new ProcessStartInfo
            {
                FileName = request.ExecutablePath,
                UseShellExecute = useShellExecute
            };

            if (!string.IsNullOrWhiteSpace(request.Arguments))
            {
                startInfo.Arguments = request.Arguments;
            }

            return startInfo;
        }

        private static bool ShouldUseShellExecute(string executablePath)
        {
            if (Uri.TryCreate(executablePath, UriKind.Absolute, out var uri) && !uri.IsFile)
            {
                return true;
            }

            string extension = Path.GetExtension(executablePath).ToLowerInvariant();
            return extension != ".exe";
        }

        private static ScreenMatchResult? FindTargetScreen(LaunchRequest request)
        {
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.DeviceName == request.MonitorDeviceName)
                {
                    return new ScreenMatchResult(screen, false);
                }
            }

            foreach (var screen in Screen.AllScreens)
            {
                if (screen.Bounds.X == request.MonitorBoundsX &&
                    screen.Bounds.Y == request.MonitorBoundsY &&
                    screen.Bounds.Width == request.MonitorBoundsWidth &&
                    screen.Bounds.Height == request.MonitorBoundsHeight)
                {
                    return new ScreenMatchResult(screen, true);
                }
            }

            foreach (var screen in Screen.AllScreens)
            {
                if (screen.Primary == request.MonitorWasPrimary &&
                    screen.Bounds.Width == request.MonitorBoundsWidth &&
                    screen.Bounds.Height == request.MonitorBoundsHeight)
                {
                    return new ScreenMatchResult(screen, true);
                }
            }

            var closestScreen = Screen.AllScreens
                .OrderBy(screen => GetScreenDistance(screen, request))
                .FirstOrDefault();

            return closestScreen == null ? null : new ScreenMatchResult(closestScreen, true);
        }

        private static int GetScreenDistance(Screen screen, LaunchRequest request)
        {
            int positionDistance = Math.Abs(screen.Bounds.X - request.MonitorBoundsX) + Math.Abs(screen.Bounds.Y - request.MonitorBoundsY);
            int sizeDistance = Math.Abs(screen.Bounds.Width - request.MonitorBoundsWidth) + Math.Abs(screen.Bounds.Height - request.MonitorBoundsHeight);
            int primaryPenalty = screen.Primary == request.MonitorWasPrimary ? 0 : 1000;
            return positionDistance + sizeDistance + primaryPenalty;
        }

        private static string BuildStatusMessage(LaunchRequest request, Screen targetScreen, bool success, bool usedFallbackMonitor, bool skippedWindowControl)
        {
            string prefix = usedFallbackMonitor ? "저장된 모니터 대신 가장 유사한 모니터를 사용했습니다. " : string.Empty;

            if (!success)
            {
                if (skippedWindowControl)
                {
                    return $"{prefix}프로그램은 실행되었지만 이 실행 방식은 창 위치 제어를 지원하지 않습니다.";
                }

                return $"{prefix}프로그램은 실행되었지만 창 위치 제어에 실패했습니다.";
            }

            if (!string.IsNullOrWhiteSpace(request.ProfileName))
            {
                return $"{prefix}프로필 '{request.ProfileName}' 실행 완료";
            }

            return $"{prefix}프로그램이 {targetScreen.DeviceName}에서 실행되었습니다.";
        }

        private sealed class ScreenMatchResult
        {
            public ScreenMatchResult(Screen screen, bool usedFallback)
            {
                Screen = screen;
                UsedFallback = usedFallback;
            }

            public Screen Screen { get; }
            public bool UsedFallback { get; }
        }
    }
}
