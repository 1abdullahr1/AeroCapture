using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AeroCapture.Models;

namespace AeroCapture.Core.Encoding;

public class FFmpegEncodingSession : IDisposable
{
    private readonly string _outputPath;
    private readonly int _width;
    private readonly int _height;
    private readonly int _fps;
    private readonly RecordingProfile _profile;
    private readonly bool _hasAudio;

    private Process? _ffmpegProcess;
    private Stream? _videoInputStream;
    private NamedPipeServerStream? _audioPipeStream;
    private string? _audioPipeName;
    private Thread? _audioPipeThread;

    private volatile bool _isRunning;
    private volatile bool _isPaused;
    private readonly Stopwatch _recordingTimer = new();
    private TimeSpan _pausedDuration = TimeSpan.Zero;
    private Stopwatch? _pauseWatch;

    public string OutputPath => _outputPath;
    public bool IsRunning => _isRunning;
    public bool IsPaused => _isPaused;
    public TimeSpan ElapsedDuration => _recordingTimer.Elapsed - _pausedDuration;

    public FFmpegEncodingSession(string outputPath, int width, int height, int fps, RecordingProfile profile, bool hasAudio)
    {
        _outputPath = outputPath;
        _width = width % 2 == 0 ? width : width - 1;
        _height = height % 2 == 0 ? height : height - 1;
        _fps = fps;
        _profile = profile;
        _hasAudio = hasAudio;

        string? dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public void Start()
    {
        if (_isRunning) return;

        string ffmpegPath = FFmpegEncoderDetector.GetFFmpegPath();
        var (encoder, presetArgs, _) = FFmpegEncoderDetector.ResolveEncoder(_profile.Encoder, _profile.Container);

        var args = new StringBuilder();
        args.Append("-y -hide_banner -loglevel warning ");

        // Video input via STDIN
        args.Append($"-f rawvideo -vcodec rawvideo -pix_fmt bgra -s {_width}x{_height} -r {_fps} -i - ");

        // Audio input via Named Pipe if enabled
        if (_hasAudio)
        {
            string pipeGuid = Guid.NewGuid().ToString("N");
            _audioPipeName = $"aerocapture_audio_{pipeGuid}";
            _audioPipeStream = new NamedPipeServerStream(
                _audioPipeName, 
                PipeDirection.Out, 
                1, 
                PipeTransmissionMode.Byte, 
                PipeOptions.Asynchronous, 
                65536, 
                65536);

            args.Append($"-f s16le -ar 48000 -ac 2 -i \"\\\\.\\pipe\\{_audioPipeName}\" ");
        }

        // Encoding options
        if (_profile.Container == VideoContainerFormat.Gif)
        {
            // High quality GIF 2-pass palette
            args.Append($"-vf \"fps={Math.Min(_fps, 25)},split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse\" \"{_outputPath}\"");
        }
        else
        {
            args.Append($"-c:v {encoder} ");
            if (!string.IsNullOrEmpty(presetArgs))
            {
                args.Append($"{presetArgs} ");
            }

            int bitrate = _profile.VideoBitrateKbps > 0 ? _profile.VideoBitrateKbps : 6000;
            args.Append($"-b:v {bitrate}k -pix_fmt yuv420p ");

            if (_hasAudio)
            {
                string audioCodec = _profile.Container == VideoContainerFormat.WebM ? "libopus" : "aac";
                string audioBitrate = _profile.AudioQuality switch
                {
                    AudioQuality.Low96k => "96k",
                    AudioQuality.Standard128k => "128k",
                    AudioQuality.High192k => "192k",
                    AudioQuality.Studio320k => "320k",
                    _ => "192k"
                };
                args.Append($"-c:a {audioCodec} -b:a {audioBitrate} ");
            }

            if (_profile.Container == VideoContainerFormat.Mp4)
            {
                args.Append("-movflags +faststart ");
            }

            args.Append($"\"{_outputPath}\"");
        }

        var psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = args.ToString(),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardError = true
        };

        _ffmpegProcess = new Process { StartInfo = psi };
        _ffmpegProcess.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Debug.WriteLine($"[FFmpeg] {e.Data}");
        };

        _ffmpegProcess.Start();
        _ffmpegProcess.BeginErrorReadLine();
        _videoInputStream = _ffmpegProcess.StandardInput.BaseStream;

        if (_hasAudio && _audioPipeStream != null)
        {
            _audioPipeThread = new Thread(() =>
            {
                try
                {
                    _audioPipeStream.WaitForConnection();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[FFmpegPipe] Pipe wait exception: {ex.Message}");
                }
            })
            {
                IsBackground = true
            };
            _audioPipeThread.Start();
        }

        _isRunning = true;
        _recordingTimer.Restart();
    }

    public void WriteVideoFrame(byte[] frameBuffer)
    {
        if (!_isRunning || _isPaused || _videoInputStream == null) return;

        try
        {
            _videoInputStream.Write(frameBuffer, 0, frameBuffer.Length);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FFmpeg] WriteVideoFrame failed: {ex.Message}");
        }
    }

    public void WriteAudioSamples(byte[] pcmBuffer, int length)
    {
        if (!_isRunning || _isPaused || _audioPipeStream == null || !_audioPipeStream.IsConnected) return;

        try
        {
            _audioPipeStream.Write(pcmBuffer, 0, length);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FFmpeg] WriteAudioSamples failed: {ex.Message}");
        }
    }

    public void Pause()
    {
        if (!_isRunning || _isPaused) return;
        _isPaused = true;
        _pauseWatch = Stopwatch.StartNew();
    }

    public void Resume()
    {
        if (!_isRunning || !_isPaused) return;
        _isPaused = false;
        if (_pauseWatch != null)
        {
            _pauseWatch.Stop();
            _pausedDuration += _pauseWatch.Elapsed;
            _pauseWatch = null;
        }
    }

    public async Task StopAsync()
    {
        if (!_isRunning) return;
        _isRunning = false;
        _recordingTimer.Stop();

        try
        {
            if (_videoInputStream != null)
            {
                await _videoInputStream.FlushAsync();
                _videoInputStream.Close();
                _videoInputStream = null;
            }

            if (_audioPipeStream != null)
            {
                if (_audioPipeStream.IsConnected)
                {
                    await _audioPipeStream.FlushAsync();
                }
                _audioPipeStream.Close();
                _audioPipeStream.Dispose();
                _audioPipeStream = null;
            }

            if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
            {
                await _ffmpegProcess.WaitForExitAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FFmpegSession] StopAsync error: {ex.Message}");
        }
        finally
        {
            _ffmpegProcess?.Dispose();
            _ffmpegProcess = null;
        }
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
