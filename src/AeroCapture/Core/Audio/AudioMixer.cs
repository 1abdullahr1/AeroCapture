using System;
using System.Collections.Concurrent;
using System.Threading;
using AeroCapture.Models;

namespace AeroCapture.Core.Audio;

public class AudioMixer : IAudioCapture
{
    private readonly WasapiLoopbackCapture? _systemCapture;
    private readonly WasapiMicCapture? _micCapture;
    private readonly float _systemVolume;
    private readonly float _micVolume;

    private readonly ConcurrentQueue<byte[]> _systemQueue = new();
    private readonly ConcurrentQueue<byte[]> _micQueue = new();

    private Thread? _mixerThread;
    private volatile bool _isRunning;

    public int SampleRate => 48000;
    public int Channels => 2;
    public int BitsPerSample => 16;
    public bool IsRunning => _isRunning;

    public event EventHandler<AudioDataEventArgs>? DataAvailable;
    public event EventHandler<AudioLevelEventArgs>? LevelChanged;

    public AudioMixer(
        bool recordSystem, 
        bool recordMic, 
        string systemDeviceId = "", 
        string micDeviceId = "",
        float systemVolume = 1.0f,
        float micVolume = 1.0f)
    {
        _systemVolume = Math.Clamp(systemVolume, 0.0f, 2.0f);
        _micVolume = Math.Clamp(micVolume, 0.0f, 2.0f);

        if (recordSystem)
        {
            _systemCapture = new WasapiLoopbackCapture(systemDeviceId);
            _systemCapture.DataAvailable += OnSystemAudioAvailable;
        }

        if (recordMic)
        {
            _micCapture = new WasapiMicCapture(micDeviceId);
            _micCapture.DataAvailable += OnMicAudioAvailable;
        }
    }

    private void OnSystemAudioAvailable(object? sender, AudioDataEventArgs e)
    {
        byte[] copy = new byte[e.Length];
        Array.Copy(e.Buffer, copy, e.Length);
        _systemQueue.Enqueue(copy);
    }

    private void OnMicAudioAvailable(object? sender, AudioDataEventArgs e)
    {
        byte[] copy = new byte[e.Length];
        Array.Copy(e.Buffer, copy, e.Length);
        _micQueue.Enqueue(copy);
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        _systemCapture?.Start();
        _micCapture?.Start();

        _mixerThread = new Thread(MixerLoop)
        {
            IsBackground = true,
            Name = "AudioMixer_Thread",
            Priority = ThreadPriority.AboveNormal
        };
        _mixerThread.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        _systemCapture?.Stop();
        _micCapture?.Stop();

        _mixerThread?.Join(1000);
        _mixerThread = null;
    }

    private void MixerLoop()
    {
        // 20ms frame at 48000Hz stereo 16-bit = 48000 * 2 channels * 2 bytes * 0.02s = 3840 bytes = 960 samples/channel
        const int frameBytes = 3840;
        byte[] mixedBytes = new byte[frameBytes];

        while (_isRunning)
        {
            Thread.Sleep(20);

            float currentSysPeak = 0.0f;
            float currentMicPeak = 0.0f;

            _systemQueue.TryDequeue(out byte[]? sysBuffer);
            _micQueue.TryDequeue(out byte[]? micBuffer);

            if (sysBuffer == null && micBuffer == null)
            {
                // Silence frame
                Array.Clear(mixedBytes, 0, frameBytes);
                DataAvailable?.Invoke(this, new AudioDataEventArgs(mixedBytes, frameBytes, 0f));
                LevelChanged?.Invoke(this, new AudioLevelEventArgs(0f, 0f));
                continue;
            }

            unsafe
            {
                fixed (byte* outPtr = mixedBytes)
                {
                    short* outSamples = (short*)outPtr;
                    int totalSamples = frameBytes / 2;

                    for (int i = 0; i < totalSamples; i++)
                    {
                        float sum = 0.0f;

                        if (sysBuffer != null && i * 2 + 1 < sysBuffer.Length)
                        {
                            short s = BitConverter.ToInt16(sysBuffer, i * 2);
                            float sf = (s / 32768.0f) * _systemVolume;
                            currentSysPeak = Math.Max(currentSysPeak, Math.Abs(sf));
                            sum += sf;
                        }

                        if (micBuffer != null && i * 2 + 1 < micBuffer.Length)
                        {
                            short m = BitConverter.ToInt16(micBuffer, i * 2);
                            float mf = (m / 32768.0f) * _micVolume;
                            currentMicPeak = Math.Max(currentMicPeak, Math.Abs(mf));
                            sum += mf;
                        }

                        // Soft saturation limiter to prevent digital wrap-around clipping
                        float limited = SoftLimit(sum);
                        outSamples[i] = (short)(limited * 32767.0f);
                    }
                }
            }

            DataAvailable?.Invoke(this, new AudioDataEventArgs(mixedBytes, frameBytes, Math.Max(currentSysPeak, currentMicPeak)));
            LevelChanged?.Invoke(this, new AudioLevelEventArgs(currentSysPeak, currentMicPeak));
        }
    }

    private static float SoftLimit(float x)
    {
        if (x > 1.0f) return (float)Math.Tanh(x);
        if (x < -1.0f) return (float)Math.Tanh(x);
        return x;
    }

    public void Dispose()
    {
        Stop();
        _systemCapture?.Dispose();
        _micCapture?.Dispose();
        GC.SuppressFinalize(this);
    }
}
