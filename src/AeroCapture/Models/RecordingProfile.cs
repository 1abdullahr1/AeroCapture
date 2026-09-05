namespace AeroCapture.Models;

public enum VideoEncoderType
{
    AutoHardware,
    NvidiaNvenc,
    AmdAmf,
    IntelQsv,
    SoftwareX264,
    SoftwareX265
}

public enum VideoContainerFormat
{
    Mp4,
    Mkv,
    WebM,
    Gif
}

public enum AudioQuality
{
    Low96k,
    Standard128k,
    High192k,
    Studio320k
}

public class RecordingProfile
{
    public VideoContainerFormat Container { get; set; } = VideoContainerFormat.Mp4;
    public VideoEncoderType Encoder { get; set; } = VideoEncoderType.AutoHardware;
    public int Framerate { get; set; } = 60;
    public int VideoBitrateKbps { get; set; } = 8000;
    public AudioQuality AudioQuality { get; set; } = AudioQuality.High192k;
    public bool RecordSystemAudio { get; set; } = true;
    public bool RecordMicrophone { get; set; } = false;
    public string SystemAudioDeviceId { get; set; } = string.Empty;
    public string MicrophoneDeviceId { get; set; } = string.Empty;
    public float SystemAudioVolume { get; set; } = 1.0f;
    public float MicrophoneVolume { get; set; } = 1.0f;
    public bool CaptureCursor { get; set; } = true;
    public bool HighlightClicks { get; set; } = false;

    public string GetFileExtension() => Container switch
    {
        VideoContainerFormat.Mp4 => ".mp4",
        VideoContainerFormat.Mkv => ".mkv",
        VideoContainerFormat.WebM => ".webm",
        VideoContainerFormat.Gif => ".gif",
        _ => ".mp4"
    };
}
