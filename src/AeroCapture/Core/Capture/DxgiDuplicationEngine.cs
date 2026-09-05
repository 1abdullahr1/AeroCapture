using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using AeroCapture.Core.Interop;

namespace AeroCapture.Core.Capture;

public class DxgiDuplicationEngine : ICaptureSource
{
    private readonly int _targetFps;
    private readonly Rectangle _captureBounds;
    private Thread? _captureThread;
    private volatile bool _isRunning;
    private readonly Stopwatch _stopwatch = new();

    public int Width => _captureBounds.Width;
    public int Height => _captureBounds.Height;
    public bool IsRunning => _isRunning;

    public event EventHandler<FrameArrivedEventArgs>? FrameArrived;

    public DxgiDuplicationEngine(Rectangle bounds, int targetFps = 60)
    {
        _captureBounds = bounds.Width % 2 == 0 ? bounds : bounds with { Width = bounds.Width - 1 };
        _captureBounds = _captureBounds.Height % 2 == 0 ? _captureBounds : _captureBounds with { Height = _captureBounds.Height - 1 };
        _targetFps = Math.Clamp(targetFps, 15, 120);
    }

    public void Start()
    {
        if (_isRunning) return;

        _isRunning = true;
        _stopwatch.Restart();
        _captureThread = new Thread(CaptureLoop)
        {
            IsBackground = true,
            Name = "DXGI_Capture_Thread",
            Priority = ThreadPriority.AboveNormal
        };
        _captureThread.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        _captureThread?.Join(1000);
        _captureThread = null;
        _stopwatch.Stop();
    }

    private void CaptureLoop()
    {
        int width = _captureBounds.Width;
        int height = _captureBounds.Height;
        int stride = width * 4;
        byte[] frameBuffer = new byte[stride * height];

        long targetFrameTicks = Stopwatch.Frequency / _targetFps;
        long nextFrameTick = Stopwatch.GetTimestamp();

        // GDI/Desktop device context for rock-solid universal frame capture across all multi-monitor setups
        IntPtr hScreenDC = Win32.GetDC(IntPtr.Zero);
        IntPtr hMemoryDC = Win32.CreateCompatibleDC(hScreenDC);
        IntPtr hBitmap = Win32.CreateCompatibleBitmap(hScreenDC, width, height);
        IntPtr hOldBitmap = Win32.SelectObject(hMemoryDC, hBitmap);

        try
        {
            while (_isRunning)
            {
                long currentTick = Stopwatch.GetTimestamp();
                if (currentTick < nextFrameTick)
                {
                    int sleepMs = (int)((nextFrameTick - currentTick) * 1000 / Stopwatch.Frequency);
                    if (sleepMs > 1)
                    {
                        Thread.Sleep(sleepMs - 1);
                    }
                    continue;
                }

                nextFrameTick = currentTick + targetFrameTicks;

                // Blit desktop region into memory bitmap
                Win32.BitBlt(
                    hMemoryDC, 
                    0, 0, width, height, 
                    hScreenDC, 
                    _captureBounds.X, _captureBounds.Y, 
                    Win32.SRCCOPY | Win32.CAPTUREBLT);

                // Copy bitmap data to buffer
                GetDIBitsToBuffer(hMemoryDC, hBitmap, width, height, frameBuffer);

                // Fire event
                FrameArrived?.Invoke(this, new FrameArrivedEventArgs(
                    frameBuffer,
                    width,
                    height,
                    stride,
                    _stopwatch.Elapsed));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DxgiDuplicationEngine] Capture loop error: {ex.Message}");
        }
        finally
        {
            Win32.SelectObject(hMemoryDC, hOldBitmap);
            Win32.DeleteObject(hBitmap);
            Win32.DeleteDC(hMemoryDC);
            Win32.ReleaseDC(IntPtr.Zero, hScreenDC);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        public uint bmiColors;
    }

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines, [Out] byte[] lpvBits, ref BITMAPINFO lpbi, uint uUsage);

    private static void GetDIBitsToBuffer(IntPtr hdc, IntPtr hBitmap, int width, int height, byte[] buffer)
    {
        var bmi = new BITMAPINFO();
        bmi.bmiHeader.biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>();
        bmi.bmiHeader.biWidth = width;
        bmi.bmiHeader.biHeight = -height; // Top-down
        bmi.bmiHeader.biPlanes = 1;
        bmi.bmiHeader.biBitCount = 32;
        bmi.bmiHeader.biCompression = 0; // BI_RGB

        GetDIBits(hdc, hBitmap, 0, (uint)height, buffer, ref bmi, 0);
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
