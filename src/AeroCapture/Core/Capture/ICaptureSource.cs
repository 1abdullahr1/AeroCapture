using System;

namespace AeroCapture.Core.Capture;

public class FrameArrivedEventArgs : EventArgs
{
    public byte[] Data { get; }
    public int Width { get; }
    public int Height { get; }
    public int Stride { get; }
    public TimeSpan Timestamp { get; }

    public FrameArrivedEventArgs(byte[] data, int width, int height, int stride, TimeSpan timestamp)
    {
        Data = data;
        Width = width;
        Height = height;
        Stride = stride;
        Timestamp = timestamp;
    }
}

public interface ICaptureSource : IDisposable
{
    int Width { get; }
    int Height { get; }
    bool IsRunning { get; }

    event EventHandler<FrameArrivedEventArgs>? FrameArrived;

    void Start();
    void Stop();
}
