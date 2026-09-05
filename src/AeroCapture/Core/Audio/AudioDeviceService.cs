using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AeroCapture.Core.Interop;

namespace AeroCapture.Core.Audio;

public class AudioDeviceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public override string ToString() => IsDefault ? $"{Name} (Default)" : Name;
}

public class AudioDeviceService
{
    public static List<AudioDeviceInfo> GetPlaybackDevices()
    {
        return EnumerateDevices(WasapiInterop.EDataFlow.eRender);
    }

    public static List<AudioDeviceInfo> GetRecordingDevices()
    {
        return EnumerateDevices(WasapiInterop.EDataFlow.eCapture);
    }

    private static List<AudioDeviceInfo> EnumerateDevices(WasapiInterop.EDataFlow dataFlow)
    {
        var devices = new List<AudioDeviceInfo>();

        try
        {
            var enumerator = (WasapiInterop.IMMDeviceEnumerator)new WasapiInterop.MMDeviceEnumerator();
            string defaultId = string.Empty;

            int hr = enumerator.GetDefaultAudioEndpoint(dataFlow, WasapiInterop.ERole.eMultimedia, out var defaultDevice);
            if (hr == 0 && defaultDevice != null)
            {
                defaultDevice.GetId(out defaultId);
            }

            devices.Add(new AudioDeviceInfo
            {
                Id = string.Empty,
                Name = dataFlow == WasapiInterop.EDataFlow.eRender ? "Default Output Device" : "Default Microphone",
                IsDefault = true
            });
        }
        catch
        {
            devices.Add(new AudioDeviceInfo
            {
                Id = string.Empty,
                Name = "Default Audio Device",
                IsDefault = true
            });
        }

        return devices;
    }
}
