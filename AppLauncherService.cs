using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MonitorLauncher
{
    public class AppLauncherService
    {
        public async Task<LaunchResult> LaunchAsync(LaunchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ExecutablePath) || !File.Exists(request.ExecutablePath))
            {
                return new LaunchResult
                {
                    FileMissing = true,
                    StatusMessage = "선택한 파일이 존재하지 않습니다."
                };
            }

            var targetScreen = FindTargetScreen(request.MonitorDeviceName);
            if (targetScreen == null)
            {
                return new LaunchResult
                {
                    MonitorMissing = true,
                    StatusMessage = "저장된 모니터를 찾을 수 없습니다."
                };
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = request.ExecutablePath,
                UseShellExecute = false
            };

            if (!string.IsNullOrWhiteSpace(request.Arguments))
            {
                startInfo.Arguments = request.Arguments;
            }

            var process = Process.Start(startInfo);
            if (process == null)
            {
                return new LaunchResult
                {
                    StatusMessage = "프로그램 실행 실패"
                };
            }

            await Task.Delay(100);
            bool success = await WindowController.MoveWindowToMonitor(process, targetScreen, request.WindowState);

            if (success)
            {
                await Task.Delay(500);
                await WindowController.EnsureWindowOnMonitor(process, targetScreen, request.WindowState);
            }

            return new LaunchResult
            {
                Succeeded = true,
                WindowMoved = success,
                StatusMessage = BuildStatusMessage(request, targetScreen, success)
            };
        }

        private static Screen? FindTargetScreen(string monitorDeviceName)
        {
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.DeviceName == monitorDeviceName)
                {
                    return screen;
                }
            }

            return null;
        }

        private static string BuildStatusMessage(LaunchRequest request, Screen targetScreen, bool success)
        {
            if (!success)
            {
                return "프로그램은 실행되었지만 창 위치 제어에 실패했습니다.";
            }

            if (!string.IsNullOrWhiteSpace(request.ProfileName))
            {
                return $"프로필 '{request.ProfileName}' 실행 완료";
            }

            return $"프로그램이 {targetScreen.DeviceName}에서 실행되었습니다.";
        }
    }
}
