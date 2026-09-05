using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using AeroCapture.Core.Interop;
using AeroCapture.Models;

namespace AeroCapture.Core.Capture;

public class WindowEnumerationService
{
    public static List<MonitorInfo> GetMonitors()
    {
        var monitors = new List<MonitorInfo>();
        int index = 0;

        Win32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdc, ref Win32.RECT rect, IntPtr data) =>
        {
            var mi = new Win32.MONITORINFOEX { cbSize = Marshal.SizeOf<Win32.MONITORINFOEX>() };
            if (Win32.GetMonitorInfo(hMonitor, ref mi))
            {
                monitors.Add(new MonitorInfo
                {
                    Handle = hMonitor,
                    DeviceName = mi.szDevice,
                    Bounds = mi.rcMonitor.ToRectangle(),
                    WorkingArea = mi.rcWork.ToRectangle(),
                    IsPrimary = (mi.dwFlags & 1) != 0,
                    Index = index++
                });
            }
            return true;
        }, IntPtr.Zero);

        return monitors;
    }

    public static Rectangle GetVirtualScreenBounds()
    {
        int x = Win32.GetSystemMetrics(Win32.SM_XVIRTUALSCREEN);
        int y = Win32.GetSystemMetrics(Win32.SM_YVIRTUALSCREEN);
        int w = Win32.GetSystemMetrics(Win32.SM_CXVIRTUALSCREEN);
        int h = Win32.GetSystemMetrics(Win32.SM_CYVIRTUALSCREEN);

        return new Rectangle(x, y, w, h);
    }

    public static List<WindowInfo> GetCapturableWindows()
    {
        var windows = new List<WindowInfo>();
        var currentPid = Environment.ProcessId;

        Win32.EnumWindows((hWnd, lParam) =>
        {
            if (!Win32.IsWindowVisible(hWnd))
                return true;

            var titleBuilder = new StringBuilder(256);
            Win32.GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
            string title = titleBuilder.ToString().Trim();

            if (string.IsNullOrEmpty(title))
                return true;

            Win32.GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid == currentPid)
                return true;

            var bounds = GetAccurateWindowBounds(hWnd);
            if (bounds.Width <= 10 || bounds.Height <= 10)
                return true;

            string processName = "Application";
            try
            {
                using var proc = Process.GetProcessById((int)pid);
                processName = proc.ProcessName;
            }
            catch
            {
                // Ignore process access permissions
            }

            // Exclude common system windows like Program Manager
            if (title == "Program Manager" || title == "Windows Shell Experience Host" || title == "Settings")
            {
                if (title == "Program Manager") return true;
            }

            windows.Add(new WindowInfo
            {
                Handle = hWnd,
                Title = title,
                ProcessName = processName,
                ProcessId = (int)pid,
                Bounds = bounds,
                IsMinimized = bounds.X < -10000 || bounds.Y < -10000
            });

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    public static Rectangle GetAccurateWindowBounds(IntPtr hWnd)
    {
        int hr = Win32.DwmGetWindowAttribute(hWnd, Win32.DWMWA_EXTENDED_FRAME_BOUNDS, out Win32.RECT rect, Marshal.SizeOf<Win32.RECT>());
        if (hr == 0)
        {
            return rect.ToRectangle();
        }

        Win32.GetWindowRect(hWnd, out rect);
        return rect.ToRectangle();
    }

    public static WindowInfo? GetWindowUnderCursor()
    {
        if (!Win32.GetCursorPos(out Win32.POINT pt))
            return null;

        IntPtr hWnd = Win32.WindowFromPoint(pt);
        if (hWnd == IntPtr.Zero)
            return null;

        // Traverse to root top-level window
        IntPtr rootHwnd = hWnd;
        while (true)
        {
            IntPtr parent = GetParentOrOwner(rootHwnd);
            if (parent == IntPtr.Zero)
                break;
            rootHwnd = parent;
        }

        var bounds = GetAccurateWindowBounds(rootHwnd);
        var titleBuilder = new StringBuilder(256);
        Win32.GetWindowText(rootHwnd, titleBuilder, titleBuilder.Capacity);
        string title = titleBuilder.ToString();

        Win32.GetWindowThreadProcessId(rootHwnd, out uint pid);
        string procName = "Application";
        try
        {
            using var proc = Process.GetProcessById((int)pid);
            procName = proc.ProcessName;
        }
        catch { }

        return new WindowInfo
        {
            Handle = rootHwnd,
            Title = title,
            ProcessName = procName,
            ProcessId = (int)pid,
            Bounds = bounds
        };
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    private static IntPtr GetParentOrOwner(IntPtr hwnd)
    {
        const uint GA_ROOT = 2;
        return GetAncestor(hwnd, GA_ROOT);
    }
}
