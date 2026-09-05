using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using AeroCapture.Models;
using AeroCapture.Services;

namespace AeroCapture.Views;

public sealed partial class SettingsPage : Page
{
    private AppSettings _settings = SettingsManager.Instance.Settings;

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettingsIntoUI();
    }

    private void LoadSettingsIntoUI()
    {
        _settings = SettingsManager.Instance.Settings;

        // Container
        ContainerComboBox.SelectedIndex = (int)_settings.DefaultProfile.Container;

        // Encoder
        EncoderComboBox.SelectedIndex = (int)_settings.DefaultProfile.Encoder;

        // FPS
        int fps = _settings.DefaultProfile.Framerate;
        FpsComboBox.SelectedIndex = fps switch
        {
            24 => 0,
            30 => 1,
            60 => 2,
            120 => 3,
            _ => 2
        };

        // Bitrate
        BitrateSlider.Value = _settings.DefaultProfile.VideoBitrateKbps;
        BitrateLabel.Text = $"Video Bitrate: {_settings.DefaultProfile.VideoBitrateKbps} Kbps";

        // Cursor
        CaptureCursorSwitch.IsOn = _settings.DefaultProfile.CaptureCursor;

        // Screenshot
        ScreenshotFormatComboBox.SelectedIndex = (int)_settings.ScreenshotFormat;
        QualitySlider.Value = _settings.JpegQuality;
        QualityLabel.Text = $"JPEG Quality: {_settings.JpegQuality}%";

        AutoClipboardSwitch.IsOn = _settings.AutoCopyToClipboard;
        OpenEditorSwitch.IsOn = _settings.OpenInAnnotationEditor;
        AutoSaveSwitch.IsOn = _settings.AutoSaveScreenshots;

        RecordingsDirBox.Text = _settings.RecordingsDirectory;
        ScreenshotsDirBox.Text = _settings.ScreenshotsDirectory;
    }

    private void OnBitrateChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (BitrateLabel != null)
        {
            BitrateLabel.Text = $"Video Bitrate: {(int)e.NewValue} Kbps";
        }
    }

    private void OnQualityChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (QualityLabel != null)
        {
            QualityLabel.Text = $"JPEG Quality: {(int)e.NewValue}%";
        }
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        _settings.DefaultProfile.Container = (VideoContainerFormat)ContainerComboBox.SelectedIndex;
        _settings.DefaultProfile.Encoder = (VideoEncoderType)EncoderComboBox.SelectedIndex;

        _settings.DefaultProfile.Framerate = FpsComboBox.SelectedIndex switch
        {
            0 => 24,
            1 => 30,
            2 => 60,
            3 => 120,
            _ => 60
        };

        _settings.DefaultProfile.VideoBitrateKbps = (int)BitrateSlider.Value;
        _settings.DefaultProfile.CaptureCursor = CaptureCursorSwitch.IsOn;

        _settings.ScreenshotFormat = (ScreenshotFormat)ScreenshotFormatComboBox.SelectedIndex;
        _settings.JpegQuality = (int)QualitySlider.Value;
        _settings.AutoCopyToClipboard = AutoClipboardSwitch.IsOn;
        _settings.OpenInAnnotationEditor = OpenEditorSwitch.IsOn;
        _settings.AutoSaveScreenshots = AutoSaveSwitch.IsOn;

        if (!string.IsNullOrWhiteSpace(RecordingsDirBox.Text))
            _settings.RecordingsDirectory = RecordingsDirBox.Text;

        if (!string.IsNullOrWhiteSpace(ScreenshotsDirBox.Text))
            _settings.ScreenshotsDirectory = ScreenshotsDirBox.Text;

        SettingsManager.Instance.SaveSettings(_settings);
    }
}
