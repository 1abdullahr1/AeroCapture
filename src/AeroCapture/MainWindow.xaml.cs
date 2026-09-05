using System;
using System.Drawing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AeroCapture.Core.Interop;
using AeroCapture.Models;
using AeroCapture.Services;
using AeroCapture.ViewModels;
using AeroCapture.Views;

namespace AeroCapture;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();

    private readonly DashboardPage _dashboardPage = new();
    private readonly HistoryPage _historyPage = new();
    private readonly SettingsPage _settingsPage = new();

    private HotKeyManager? _hotKeyManager;
    private TrayIconManager? _trayIconManager;
    private RecordingControlBar? _recordingControlBar;

    public MainWindow()
    {
        InitializeComponent();

        _dashboardPage.Initialize(ViewModel);

        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        // Setup System Tray
        _trayIconManager = new TrayIconManager();
        _trayIconManager.Initialize(hwnd);
        _trayIconManager.OpenRequested += (s, e) => DispatcherQueue.TryEnqueue(ShowApp);
        _trayIconManager.CaptureRegionRequested += (s, e) => DispatcherQueue.TryEnqueue(() => ViewModel.RequestRegionScreenshotCommand.Execute(null));
        _trayIconManager.CaptureFullRequested += (s, e) => DispatcherQueue.TryEnqueue(() => ViewModel.CaptureFullscreenScreenshotCommand.Execute(null));
        _trayIconManager.RecordToggleRequested += (s, e) => DispatcherQueue.TryEnqueue(() =>
        {
            if (ViewModel.IsRecording) ViewModel.StopRecordingCommand.Execute(null);
            else ViewModel.StartFullscreenRecordingCommand.Execute(null);
        });
        _trayIconManager.SettingsRequested += (s, e) => DispatcherQueue.TryEnqueue(() => ContentFrame.Navigate(typeof(SettingsPage)));
        _trayIconManager.ExitRequested += (s, e) => DispatcherQueue.TryEnqueue(ExitApp);

        // Setup Global Hotkeys
        _hotKeyManager = new HotKeyManager();
        _hotKeyManager.ScreenshotRegionRequested += (s, e) => DispatcherQueue.TryEnqueue(() => ViewModel.RequestRegionScreenshotCommand.Execute(null));
        _hotKeyManager.ScreenshotFullRequested += (s, e) => DispatcherQueue.TryEnqueue(() => ViewModel.CaptureFullscreenScreenshotCommand.Execute(null));
        _hotKeyManager.ScreenshotWindowRequested += (s, e) => DispatcherQueue.TryEnqueue(() => ViewModel.RequestRegionScreenshotCommand.Execute(null));
        _hotKeyManager.RecordToggleRequested += (s, e) => DispatcherQueue.TryEnqueue(() =>
        {
            if (ViewModel.IsRecording) ViewModel.StopRecordingCommand.Execute(null);
            else ViewModel.StartFullscreenRecordingCommand.Execute(null);
        });
        _hotKeyManager.RecordPauseRequested += (s, e) => DispatcherQueue.TryEnqueue(() => ViewModel.TogglePauseRecordingCommand.Execute(null));
        _hotKeyManager.Start();

        // ViewModel event wiring
        ViewModel.SelectionOverlayRequested += OnSelectionOverlayRequested;
        ViewModel.AnnotationEditorRequested += OnAnnotationEditorRequested;

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.IsRecording))
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.IsRecording)
                    {
                        _recordingControlBar = new RecordingControlBar(ViewModel);
                        _recordingControlBar.Activate();
                    }
                    else
                    {
                        _recordingControlBar?.Close();
                        _recordingControlBar = null;
                    }
                });
            }
        };

        // Window size & position
        SetInitialWindowSize(hwnd, 960, 680);
    }

    private static void SetInitialWindowSize(IntPtr hwnd, int width, int height)
    {
        int screenW = Win32.GetSystemMetrics(0);
        int screenH = Win32.GetSystemMetrics(1);
        int x = (screenW - width) / 2;
        int y = (screenH - height) / 2;
        Win32.SetWindowPos(hwnd, IntPtr.Zero, x, y, width, height, Win32.SWP_SHOWWINDOW);
    }

    private void OnNavLoaded(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(typeof(DashboardPage));
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
        }
        else if (args.SelectedItem is NavigationViewItem item)
        {
            switch (item.Tag?.ToString())
            {
                case "Dashboard":
                    ContentFrame.Content = _dashboardPage;
                    break;
                case "History":
                    ContentFrame.Content = _historyPage;
                    break;
            }
        }
    }

    private void OnSelectionOverlayRequested(object? sender, CaptureTargetType targetType)
    {
        var overlay = new CaptureOverlayWindow(targetType);

        if (targetType == CaptureTargetType.VideoRecording)
        {
            overlay.RegionSelectedForRecording += (s, bounds) =>
            {
                ViewModel.StartRegionRecordingWithBounds(bounds);
            };
            overlay.WindowSelectedForRecording += (s, hwnd) =>
            {
                ViewModel.StartWindowRecordingWithHandle(hwnd);
            };
        }
        else
        {
            overlay.RegionSelectedForScreenshot += (s, bounds) =>
            {
                var bmp = Core.Capture.WindowsGraphicsCaptureEngine.CaptureSnapshot(bounds);
                ViewModel.ProcessCapturedScreenshot(bmp);
            };
            overlay.WindowSelectedForScreenshot += (s, hwnd) =>
            {
                var bmp = Core.Capture.WindowsGraphicsCaptureEngine.CaptureWindowSnapshot(hwnd);
                ViewModel.ProcessCapturedScreenshot(bmp);
            };
        }

        overlay.Activate();
    }

    private void OnAnnotationEditorRequested(object? sender, Bitmap bmp)
    {
        var editor = new AnnotationWindow(bmp);
        editor.Activate();
    }

    public void ShowApp()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        Win32.ShowWindow(hwnd, Win32.SW_RESTORE);
        Win32.SetForegroundWindow(hwnd);
    }

    public void ExitApp()
    {
        _hotKeyManager?.Dispose();
        _trayIconManager?.Dispose();
        _captureManager?.Dispose();
        Application.Current.Exit();
    }

    private readonly Core.Capture.ScreenCaptureManager _captureManager = new();
}
