using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using AeroCapture.Core.Interop;

namespace AeroCapture.Services;

public class HotKeyManager : IDisposable
{
    private const int HOTKEY_SCREENSHOT_REGION = 1001;
    private const int HOTKEY_SCREENSHOT_FULL = 1002;
    private const int HOTKEY_SCREENSHOT_WINDOW = 1003;
    private const int HOTKEY_RECORD_TOGGLE = 1004;
    private const int HOTKEY_RECORD_PAUSE = 1005;

    public event EventHandler? ScreenshotRegionRequested;
    public event EventHandler? ScreenshotFullRequested;
    public event EventHandler? ScreenshotWindowRequested;
    public event EventHandler? RecordToggleRequested;
    public event EventHandler? RecordPauseRequested;

    private Thread? _messageThread;
    private IntPtr _hwnd = IntPtr.Zero;
    private volatile bool _isRunning;
    private readonly AutoResetEvent _initEvent = new(false);

    public void Start()
    {
        if (_isRunning) return;

        _isRunning = true;
        _messageThread = new Thread(MessageLoop)
        {
            IsBackground = true,
            Name = "AeroCapture_HotKey_Thread"
        };
        _messageThread.Start();
        _initEvent.WaitOne(2000);
    }

    public void Stop()
    {
        _isRunning = false;
        if (_hwnd != IntPtr.Zero)
        {
            PostMessage(_hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            _messageThread?.Join(1000);
            _hwnd = IntPtr.Zero;
        }
    }

    private void MessageLoop()
    {
        var wndClass = new WNDCLASSEX
        {
            cbSize = Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc = WndProc,
            lpszClassName = "AeroCapture_HotKey_Receiver_" + Guid.NewGuid().ToString("N")
        };

        ushort atom = RegisterClassEx(ref wndClass);
        if (atom == 0)
        {
            _initEvent.Set();
            return;
        }

        _hwnd = CreateWindowEx(0, wndClass.lpszClassName, "AeroCaptureHotKeys", 0, 0, 0, 0, 0, HWND_MESSAGE, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        RegisterHotKeys(_hwnd);
        _initEvent.Set();

        while (_isRunning && GetMessage(out MSG msg, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        UnregisterHotKeys(_hwnd);
        DestroyWindow(_hwnd);
        UnregisterClass(wndClass.lpszClassName, IntPtr.Zero);
    }

    private void RegisterHotKeys(IntPtr hwnd)
    {
        var settings = SettingsManager.Instance.Settings;

        // PrintScreen for Region
        RegisterKey(hwnd, HOTKEY_SCREENSHOT_REGION, settings.HotkeyScreenshotRegion, 0x2C /* VK_SNAPSHOT */, 0);

        // Ctrl + PrintScreen for Fullscreen
        RegisterKey(hwnd, HOTKEY_SCREENSHOT_FULL, settings.HotkeyScreenshotFull, 0x2C /* VK_SNAPSHOT */, Win32.MOD_CONTROL);

        // Alt + PrintScreen for Window
        RegisterKey(hwnd, HOTKEY_SCREENSHOT_WINDOW, settings.HotkeyScreenshotWindow, 0x2C /* VK_SNAPSHOT */, Win32.MOD_ALT);

        // Ctrl + Shift + R for Recording Toggle
        RegisterKey(hwnd, HOTKEY_RECORD_TOGGLE, settings.HotkeyRecordToggle, 0x52 /* 'R' */, Win32.MOD_CONTROL | Win32.MOD_SHIFT);

        // Ctrl + Shift + P for Pause / Resume
        RegisterKey(hwnd, HOTKEY_RECORD_PAUSE, settings.HotkeyRecordPause, 0x50 /* 'P' */, Win32.MOD_CONTROL | Win32.MOD_SHIFT);
    }

    private static void RegisterKey(IntPtr hwnd, int id, string config, uint defaultVk, uint defaultMod)
    {
        uint mod = defaultMod;
        uint vk = defaultVk;

        if (!string.IsNullOrEmpty(config))
        {
            ParseHotkeyString(config, out mod, out vk);
        }

        Win32.RegisterHotKey(hwnd, id, mod | Win32.MOD_NOREPEAT, vk);
    }

    private static void ParseHotkeyString(string str, out uint mod, out uint vk)
    {
        mod = 0;
        vk = 0x2C; // Default snapshot

        string[] parts = str.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            if (p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || p.Equals("Control", StringComparison.OrdinalIgnoreCase))
                mod |= Win32.MOD_CONTROL;
            else if (p.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                mod |= Win32.MOD_ALT;
            else if (p.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                mod |= Win32.MOD_SHIFT;
            else if (p.Equals("Win", StringComparison.OrdinalIgnoreCase))
                mod |= Win32.MOD_WIN;
            else if (p.Equals("PrintScreen", StringComparison.OrdinalIgnoreCase) || p.Equals("PrtScn", StringComparison.OrdinalIgnoreCase))
                vk = 0x2C;
            else if (p.Length == 1 && char.IsLetterOrDigit(p[0]))
                vk = (uint)char.ToUpperInvariant(p[0]);
        }
    }

    private void UnregisterHotKeys(IntPtr hwnd)
    {
        Win32.UnregisterHotKey(hwnd, HOTKEY_SCREENSHOT_REGION);
        Win32.UnregisterHotKey(hwnd, HOTKEY_SCREENSHOT_FULL);
        Win32.UnregisterHotKey(hwnd, HOTKEY_SCREENSHOT_WINDOW);
        Win32.UnregisterHotKey(hwnd, HOTKEY_RECORD_TOGGLE);
        Win32.UnregisterHotKey(hwnd, HOTKEY_RECORD_PAUSE);
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == Win32.WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            switch (id)
            {
                case HOTKEY_SCREENSHOT_REGION:
                    ScreenshotRegionRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case HOTKEY_SCREENSHOT_FULL:
                    ScreenshotFullRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case HOTKEY_SCREENSHOT_WINDOW:
                    ScreenshotWindowRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case HOTKEY_RECORD_TOGGLE:
                    RecordToggleRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case HOTKEY_RECORD_PAUSE:
                    RecordPauseRequested?.Invoke(this, EventArgs.Empty);
                    break;
            }
            return IntPtr.Zero;
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        Stop();
        _initEvent.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Win32 Message Window Interop
    private static readonly IntPtr HWND_MESSAGE = new(-3);
    private const uint WM_CLOSE = 0x0010;

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public Win32.POINT pt;
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct WNDCLASSEX
    {
        public int cbSize;
        public uint style;
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public WndProcDelegate lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern sbyte GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage([In] ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
    #endregion
}
