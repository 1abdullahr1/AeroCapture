using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AeroCapture.Models;

namespace AeroCapture.Core.Encoding;

public static class FFmpegEncoderDetector
{
    private static readonly HashSet<string> _detectedEncoders = new(StringComparer.OrdinalIgnoreCase);
    private static bool _hasProbed = false;
    private static string? _cachedFfmpegPath;

    public static string GetFFmpegPath()
    {
        if (!string.IsNullOrEmpty(_cachedFfmpegPath) && File.Exists(_cachedFfmpegPath))
            return _cachedFfmpegPath;

        string appDir = AppDomain.CurrentDomain.BaseDirectory;

        // 1. Check bundled ffmpeg subfolder
        string bundledSub = Path.Combine(appDir, "ffmpeg", "ffmpeg.exe");
        if (File.Exists(bundledSub))
        {
            _cachedFfmpegPath = bundledSub;
            return bundledSub;
        }

        // 2. Check app directory root
        string appRoot = Path.Combine(appDir, "ffmpeg.exe");
        if (File.Exists(appRoot))
        {
            _cachedFfmpegPath = appRoot;
            return appRoot;
        }

        // 3. Fallback to system PATH
        _cachedFfmpegPath = "ffmpeg";
        return _cachedFfmpegPath;
    }

    public static void ProbeEncoders()
    {
        if (_hasProbed) return;

        string ffmpeg = GetFFmpegPath();
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = ffmpeg,
                Arguments = "-hide_banner -encoders",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(3000);

                if (output.Contains("h264_nvenc")) _detectedEncoders.Add("h264_nvenc");
                if (output.Contains("hevc_nvenc")) _detectedEncoders.Add("hevc_nvenc");
                if (output.Contains("h264_amf")) _detectedEncoders.Add("h264_amf");
                if (output.Contains("hevc_amf")) _detectedEncoders.Add("hevc_amf");
                if (output.Contains("h264_qsv")) _detectedEncoders.Add("h264_qsv");
                if (output.Contains("hevc_qsv")) _detectedEncoders.Add("hevc_qsv");
                if (output.Contains("libx264")) _detectedEncoders.Add("libx264");
                if (output.Contains("libx265")) _detectedEncoders.Add("libx265");
                if (output.Contains("libvpx-vp9")) _detectedEncoders.Add("libvpx-vp9");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FFmpegEncoderDetector] Probe exception: {ex.Message}");
            // Default fallbacks
            _detectedEncoders.Add("libx264");
        }

        _hasProbed = true;
    }

    public static (string VideoEncoder, string Preset, string DisplayName) ResolveEncoder(VideoEncoderType preferred, VideoContainerFormat format)
    {
        ProbeEncoders();

        if (format == VideoContainerFormat.Gif)
        {
            return ("gif", "", "Animated GIF");
        }

        if (format == VideoContainerFormat.WebM)
        {
            return ("libvpx-vp9", "-deadline realtime -cpu-used 4", "VP9 (WebM)");
        }

        // Check user preference or Auto
        if (preferred == VideoEncoderType.NvidiaNvenc && _detectedEncoders.Contains("h264_nvenc"))
        {
            return ("h264_nvenc", "-preset p4 -tune ll -zerolatency 1", "NVIDIA NVENC (H.264)");
        }
        if (preferred == VideoEncoderType.AmdAmf && _detectedEncoders.Contains("h264_amf"))
        {
            return ("h264_amf", "-quality speed -rc cbr", "AMD AMF (H.264)");
        }
        if (preferred == VideoEncoderType.IntelQsv && _detectedEncoders.Contains("h264_qsv"))
        {
            return ("h264_qsv", "-preset veryfast", "Intel QuickSync (H.264)");
        }
        if (preferred == VideoEncoderType.SoftwareX265 && _detectedEncoders.Contains("libx265"))
        {
            return ("libx265", "-preset veryfast -crf 26", "Software HEVC (x265)");
        }
        if (preferred == VideoEncoderType.SoftwareX264)
        {
            return ("libx264", "-preset veryfast -tune zerolatency -crf 23", "Software (x264)");
        }

        // AutoHardware priority: NVENC -> QSV -> AMF -> libx264
        if (_detectedEncoders.Contains("h264_nvenc"))
        {
            return ("h264_nvenc", "-preset p4 -tune ll -zerolatency 1", "⚡ NVIDIA NVENC (Hardware)");
        }
        if (_detectedEncoders.Contains("h264_qsv"))
        {
            return ("h264_qsv", "-preset veryfast", "⚡ Intel QuickSync (Hardware)");
        }
        if (_detectedEncoders.Contains("h264_amf"))
        {
            return ("h264_amf", "-quality speed", "⚡ AMD AMF (Hardware)");
        }

        return ("libx264", "-preset veryfast -tune zerolatency -crf 23", "Software CPU (libx264)");
    }
}
