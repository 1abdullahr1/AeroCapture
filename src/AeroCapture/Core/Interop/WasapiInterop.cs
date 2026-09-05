using System;
using System.Runtime.InteropServices;

namespace AeroCapture.Core.Interop;

public static class WasapiInterop
{
    public const uint AUDCLNT_STREAMFLAGS_LOOPBACK = 0x00020000;
    public const int AUDCLNT_SHAREMODE_SHARED = 0;
    public const int AUDCLNT_BUFFERFLAGS_SILENT = 0x2;

    public const int AUDCLNT_E_DEVICE_INVALIDATED = unchecked((int)0x88890004);
    public const int AUDCLNT_S_BUFFER_EMPTY = 0x08890001;

    public const ushort WAVE_FORMAT_PCM = 1;
    public const ushort WAVE_FORMAT_IEEE_FLOAT = 3;
    public const ushort WAVE_FORMAT_EXTENSIBLE = 0xFFFE;

    public static readonly Guid KSDATAFORMAT_SUBTYPE_PCM = new("00000001-0000-0010-8000-00AA00389B71");
    public static readonly Guid KSDATAFORMAT_SUBTYPE_IEEE_FLOAT = new("00000003-0000-0010-8000-00AA00389B71");

    public enum EDataFlow
    {
        eRender = 0,
        eCapture = 1,
        eAll = 2
    }

    public enum ERole
    {
        eConsole = 0,
        eMultimedia = 1,
        eCommunications = 2
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct WAVEFORMATEX
    {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct WAVEFORMATEXTENSIBLE
    {
        public WAVEFORMATEX Format;
        public ushort wValidBitsPerSample;
        public uint dwChannelMask;
        public Guid SubFormat;
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    public class MMDeviceEnumerator
    {
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(EDataFlow dataFlow, uint dwStateMask, out IntPtr ppDevices);

        [PreserveSig]
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);

        [PreserveSig]
        int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);

        [PreserveSig]
        int RegisterEndpointNotificationCallback(IntPtr pClient);

        [PreserveSig]
        int UnregisterEndpointNotificationCallback(IntPtr pClient);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, uint dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

        [PreserveSig]
        int OpenPropertyStore(uint stgmAccess, out IntPtr ppProperties);

        [PreserveSig]
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);

        [PreserveSig]
        int GetState(out uint pdwState);
    }

    [ComImport]
    [Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioClient
    {
        [PreserveSig]
        int Initialize(
            int ShareMode,
            uint StreamFlags,
            long hnsBufferDuration,
            long hnsPeriodicity,
            IntPtr pFormat,
            ref Guid AudioSessionGuid);

        [PreserveSig]
        int GetBufferSize(out uint pNumBufferFrames);

        [PreserveSig]
        int GetStreamLatency(out long phnsLatency);

        [PreserveSig]
        int GetCurrentPadding(out uint pNumPaddingFrames);

        [PreserveSig]
        int IsFormatSupported(int ShareMode, IntPtr pFormat, out IntPtr ppClosestMatch);

        [PreserveSig]
        int GetMixFormat(out IntPtr ppDeviceFormat);

        [PreserveSig]
        int GetDevicePeriod(out long phnsDefaultDevicePeriod, out long phnsMinimumDevicePeriod);

        [PreserveSig]
        int Start();

        [PreserveSig]
        int Stop();

        [PreserveSig]
        int Reset();

        [PreserveSig]
        int SetEventHandle(IntPtr eventHandle);

        [PreserveSig]
        int GetService(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
    }

    [ComImport]
    [Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioCaptureClient
    {
        [PreserveSig]
        int GetBuffer(
            out IntPtr ppData,
            out uint pNumFramesToRead,
            out uint pdwFlags,
            out ulong pu64DevicePosition,
            out ulong pu64QPCPosition);

        [PreserveSig]
        int ReleaseBuffer(uint NumFramesRead);

        [PreserveSig]
        int GetNextPacketSize(out uint pNumFramesInNextPacket);
    }

    public static readonly Guid IID_IAudioClient = new("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
    public static readonly Guid IID_IAudioCaptureClient = new("C8ADBD64-E71E-48a0-A4DE-185C395CD317");
}
