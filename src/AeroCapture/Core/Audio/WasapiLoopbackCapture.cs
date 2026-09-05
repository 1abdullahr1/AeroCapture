using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using AeroCapture.Core.Interop;

namespace AeroCapture.Core.Audio;

public class WasapiLoopbackCapture : IAudioCapture
{
    private Thread? _captureThread;
    private volatile bool _isRunning;
    private readonly string _deviceId;

    public int SampleRate { get; private set; } = 48000;
    public int Channels { get; private set; } = 2;
    public int BitsPerSample => 16;
    public bool IsRunning => _isRunning;

    public event EventHandler<AudioDataEventArgs>? DataAvailable;

    public WasapiLoopbackCapture(string deviceId = "")
    {
        _deviceId = deviceId;
    }

    public void Start()
    {
        if (_isRunning) return;

        _isRunning = true;
        _captureThread = new Thread(CaptureLoop)
        {
            IsBackground = true,
            Name = "WASAPI_Loopback_Thread",
            Priority = ThreadPriority.AboveNormal
        };
        _captureThread.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        _captureThread?.Join(1000);
        _captureThread = null;
    }

    private void CaptureLoop()
    {
        WasapiInterop.IMMDevice? device = null;
        WasapiInterop.IAudioClient? audioClient = null;
        WasapiInterop.IAudioCaptureClient? captureClient = null;
        IntPtr pFormat = IntPtr.Zero;

        try
        {
            var enumerator = (WasapiInterop.IMMDeviceEnumerator)new WasapiInterop.MMDeviceEnumerator();
            int hr;

            if (string.IsNullOrEmpty(_deviceId))
            {
                hr = enumerator.GetDefaultAudioEndpoint(WasapiInterop.EDataFlow.eRender, WasapiInterop.ERole.eMultimedia, out device);
            }
            else
            {
                hr = enumerator.GetDevice(_deviceId, out device);
            }

            if (hr != 0 || device == null)
            {
                Debug.WriteLine("[WasapiLoopback] Failed to get audio endpoint.");
                RunSilentLoop();
                return;
            }

            var guidAudioClient = WasapiInterop.IID_IAudioClient;
            hr = device.Activate(ref guidAudioClient, 1 /* CLSCTX_INPROC_SERVER */, IntPtr.Zero, out object clientObj);
            if (hr != 0 || clientObj == null)
            {
                Debug.WriteLine("[WasapiLoopback] Failed to activate audio client.");
                RunSilentLoop();
                return;
            }

            audioClient = (WasapiInterop.IAudioClient)clientObj;
            hr = audioClient.GetMixFormat(out pFormat);
            if (hr != 0 || pFormat == IntPtr.Zero)
            {
                RunSilentLoop();
                return;
            }

            var waveFormat = Marshal.PtrToStructure<WasapiInterop.WAVEFORMATEX>(pFormat);
            SampleRate = (int)waveFormat.nSamplesPerSec;
            Channels = waveFormat.nChannels;
            bool isFloat = waveFormat.wFormatTag == WasapiInterop.WAVE_FORMAT_IEEE_FLOAT ||
                           (waveFormat.wFormatTag == WasapiInterop.WAVE_FORMAT_EXTENSIBLE &&
                            Marshal.PtrToStructure<WasapiInterop.WAVEFORMATEXTENSIBLE>(pFormat).SubFormat == WasapiInterop.KSDATAFORMAT_SUBTYPE_IEEE_FLOAT);

            long hnsBufferDuration = 10000000; // 1 second
            var sessionGuid = Guid.Empty;
            hr = audioClient.Initialize(
                WasapiInterop.AUDCLNT_SHAREMODE_SHARED,
                WasapiInterop.AUDCLNT_STREAMFLAGS_LOOPBACK,
                hnsBufferDuration,
                0,
                pFormat,
                ref sessionGuid);

            if (hr != 0)
            {
                RunSilentLoop();
                return;
            }

            var guidCaptureClient = WasapiInterop.IID_IAudioCaptureClient;
            hr = audioClient.GetService(ref guidCaptureClient, out object captureObj);
            if (hr != 0 || captureObj == null)
            {
                RunSilentLoop();
                return;
            }

            captureClient = (WasapiInterop.IAudioCaptureClient)captureObj;
            audioClient.Start();

            byte[] pcmBuffer = new byte[8192 * 4];

            while (_isRunning)
            {
                Thread.Sleep(10);

                hr = captureClient.GetNextPacketSize(out uint packetSize);
                if (hr != 0) break;

                if (packetSize == 0)
                {
                    // Generate small silence frame to prevent desync
                    continue;
                }

                while (packetSize > 0)
                {
                    hr = captureClient.GetBuffer(out IntPtr pData, out uint numFramesToRead, out uint dwFlags, out _, out _);
                    if (hr != 0) break;

                    int bytesToRead = (int)(numFramesToRead * waveFormat.nBlockAlign);
                    float peak = 0.0f;

                    if ((dwFlags & WasapiInterop.AUDCLNT_BUFFERFLAGS_SILENT) != 0 || bytesToRead == 0)
                    {
                        // Output silence
                        int outBytes = (int)(numFramesToRead * Channels * 2);
                        if (pcmBuffer.Length < outBytes) pcmBuffer = new byte[outBytes];
                        Array.Clear(pcmBuffer, 0, outBytes);
                        DataAvailable?.Invoke(this, new AudioDataEventArgs(pcmBuffer, outBytes, 0f));
                    }
                    else
                    {
                        // Read samples and convert to 16-bit PCM
                        int outBytes = (int)(numFramesToRead * Channels * 2);
                        if (pcmBuffer.Length < outBytes) pcmBuffer = new byte[outBytes];

                        if (isFloat)
                        {
                            unsafe
                            {
                                float* src = (float*)pData;
                                fixed (byte* dstBytes = pcmBuffer)
                                {
                                    short* dst = (short*)dstBytes;
                                    int sampleCount = (int)(numFramesToRead * Channels);
                                    for (int i = 0; i < sampleCount; i++)
                                    {
                                        float val = src[i];
                                        float abs = Math.Abs(val);
                                        if (abs > peak) peak = abs;
                                        short sample = (short)Math.Clamp(val * 32767f, -32768f, 32767f);
                                        dst[i] = sample;
                                    }
                                }
                            }
                        }
                        else
                        {
                            Marshal.Copy(pData, pcmBuffer, 0, bytesToRead);
                            // Estimate peak from 16-bit
                            unsafe
                            {
                                fixed (byte* ptr = pcmBuffer)
                                {
                                    short* sPtr = (short*)ptr;
                                    int count = bytesToRead / 2;
                                    for (int i = 0; i < count; i++)
                                    {
                                        float abs = Math.Abs(sPtr[i]) / 32768f;
                                        if (abs > peak) peak = abs;
                                    }
                                }
                            }
                        }

                        DataAvailable?.Invoke(this, new AudioDataEventArgs(pcmBuffer, outBytes, peak));
                    }

                    captureClient.ReleaseBuffer(numFramesToRead);
                    hr = captureClient.GetNextPacketSize(out packetSize);
                    if (hr != 0) break;
                }
            }

            audioClient.Stop();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WasapiLoopback] Exception: {ex.Message}");
            RunSilentLoop();
        }
        finally
        {
            if (pFormat != IntPtr.Zero) Marshal.FreeCoTaskMem(pFormat);
        }
    }

    private void RunSilentLoop()
    {
        // Generates periodic silence to ensure FFmpeg audio pipeline always receives continuous timestamps
        byte[] silence = new byte[1920 * 4]; // 20ms of 48kHz stereo 16-bit
        while (_isRunning)
        {
            Thread.Sleep(20);
            DataAvailable?.Invoke(this, new AudioDataEventArgs(silence, silence.Length, 0.0f));
        }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
