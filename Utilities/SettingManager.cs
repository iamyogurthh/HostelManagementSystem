using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace HostelManagementSystem.Helpers
{
    public class AppConfig
    {
        public bool IsDarkMode { get; set; } = false;
        // Add future settings here...
    }

    public static class SettingsManager
    {
        private static readonly string FolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HostelManagementSystem");

        private static readonly string FilePath = Path.Combine(FolderPath, "settings.json");

        public static AppConfig Config { get; private set; } = new AppConfig();

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    Config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                // Log failure to Output/Debug window
                Debug.WriteLine($"[SettingsManager] Failed to load settings: {ex.Message}");

                // Fallback to safe defaults so the app still runs smoothly
                Config = new AppConfig();
            }
        }

        public static async void SaveAsync()
        {
            try
            {
                if (!Directory.Exists(FolderPath))
                {
                    Directory.CreateDirectory(FolderPath);
                }

                string json = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(FilePath, json);
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"[SettingsManager] File permission error: {ex.Message}");
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"[SettingsManager] Disk access/locking error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsManager] Unexpected save failure: {ex.Message}");
            }
        }
    }
}