using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using AeroCapture.Core.Capture;
using AeroCapture.Core.Encoding;
using AeroCapture.Models;
using AeroCapture.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AeroCapture.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ScreenCaptureManager _captureManager = new();
    private readonly System.Timers.Timer _durationTimer = new(500);

    [ObservableProperty]
    private bool isRecording;

    [ObservableProperty]
    private bool isPaused;

    [ObservableProperty]
    private string recordingDurationText = "00:00:00";

    [ObservableProperty]
    private string activeEncoderName = "Detecting...";

    [ObservableProperty]
    private bool recordSystemAudio = true;

    [ObservableProperty]
    private bool recordMicrophone = false;

    [ObservableProperty]
    private float systemAudioLevel = 0.0f;

    [ObservableProperty]
    private float microphoneAudioLevel = 0.0f;

    public event EventHandler<CaptureTargetType>? SelectionOverlayRequested;
    public event EventHandler? WindowPickerRequested;
    public event EventHandler<Bitmap>? AnnotationEditorRequested;

    public MainViewModel()
    {
        _durationTimer.Elapsed += (s, e) =>
        {
            if (IsRecording)
            {
                var dur = _captureManager.ElapsedDuration;
                RecordingDurationText = dur.ToString(@"hh\:mm\:ss");
            }
        };

        _captureManager.RecordingStarted += (s, e) =>
        {
            IsRecording = true;
            IsPaused = false;
            _durationTimer.Start();
        };

        _captureManager.RecordingPaused += (s, e) =>
        {
            IsPaused = true;
        };

        _captureManager.RecordingResumed += (s, e) =>
        {
            IsPaused = false;
        };

        _captureManager.RecordingStopped += (s, item) =>
        {
            IsRecording = false;
            IsPaused = false;
            _durationTimer.Stop();
            RecordingDurationText = "00:00:00";
            SystemAudioLevel = 0f;
            MicrophoneAudioLevel = 0f;

            if (item != null)
            {
                HistoryManager.Instance.AddRecording(item);
            }
        };

        _captureManager.AudioLevelChanged += (s, e) =>
        {
            SystemAudioLevel = e.SystemAudioLevel;
            MicrophoneAudioLevel = e.MicrophoneLevel;
        };

        RefreshEncoderBadge();
    }

    public void RefreshEncoderBadge()
    {
        Task.Run(() =>
        {
            var profile = SettingsManager.Instance.Settings.DefaultProfile;
            var (_, _, displayName) = FFmpegEncoderDetector.ResolveEncoder(profile.Encoder, profile.Container);
            ActiveEncoderName = displayName;
        });
    }

    [RelayCommand]
    private void StartFullscreenRecording()
    {
        var settings = SettingsManager.Instance.Settings;
        var profile = settings.DefaultProfile;
        profile.RecordSystemAudio = RecordSystemAudio;
        profile.RecordMicrophone = RecordMicrophone;

        _captureManager.StartRecording(
            CaptureMode.FullScreen,
            Rectangle.Empty,
            IntPtr.Zero,
            profile,
            settings.RecordingsDirectory);
    }

    [RelayCommand]
    private void RequestRegionRecording()
    {
        SelectionOverlayRequested?.Invoke(this, CaptureTargetType.VideoRecording);
    }

    [RelayCommand]
    private void RequestWindowRecording()
    {
        WindowPickerRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void CaptureFullscreenScreenshot()
    {
        var bmp = ScreenCaptureManager.CaptureScreenshot(CaptureMode.FullScreen, Rectangle.Empty, IntPtr.Zero);
        ProcessCapturedScreenshot(bmp);
    }

    [RelayCommand]
    private void RequestRegionScreenshot()
    {
        SelectionOverlayRequested?.Invoke(this, CaptureTargetType.Screenshot);
    }

    [RelayCommand]
    private void TogglePauseRecording()
    {
        if (_captureManager.IsPaused)
        {
            _captureManager.ResumeRecording();
        }
        else if (_captureManager.IsRecording)
        {
            _captureManager.PauseRecording();
        }
    }

    [RelayCommand]
    private async Task StopRecording()
    {
        await _captureManager.StopRecordingAsync();
    }

    public void StartRegionRecordingWithBounds(Rectangle bounds)
    {
        var settings = SettingsManager.Instance.Settings;
        var profile = settings.DefaultProfile;
        profile.RecordSystemAudio = RecordSystemAudio;
        profile.RecordMicrophone = RecordMicrophone;

        _captureManager.StartRecording(
            CaptureMode.SelectedRegion,
            bounds,
            IntPtr.Zero,
            profile,
            settings.RecordingsDirectory);
    }

    public void StartWindowRecordingWithHandle(IntPtr hWnd)
    {
        var settings = SettingsManager.Instance.Settings;
        var profile = settings.DefaultProfile;
        profile.RecordSystemAudio = RecordSystemAudio;
        profile.RecordMicrophone = RecordMicrophone;

        _captureManager.StartRecording(
            CaptureMode.Window,
            Rectangle.Empty,
            hWnd,
            profile,
            settings.RecordingsDirectory);
    }

    public void ProcessCapturedScreenshot(Bitmap bmp)
    {
        var settings = SettingsManager.Instance.Settings;

        if (settings.AutoCopyToClipboard)
        {
            ScreenshotExporter.CopyToClipboard(bmp);
        }

        if (settings.AutoSaveScreenshots)
        {
            string path = ScreenshotExporter.SaveScreenshot(
                bmp,
                settings.ScreenshotsDirectory,
                settings.ScreenshotFormat,
                settings.JpegQuality);

            var fi = new FileInfo(path);
            var item = new ScreenshotItem
            {
                FilePath = path,
                CreatedAt = fi.CreationTime,
                FileSizeBytes = fi.Length,
                Width = bmp.Width,
                Height = bmp.Height
            };
            HistoryManager.Instance.AddScreenshot(item);
        }

        if (settings.OpenInAnnotationEditor)
        {
            AnnotationEditorRequested?.Invoke(this, bmp);
        }
    }
}
