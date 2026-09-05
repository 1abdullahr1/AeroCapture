using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AeroCapture.Models;

namespace AeroCapture.Services;

public class HistoryManager
{
    private static HistoryManager? _instance;
    public static HistoryManager Instance => _instance ??= new HistoryManager();

    public ObservableCollection<RecordingItem> Recordings { get; } = new();
    public ObservableCollection<ScreenshotItem> Screenshots { get; } = new();

    private HistoryManager()
    {
        RefreshHistory();
    }

    public void RefreshHistory()
    {
        var settings = SettingsManager.Instance.Settings;

        // Load Recordings
        Recordings.Clear();
        if (Directory.Exists(settings.RecordingsDirectory))
        {
            var videoFiles = Directory.GetFiles(settings.RecordingsDirectory)
                .Where(f => f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".webm", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime);

            foreach (var fi in videoFiles)
            {
                Recordings.Add(new RecordingItem
                {
                    FilePath = fi.FullName,
                    CreatedAt = fi.CreationTime,
                    FileSizeBytes = fi.Length,
                    ContainerFormat = fi.Extension.TrimStart('.').ToUpperInvariant()
                });
            }
        }

        // Load Screenshots
        Screenshots.Clear();
        if (Directory.Exists(settings.ScreenshotsDirectory))
        {
            var imageFiles = Directory.GetFiles(settings.ScreenshotsDirectory)
                .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime);

            foreach (var fi in imageFiles)
            {
                Screenshots.Add(new ScreenshotItem
                {
                    FilePath = fi.FullName,
                    CreatedAt = fi.CreationTime,
                    FileSizeBytes = fi.Length
                });
            }
        }
    }

    public void AddRecording(RecordingItem item)
    {
        Recordings.Insert(0, item);
    }

    public void AddScreenshot(ScreenshotItem item)
    {
        Screenshots.Insert(0, item);
    }

    public static void OpenFile(string filePath)
    {
        if (!File.Exists(filePath)) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HistoryManager] OpenFile error: {ex.Message}");
        }
    }

    public static void RevealInExplorer(string filePath)
    {
        if (!File.Exists(filePath)) return;
        try
        {
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HistoryManager] RevealInExplorer error: {ex.Message}");
        }
    }

    public void DeleteRecording(RecordingItem item)
    {
        try
        {
            if (File.Exists(item.FilePath))
            {
                File.Delete(item.FilePath);
            }
            Recordings.Remove(item);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HistoryManager] Delete recording error: {ex.Message}");
        }
    }

    public void DeleteScreenshot(ScreenshotItem item)
    {
        try
        {
            if (File.Exists(item.FilePath))
            {
                File.Delete(item.FilePath);
            }
            Screenshots.Remove(item);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HistoryManager] Delete screenshot error: {ex.Message}");
        }
    }
}
