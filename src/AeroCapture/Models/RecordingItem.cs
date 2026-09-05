using System;
using System.IO;

namespace AeroCapture.Models;

public class RecordingItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FilePath { get; set; } = string.Empty;
    public string FileName => Path.GetFileName(FilePath);
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public TimeSpan Duration { get; set; }
    public long FileSizeBytes { get; set; }
    public string Resolution { get; set; } = string.Empty;
    public string ContainerFormat { get; set; } = "MP4";

    public string FormattedDuration => Duration.Hours > 0 
        ? Duration.ToString(@"hh\:mm\:ss") 
        : Duration.ToString(@"mm\:ss");

    public string FormattedFileSize => FormatBytes(FileSizeBytes);

    public string FormattedDate => CreatedAt.ToString("MMM dd, yyyy  hh:mm tt");

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }
}

public class ScreenshotItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FilePath { get; set; } = string.Empty;
    public string FileName => Path.GetFileName(FilePath);
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public long FileSizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Resolution => $"{Width} × {Height}";

    public string FormattedFileSize => FormatBytes(FileSizeBytes);
    public string FormattedDate => CreatedAt.ToString("MMM dd, yyyy  hh:mm tt");

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }
}

public class AudioLevelEventArgs : EventArgs
{
    public float SystemAudioLevel { get; }
    public float MicrophoneLevel { get; }

    public AudioLevelEventArgs(float systemAudioLevel, float microphoneLevel)
    {
        SystemAudioLevel = Math.Clamp(systemAudioLevel, 0.0f, 1.0f);
        MicrophoneLevel = Math.Clamp(microphoneLevel, 0.0f, 1.0f);
    }
}
