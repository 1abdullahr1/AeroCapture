using System;
using System.IO;

namespace AeroCapture.Models;

public enum ScreenshotFormat
{
    Png,
    Jpeg,
    WebP,
    Bmp
}

public class AppSettings
{
    public string RecordingsDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "AeroCapture");

    public string ScreenshotsDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "AeroCapture");

    public ScreenshotFormat ScreenshotFormat { get; set; } = ScreenshotFormat.Png;
    public int JpegQuality { get; set; } = 90;

    public bool AutoCopyToClipboard { get; set; } = true;
    public bool OpenInAnnotationEditor { get; set; } = true;
    public bool AutoSaveScreenshots { get; set; } = true;
    public bool MinimizeToTrayOnClose { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
    public bool ShowCountdownBeforeRecording { get; set; } = true;
    public int CountdownSeconds { get; set; } = 3;

    // Hotkey configurations (virtual key + modifiers)
    public string HotkeyScreenshotRegion { get; set; } = "PrintScreen";
    public string HotkeyScreenshotFull { get; set; } = "Ctrl+PrintScreen";
    public string HotkeyScreenshotWindow { get; set; } = "Alt+PrintScreen";
    public string HotkeyRecordToggle { get; set; } = "Ctrl+Shift+R";
    public string HotkeyRecordPause { get; set; } = "Ctrl+Shift+P";

    // Recording default profile
    public RecordingProfile DefaultProfile { get; set; } = new();
}
