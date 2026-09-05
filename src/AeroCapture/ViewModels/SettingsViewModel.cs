using System;
using System.Collections.Generic;
using AeroCapture.Core.Audio;
using AeroCapture.Models;
using AeroCapture.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AeroCapture.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private AppSettings settings;

    public List<AudioDeviceInfo> PlaybackDevices { get; }
    public List<AudioDeviceInfo> RecordingDevices { get; }

    public SettingsViewModel()
    {
        settings = SettingsManager.Instance.Settings;
        PlaybackDevices = AudioDeviceService.GetPlaybackDevices();
        RecordingDevices = AudioDeviceService.GetRecordingDevices();
    }

    [RelayCommand]
    private void Save()
    {
        SettingsManager.Instance.SaveSettings(Settings);
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        Settings = new AppSettings();
        SettingsManager.Instance.SaveSettings(Settings);
    }
}
