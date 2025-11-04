using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonitorLauncher
{
    public class Profile
    {
        public string Name { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string MonitorDeviceName { get; set; } = string.Empty;
        public WindowState WindowState { get; set; } = WindowState.Maximized;

        public static void SaveProfiles(List<Profile> profiles, string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                var json = JsonSerializer.Serialize(profiles, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"프로필 저장 실패: {ex.Message}", ex);
            }
        }

        public static List<Profile> LoadProfiles(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return new List<Profile>();

                var json = File.ReadAllText(filePath);
                var profiles = JsonSerializer.Deserialize<List<Profile>>(json);
                return profiles ?? new List<Profile>();
            }
            catch (Exception ex)
            {
                throw new Exception($"프로필 로드 실패: {ex.Message}", ex);
            }
        }

        public static string GetProfilesFilePath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appData, "MonitorLauncher");
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, "profiles.json");
        }
    }
}
