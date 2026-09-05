using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AeroCapture.ViewModels;

namespace AeroCapture.Views;

public sealed partial class DashboardPage : Page
{
    public MainViewModel ViewModel { get; set; } = null!;

    public DashboardPage()
    {
        InitializeComponent();
    }

    public void Initialize(MainViewModel viewModel)
    {
        ViewModel = viewModel;
        ViewModel.PropertyChanged += (s, e) =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (e.PropertyName == nameof(ViewModel.ActiveEncoderName))
                {
                    EncoderBadgeText.Text = ViewModel.ActiveEncoderName;
                }
                else if (e.PropertyName == nameof(ViewModel.SystemAudioLevel))
                {
                    SystemAudioMeter.Value = ViewModel.SystemAudioLevel;
                }
                else if (e.PropertyName == nameof(ViewModel.MicrophoneAudioLevel))
                {
                    MicAudioMeter.Value = ViewModel.MicrophoneAudioLevel;
                }
            });
        };

        EncoderBadgeText.Text = ViewModel.ActiveEncoderName;
        SystemAudioSwitch.IsOn = ViewModel.RecordSystemAudio;
        MicAudioSwitch.IsOn = ViewModel.RecordMicrophone;
    }

    private void OnRecordFullscreenClicked(object sender, RoutedEventArgs e)
    {
        ViewModel?.StartFullscreenRecordingCommand.Execute(null);
    }

    private void OnRecordRegionClicked(object sender, RoutedEventArgs e)
    {
        ViewModel?.RequestRegionRecordingCommand.Execute(null);
    }

    private void OnRecordWindowClicked(object sender, RoutedEventArgs e)
    {
        ViewModel?.RequestWindowRecordingCommand.Execute(null);
    }

    private void OnScreenshotFullscreenClicked(object sender, RoutedEventArgs e)
    {
        ViewModel?.CaptureFullscreenScreenshotCommand.Execute(null);
    }

    private void OnScreenshotRegionClicked(object sender, RoutedEventArgs e)
    {
        ViewModel?.RequestRegionScreenshotCommand.Execute(null);
    }

    private void OnSystemAudioToggled(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.RecordSystemAudio = SystemAudioSwitch.IsOn;
        }
    }

    private void OnMicAudioToggled(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.RecordMicrophone = MicAudioSwitch.IsOn;
        }
    }
}
