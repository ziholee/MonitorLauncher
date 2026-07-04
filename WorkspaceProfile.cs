using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonitorLauncher
{
    public class WorkspaceProfile
    {
        public string Name { get; set; } = string.Empty;
        public List<AppWindowProfile> Apps { get; set; } = new List<AppWindowProfile>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public override string ToString()
        {
            return $"{Name} ({Apps.Count}개 창)";
        }

        public static void SaveWorkspaces(List<WorkspaceProfile> workspaces, string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                var json = JsonSerializer.Serialize(workspaces, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"워크스페이스 저장 실패: {ex.Message}", ex);
            }
        }

        public static List<WorkspaceProfile> LoadWorkspaces(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return new List<WorkspaceProfile>();

                var json = File.ReadAllText(filePath);
                var workspaces = JsonSerializer.Deserialize<List<WorkspaceProfile>>(json);
                return workspaces ?? new List<WorkspaceProfile>();
            }
            catch (Exception ex)
            {
                throw new Exception($"워크스페이스 로드 실패: {ex.Message}", ex);
            }
        }

        public static string GetWorkspacesFilePath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appData, "MonitorLauncher");
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, "workspaces.json");
        }
    }
}
