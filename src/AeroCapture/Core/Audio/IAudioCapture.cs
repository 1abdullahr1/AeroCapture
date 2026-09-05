using System;

namespace AeroCapture.Core.Audio;

public class AudioDataEventArgs : EventArgs
{
    public byte[] Buffer { get; }
    public int Length { get; }
    public float PeakLevel { get; }

    public AudioDataEventArgs(byte[] buffer, int length, float peakLevel)
    {
        Buffer = buffer;
        Length = length;
        PeakLevel = peakLevel;
    }
}

public interface IAudioCapture : IDisposable
{
    int SampleRate { get; }
    int Channels { get; }
    int BitsPerSample { get; }
    bool IsRunning { get; }

    event EventHandler<AudioDataEventArgs>? DataAvailable;

    void Start();
    void Stop();
}
