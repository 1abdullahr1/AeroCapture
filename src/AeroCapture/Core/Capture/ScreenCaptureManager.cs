using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using AeroCapture.Core.Audio;
using AeroCapture.Core.Encoding;
using AeroCapture.Models;

namespace AeroCapture.Core.Capture;

public enum RecordingState
{
    Idle,
    Recording,
    Paused
}

public class ScreenCaptureManager : IDisposable
{
    private ICaptureSource? _captureSource;
    private AudioMixer? _audioMixer;
    private FFmpegEncodingSession? _encodingSession;
    private RecordingState _state = RecordingState.Idle;
    private string? _currentRecordingPath;
    private DateTime _recordingStartTime;

    public RecordingState State => _state;
    public bool IsRecording => _state == RecordingState.Recording;
    public bool IsPaused => _state == RecordingState.Paused;
    public TimeSpan ElapsedDuration => _encodingSession?.ElapsedDuration ?? TimeSpan.Zero;

    public event EventHandler? RecordingStarted;
    public event EventHandler? RecordingPaused;
    public event EventHandler? RecordingResumed;
    public event EventHandler<RecordingItem>? RecordingStopped;
    public event EventHandler<AudioLevelEventArgs>? AudioLevelChanged;

    public void StartRecording(CaptureMode mode, Rectangle region, IntPtr windowHandle, RecordingProfile profile, string outputDirectory)
    {
        if (_state != RecordingState.Idle) return;

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        string ext = profile.GetFileExtension();
        string filename = $"Recording_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{ext}";
        _currentRecordingPath = Path.Combine(outputDirectory, filename);

        // Determine capture bounds & source
        Rectangle captureBounds;
        if (mode == CaptureMode.Window && windowHandle != IntPtr.Zero)
        {
            _captureSource = new WindowsGraphicsCaptureEngine(windowHandle, profile.Framerate, profile.CaptureCursor);
            captureBounds = WindowEnumerationService.GetAccurateWindowBounds(windowHandle);
        }
        else if (mode == CaptureMode.SelectedRegion && !region.IsEmpty)
        {
            _captureSource = new WindowsGraphicsCaptureEngine(region, profile.Framerate, profile.CaptureCursor);
            captureBounds = region;
        }
        else
        {
            captureBounds = WindowEnumerationService.GetVirtualScreenBounds();
            _captureSource = new DxgiDuplicationEngine(captureBounds, profile.Framerate);
        }

        int width = _captureSource.Width;
        int height = _captureSource.Height;

        bool hasAudio = (profile.RecordSystemAudio || profile.RecordMicrophone) && profile.Container != VideoContainerFormat.Gif;

        // Initialize Audio Mixer if audio enabled
        if (hasAudio)
        {
            _audioMixer = new AudioMixer(
                profile.RecordSystemAudio,
                profile.RecordMicrophone,
                profile.SystemAudioDeviceId,
                profile.MicrophoneDeviceId,
                profile.SystemAudioVolume,
                profile.MicrophoneVolume);

            _audioMixer.DataAvailable += (s, e) =>
            {
                _encodingSession?.WriteAudioSamples(e.Buffer, e.Length);
            };

            _audioMixer.LevelChanged += (s, e) =>
            {
                AudioLevelChanged?.Invoke(this, e);
            };
        }

        // Initialize FFmpeg session
        _encodingSession = new FFmpegEncodingSession(
            _currentRecordingPath,
            width,
            height,
            profile.Framerate,
            profile,
            hasAudio);

        // Pipe video frames to FFmpeg
        _captureSource.FrameArrived += (s, e) =>
        {
            _encodingSession?.WriteVideoFrame(e.Data);
        };

        // Start subsystems
        _encodingSession.Start();
        _audioMixer?.Start();
        _captureSource.Start();

        _recordingStartTime = DateTime.Now;
        _state = RecordingState.Recording;
        RecordingStarted?.Invoke(this, EventArgs.Empty);
    }

    public void PauseRecording()
    {
        if (_state != RecordingState.Recording) return;

        _encodingSession?.Pause();
        _state = RecordingState.Paused;
        RecordingPaused?.Invoke(this, EventArgs.Empty);
    }

    public void ResumeRecording()
    {
        if (_state != RecordingState.Paused) return;

        _encodingSession?.Resume();
        _state = RecordingState.Recording;
        RecordingResumed?.Invoke(this, EventArgs.Empty);
    }

    public async Task<RecordingItem?> StopRecordingAsync()
    {
        if (_state == RecordingState.Idle || _encodingSession == null) return null;

        _state = RecordingState.Idle;

        _captureSource?.Stop();
        _captureSource?.Dispose();
        _captureSource = null;

        _audioMixer?.Stop();
        _audioMixer?.Dispose();
        _audioMixer = null;

        var duration = _encodingSession.ElapsedDuration;
        await _encodingSession.StopAsync();
        _encodingSession = null;

        RecordingItem? item = null;
        if (!string.IsNullOrEmpty(_currentRecordingPath) && File.Exists(_currentRecordingPath))
        {
            var fileInfo = new FileInfo(_currentRecordingPath);
            item = new RecordingItem
            {
                FilePath = _currentRecordingPath,
                CreatedAt = _recordingStartTime,
                Duration = duration,
                FileSizeBytes = fileInfo.Length,
                ContainerFormat = fileInfo.Extension.TrimStart('.').ToUpperInvariant()
            };

            RecordingStopped?.Invoke(this, item);
        }

        _currentRecordingPath = null;
        return item;
    }

    public static Bitmap CaptureScreenshot(CaptureMode mode, Rectangle region, IntPtr windowHandle)
    {
        if (mode == CaptureMode.Window && windowHandle != IntPtr.Zero)
        {
            return WindowsGraphicsCaptureEngine.CaptureWindowSnapshot(windowHandle);
        }
        else if (mode == CaptureMode.SelectedRegion && !region.IsEmpty)
        {
            return WindowsGraphicsCaptureEngine.CaptureSnapshot(region);
        }
        else
        {
            var virtualScreen = WindowEnumerationService.GetVirtualScreenBounds();
            return WindowsGraphicsCaptureEngine.CaptureSnapshot(virtualScreen);
        }
    }

    public void Dispose()
    {
        _captureSource?.Dispose();
        _audioMixer?.Dispose();
        _encodingSession?.Dispose();
        GC.SuppressFinalize(this);
    }
}
