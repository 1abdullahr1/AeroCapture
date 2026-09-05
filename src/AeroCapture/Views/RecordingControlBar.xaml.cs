using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using AeroCapture.Core.Interop;
using AeroCapture.ViewModels;

namespace AeroCapture.Views;

public sealed partial class RecordingControlBar : Window
{
    private readonly MainViewModel _viewModel;

    public RecordingControlBar(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;

        // Position window at top center of primary display
        PositionWindowTopCenter();

        _viewModel.PropertyChanged += (s, e) =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (e.PropertyName == nameof(_viewModel.RecordingDurationText))
                {
                    TimerText.Text = _viewModel.RecordingDurationText;
                }
                else if (e.PropertyName == nameof(_viewModel.IsPaused))
                {
                    PauseIcon.Glyph = _viewModel.IsPaused ? "\uE768" /* Play */ : "\uE769" /* Pause */;
                    IndicatorDot.Fill = _viewModel.IsPaused ? new SolidColorBrush(Microsoft.UI.Colors.Orange) : new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 59, 48));
                }
                else if (e.PropertyName == nameof(_viewModel.SystemAudioLevel) || e.PropertyName == nameof(_viewModel.MicrophoneAudioLevel))
                {
                    AudioMeter.Value = Math.Max(_viewModel.SystemAudioLevel, _viewModel.MicrophoneAudioLevel);
                }
            });
        };
    }

    private void PositionWindowTopCenter()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        int width = 300;
        int height = 54;

        int screenWidth = Win32.GetSystemMetrics(0 /* SM_CXSCREEN */);
        int x = (screenWidth - width) / 2;
        int y = 20;

        Win32.SetWindowPos(hwnd, Win32.HWND_TOPMOST, x, y, width, height, Win32.SWP_SHOWWINDOW);
    }

    private void OnDragPressed(object sender, PointerRoutedEventArgs e)
    {
        // Allow moving the control bar
    }

    private void OnPauseResumeClicked(object sender, RoutedEventArgs e)
    {
        _viewModel.TogglePauseRecordingCommand.Execute(null);
    }

    private void OnStopClicked(object sender, RoutedEventArgs e)
    {
        _viewModel.StopRecordingCommand.Execute(null);
        Close();
    }
}
