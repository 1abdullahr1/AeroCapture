using System;
using System.Drawing;

namespace AeroCapture.Models;

public class WindowInfo
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public Rectangle Bounds { get; set; }
    public bool IsMinimized { get; set; }
    public string DisplayText => string.IsNullOrWhiteSpace(Title) ? ProcessName : $"{Title} ({ProcessName})";

    public override string ToString() => DisplayText;
}

public class MonitorInfo
{
    public IntPtr Handle { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public Rectangle Bounds { get; set; }
    public Rectangle WorkingArea { get; set; }
    public bool IsPrimary { get; set; }
    public int Index { get; set; }

    public string DisplayName => IsPrimary 
        ? $"Display {Index + 1} (Primary) - {Bounds.Width}x{Bounds.Height}" 
        : $"Display {Index + 1} - {Bounds.Width}x{Bounds.Height}";

    public override string ToString() => DisplayName;
}
