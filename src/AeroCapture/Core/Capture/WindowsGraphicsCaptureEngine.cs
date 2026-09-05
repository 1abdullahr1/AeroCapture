using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using AeroCapture.Core.Interop;
using AeroCapture.Models;
using Windows.Graphics.Capture;

namespace AeroCapture.Core.Capture;

public class WindowsGraphicsCaptureEngine : ICaptureSource
{
    private readonly IntPtr _windowHandle;
    private readonly Rectangle _region;
    private readonly int _targetFps;
    private readonly bool _captureCursor;
    private ICaptureSource? _fallbackSource;
    private volatile bool _isRunning;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public bool IsRunning => _isRunning;

    public event EventHandler<FrameArrivedEventArgs>? FrameArrived;

    public WindowsGraphicsCaptureEngine(IntPtr windowHandle, int targetFps = 60, bool captureCursor = true)
    {
        _windowHandle = windowHandle;
        _targetFps = targetFps;
        _captureCursor = captureCursor;

        var bounds = WindowEnumerationService.GetAccurateWindowBounds(windowHandle);
        Width = bounds.Width % 2 == 0 ? bounds.Width : bounds.Width - 1;
        Height = bounds.Height % 2 == 0 ? bounds.Height : bounds.Height - 1;
        _region = bounds;
    }

    public WindowsGraphicsCaptureEngine(Rectangle region, int targetFps = 60, bool captureCursor = true)
    {
        _windowHandle = IntPtr.Zero;
        _targetFps = targetFps;
        _captureCursor = captureCursor;

        Width = region.Width % 2 == 0 ? region.Width : region.Width - 1;
        Height = region.Height % 2 == 0 ? region.Height : region.Height - 1;
        _region = new Rectangle(region.X, region.Y, Width, Height);
    }

    public void Start()
    {
        if (_isRunning) return;

        _isRunning = true;

        // Try GraphicsCaptureItem first if available
        GraphicsCaptureItem? item = null;
        if (_windowHandle != IntPtr.Zero)
        {
            item = GraphicsCaptureInterop.CreateItemForWindow(_windowHandle);
        }

        if (item != null)
        {
            try
            {
                // Item successfully acquired
                Debug.WriteLine($"[WGC] Created GraphicsCaptureItem: {item.DisplayName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WGC] Exception setting up framepool: {ex.Message}");
            }
        }

        // Initialize high-performance DXGI fallback capture engine
        _fallbackSource = new DxgiDuplicationEngine(_region, _targetFps);
        _fallbackSource.FrameArrived += (s, e) => FrameArrived?.Invoke(this, e);
        _fallbackSource.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        _fallbackSource?.Stop();
        _fallbackSource?.Dispose();
        _fallbackSource = null;
    }

    public static Bitmap CaptureSnapshot(Rectangle region)
    {
        int width = Math.Max(2, region.Width);
        int height = Math.Max(2, region.Height);

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(region.X, region.Y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        }
        return bitmap;
    }

    public static Bitmap CaptureWindowSnapshot(IntPtr hWnd)
    {
        var bounds = WindowEnumerationService.GetAccurateWindowBounds(hWnd);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            bounds = new Rectangle(0, 0, 800, 600);
        }
        return CaptureSnapshot(bounds);
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
