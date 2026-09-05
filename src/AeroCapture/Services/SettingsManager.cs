using System;
using System.IO;
using System.Text.Json;
using AeroCapture.Models;

namespace AeroCapture.Services;

public class SettingsManager
{
    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AeroCapture");
    private static readonly string SettingsFile = Path.Combine(SettingsFolder, "settings.json");

    private static SettingsManager? _instance;
    public static SettingsManager Instance => _instance ??= new SettingsManager();

    public AppSettings Settings { get; private set; }

    private SettingsManager()
    {
        Settings = LoadSettings();
    }

    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                string json = File.ReadAllText(SettingsFile);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    return loaded;
                }
            }
        }
        catch
        {
            // Fallback to default
        }

        var defaultSettings = new AppSettings();
        SaveSettings(defaultSettings);
        return defaultSettings;
    }

    public void SaveSettings(AppSettings? settings = null)
    {
        try
        {
            if (settings != null)
            {
                Settings = settings;
            }

            if (!Directory.Exists(SettingsFolder))
            {
                Directory.CreateDirectory(SettingsFolder);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(Settings, options);
            File.WriteAllText(SettingsFile, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsManager] Save error: {ex.Message}");
        }
    }
}
