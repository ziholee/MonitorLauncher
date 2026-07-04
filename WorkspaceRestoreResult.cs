using System.Collections.Generic;

namespace MonitorLauncher
{
    public class WorkspaceRestoreResult
    {
        public int TotalApps { get; set; }
        public int RestoredWindows { get; set; }
        public int LaunchedApps { get; set; }
        public int FailedApps { get; set; }
        public List<string> Messages { get; set; } = new List<string>();

        public string Summary
        {
            get
            {
                return $"워크스페이스 복원 완료: {RestoredWindows}/{TotalApps}개 창 이동, {LaunchedApps}개 앱 실행, {FailedApps}개 실패";
            }
        }
    }
}
