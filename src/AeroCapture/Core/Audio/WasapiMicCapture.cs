using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using AeroCapture.Core.Interop;

namespace AeroCapture.Core.Audio;

public class WasapiMicCapture : IAudioCapture
{
    private Thread? _captureThread;
    private volatile bool _isRunning;
    private readonly string _deviceId;

    public int SampleRate { get; private set; } = 48000;
    public int Channels { get; private set; } = 2;
    public int BitsPerSample => 16;
    public bool IsRunning => _isRunning;

    public event EventHandler<AudioDataEventArgs>? DataAvailable;

    public WasapiMicCapture(string deviceId = "")
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
            Name = "WASAPI_Mic_Thread",
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
                hr = enumerator.GetDefaultAudioEndpoint(WasapiInterop.EDataFlow.eCapture, WasapiInterop.ERole.eCommunications, out device);
            }
            else
            {
                hr = enumerator.GetDevice(_deviceId, out device);
            }

            if (hr != 0 || device == null)
            {
                Debug.WriteLine("[WasapiMic] Failed to get mic endpoint.");
                return;
            }

            var guidAudioClient = WasapiInterop.IID_IAudioClient;
            hr = device.Activate(ref guidAudioClient, 1 /* CLSCTX_INPROC_SERVER */, IntPtr.Zero, out object clientObj);
            if (hr != 0 || clientObj == null) return;

            audioClient = (WasapiInterop.IAudioClient)clientObj;
            hr = audioClient.GetMixFormat(out pFormat);
            if (hr != 0 || pFormat == IntPtr.Zero) return;

            var waveFormat = Marshal.PtrToStructure<WasapiInterop.WAVEFORMATEX>(pFormat);
            SampleRate = (int)waveFormat.nSamplesPerSec;
            Channels = waveFormat.nChannels;
            bool isFloat = waveFormat.wFormatTag == WasapiInterop.WAVE_FORMAT_IEEE_FLOAT ||
                           (waveFormat.wFormatTag == WasapiInterop.WAVE_FORMAT_EXTENSIBLE &&
                            Marshal.PtrToStructure<WasapiInterop.WAVEFORMATEXTENSIBLE>(pFormat).SubFormat == WasapiInterop.KSDATAFORMAT_SUBTYPE_IEEE_FLOAT);

            long hnsBufferDuration = 10000000;
            var sessionGuid = Guid.Empty;
            hr = audioClient.Initialize(
                WasapiInterop.AUDCLNT_SHAREMODE_SHARED,
                0, // regular capture
                hnsBufferDuration,
                0,
                pFormat,
                ref sessionGuid);

            if (hr != 0) return;

            var guidCaptureClient = WasapiInterop.IID_IAudioCaptureClient;
            hr = audioClient.GetService(ref guidCaptureClient, out object captureObj);
            if (hr != 0 || captureObj == null) return;

            captureClient = (WasapiInterop.IAudioCaptureClient)captureObj;
            audioClient.Start();

            byte[] pcmBuffer = new byte[8192 * 4];

            while (_isRunning)
            {
                Thread.Sleep(10);

                hr = captureClient.GetNextPacketSize(out uint packetSize);
                if (hr != 0) break;

                while (packetSize > 0)
                {
                    hr = captureClient.GetBuffer(out IntPtr pData, out uint numFramesToRead, out uint dwFlags, out _, out _);
                    if (hr != 0) break;

                    float peak = 0.0f;
                    int outBytes = (int)(numFramesToRead * Channels * 2);
                    if (pcmBuffer.Length < outBytes) pcmBuffer = new byte[outBytes];

                    if ((dwFlags & WasapiInterop.AUDCLNT_BUFFERFLAGS_SILENT) != 0 || numFramesToRead == 0)
                    {
                        Array.Clear(pcmBuffer, 0, outBytes);
                        DataAvailable?.Invoke(this, new AudioDataEventArgs(pcmBuffer, outBytes, 0f));
                    }
                    else
                    {
                        if (isFloat)
                        {
                            unsafe
                            {
                                float* src = (float*)pData;
                                fixed (byte* dstBytes = pcmBuffer)
                                {
                                    short* dst = (short*)dstBytes;
                                    int count = (int)(numFramesToRead * Channels);
                                    for (int i = 0; i < count; i++)
                                    {
                                        float val = src[i];
                                        float abs = Math.Abs(val);
                                        if (abs > peak) peak = abs;
                                        dst[i] = (short)Math.Clamp(val * 32767f, -32768f, 32767f);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Marshal.Copy(pData, pcmBuffer, 0, outBytes);
                            unsafe
                            {
                                fixed (byte* ptr = pcmBuffer)
                                {
                                    short* sPtr = (short*)ptr;
                                    int count = outBytes / 2;
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
            Debug.WriteLine($"[WasapiMic] Exception: {ex.Message}");
        }
        finally
        {
            if (pFormat != IntPtr.Zero) Marshal.FreeCoTaskMem(pFormat);
        }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
