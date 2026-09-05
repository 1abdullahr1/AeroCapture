using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using AeroCapture.Core.Interop;

namespace AeroCapture.Services;

public class TrayIconManager : IDisposable
{
    private const int WM_USER = 0x0400;
    private const int WM_TRAYICON = WM_USER + 101;
    private const int TRAY_ICON_ID = 1;

    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_LBUTTONUP = 0x0202;

    public event EventHandler? OpenRequested;
    public event EventHandler? CaptureRegionRequested;
    public event EventHandler? CaptureFullRequested;
    public event EventHandler? RecordToggleRequested;
    public event EventHandler? SettingsRequested;
    public event EventHandler? ExitRequested;

    private IntPtr _hwnd = IntPtr.Zero;
    private bool _isAdded = false;

    public void Initialize(IntPtr hwnd)
    {
        _hwnd = hwnd;
        AddTrayIcon();
    }

    private void AddTrayIcon()
    {
        if (_hwnd == IntPtr.Zero || _isAdded) return;

        var nid = new Win32.NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<Win32.NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = TRAY_ICON_ID,
            uFlags = Win32.NIF_ICON | Win32.NIF_MESSAGE | Win32.NIF_TIP,
            uCallbackMessage = WM_TRAYICON,
            hIcon = GetDefaultAppIcon(),
            szTip = "AeroCapture - Screen Recorder & Screenshot Studio"
        };

        _isAdded = Win32.Shell_NotifyIcon(Win32.NIM_ADD, ref nid);
    }

    public void ShowNotification(string title, string message)
    {
        if (_hwnd == IntPtr.Zero) return;

        var nid = new Win32.NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<Win32.NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = TRAY_ICON_ID,
            uFlags = Win32.NIF_INFO,
            szInfo = message,
            szInfoTitle = title,
            dwInfoFlags = 1 /* NIIF_INFO */
        };

        Win32.Shell_NotifyIcon(Win32.NIM_MODIFY, ref nid);
    }

    public void RemoveTrayIcon()
    {
        if (!_isAdded || _hwnd == IntPtr.Zero) return;

        var nid = new Win32.NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<Win32.NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = TRAY_ICON_ID
        };

        Win32.Shell_NotifyIcon(Win32.NIM_DELETE, ref nid);
        _isAdded = false;
    }

    public bool ProcessWindowMessage(uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_TRAYICON)
        {
            int eventId = lParam.ToInt32();
            if (eventId == WM_LBUTTONUP || eventId == WM_LBUTTONDBLCLK)
            {
                OpenRequested?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else if (eventId == WM_RBUTTONUP)
            {
                ShowContextMenu();
                return true;
            }
        }
        return false;
    }

    private void ShowContextMenu()
    {
        Win32.GetCursorPos(out Win32.POINT pt);
        IntPtr hMenu = CreatePopupMenu();

        AppendMenu(hMenu, 0, 1, "Open AeroCapture");
        AppendMenu(hMenu, 0x0800 /* MF_SEPARATOR */, 0, string.Empty);
        AppendMenu(hMenu, 0, 2, "📸 Capture Region");
        AppendMenu(hMenu, 0, 3, "🖥️ Capture Full Screen");
        AppendMenu(hMenu, 0, 4, "🎥 Record Screen");
        AppendMenu(hMenu, 0x0800, 0, string.Empty);
        AppendMenu(hMenu, 0, 5, "⚙️ Settings");
        AppendMenu(hMenu, 0x0800, 0, string.Empty);
        AppendMenu(hMenu, 0, 6, "Exit");

        Win32.SetForegroundWindow(_hwnd);
        int cmd = TrackPopupMenu(hMenu, 0x0100 /* TPM_RETURNCMD */ | 0x0002 /* TPM_RIGHTBUTTON */, pt.X, pt.Y, 0, _hwnd, IntPtr.Zero);
        DestroyMenu(hMenu);

        switch (cmd)
        {
            case 1:
                OpenRequested?.Invoke(this, EventArgs.Empty);
                break;
            case 2:
                CaptureRegionRequested?.Invoke(this, EventArgs.Empty);
                break;
            case 3:
                CaptureFullRequested?.Invoke(this, EventArgs.Empty);
                break;
            case 4:
                RecordToggleRequested?.Invoke(this, EventArgs.Empty);
                break;
            case 5:
                SettingsRequested?.Invoke(this, EventArgs.Empty);
                break;
            case 6:
                ExitRequested?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    private static IntPtr GetDefaultAppIcon()
    {
        try
        {
            return Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty)?.Handle ?? IntPtr.Zero;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    public void Dispose()
    {
        RemoveTrayIcon();
        GC.SuppressFinalize(this);
    }
}
