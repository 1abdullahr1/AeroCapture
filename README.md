# AeroCapture ⚡

A polished, high-performance, Windows-native screen recording and screenshot studio built with **C#**, **.NET 8**, **WinUI 3**, **Windows App SDK**, **DirectX/DXGI**, **WASAPI CoreAudio**, and **FFmpeg with Hardware Acceleration**.

![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011%20(x64)-blue)
![Framework](https://img.shields.io/badge/.NET-8.0-purple)
![UI](https://img.shields.io/badge/UI-WinUI%203%20%7C%20Windows%20App%20SDK%201.6-0078D4)
![License](https://img.shields.io/badge/License-MIT-green)

---

## ✨ Features

### 🎥 Screen Recording
- **Full Screen Recording**: Single monitor or multi-monitor virtual desktop spanning.
- **Selected Region Recording**: Interactive translucent selection overlay with crosshair coordinates and real-time dimension badges.
- **Window / Application Recording**: Cleanly capture specific application windows with accurate DWM frame boundaries (no shadow bleed).
- **Hardware-Accelerated Encoding**: Automatically probes and leverages GPU video engines:
  - **NVIDIA NVENC** (`h264_nvenc`, `hevc_nvenc`)
  - **AMD AMF** (`h264_amf`, `hevc_amf`)
  - **Intel QuickSync** (`h264_qsv`, `hevc_qsv`)
  - **CPU Software Fallback** (`libx264`, `libx265`, `libvpx-vp9`)
- **Containers & Formats**: MP4 (`+faststart`), crash-resilient MKV, WebM, and animated GIF (with 2-pass palette optimization).
- **Pause / Resume / Stop**: Graceful container closure ensuring zero corrupted video files.
- **Floating Mini Control Bar**: Non-intrusive pill overlay showing recording elapsed timer, live audio VU meter, pause/resume, and stop buttons.

### 🎙️ Audio System
- **System Audio Loopback**: Captures game sound, music, browser audio, and system sounds via WASAPI loopback with zero latency.
- **Microphone Input**: Real-time microphone capture from default or selected recording devices.
- **Dual Stream Real-Time Mixer**: High-fidelity 48kHz 16-bit stereo mixer with individual volume controls and soft-saturation limiter to prevent clipping.

### 📸 Screenshots & Annotation Studio
- **Capture Modes**: Fullscreen, selected region, and window snip.
- **Modern Annotation Studio**:
  - **Tools**: Freehand Pen, Highlighter, Arrow, Line, Rectangle, Ellipse, Typography Text, Numbered Step Badges (①, ②, ③...), Privacy Blur / Pixelate redaction tool.
  - **Color Palette**: Modern Fluent color swatches + custom hex support.
  - **History**: Full Undo and Redo stacks.
  - **Instant Export**: Copy directly to clipboard (DIB/PNG) or auto-save with naming templates.

### ⌨️ Global Keyboard Shortcuts

| Action | Default Shortcut |
| :--- | :--- |
| **Capture Region Screenshot** | `PrintScreen` |
| **Capture Full Screen Screenshot** | `Ctrl + PrintScreen` |
| **Capture Window Screenshot** | `Alt + PrintScreen` |
| **Start / Stop Screen Recording** | `Ctrl + Shift + R` |
| **Pause / Resume Recording** | `Ctrl + Shift + P` |

---

## 🏛️ Architecture

```
AeroCapture
├── Core
│   ├── Audio        (WASAPI Loopback, Microphone, Real-time Mixer)
│   ├── Capture      (DirectX/DXGI Duplication, Windows.Graphics.Capture, Window Discovery)
│   ├── Encoding     (FFmpeg Pipeline via Pipes, Hardware Encoder Detector, Exporter)
│   └── Interop      (Win32, COM, Direct3D 11, WASAPI, Shell NotifyIcon)
├── Models           (AppSettings, RecordingProfile, AnnotationModel, Items)
├── Services         (HotKeyManager, HistoryManager, SettingsManager, TrayIconManager)
├── ViewModels       (MainViewModel, HistoryViewModel, SettingsViewModel, AnnotationViewModel)
├── Views            (Dashboard, History, Settings, Overlay, MiniBar, AnnotationStudio)
└── Installer        (Inno Setup Script, Remote GitHub Actions CI/CD)
```

---

## 📦 Production Windows Installer

AeroCapture uses **Inno Setup** to produce a standalone Windows installer executable (`AeroCapture-Setup-v1.0.0.exe`):
- **Self-Contained Deployment**: Bundles the .NET 8 runtime, Windows App SDK binaries, and static `ffmpeg.exe`. No prerequisite runtime or SDK installations are required on the user's machine.
- **Start Menu & Desktop Shortcuts**: Optional desktop and start menu shortcuts.
- **Clean Uninstallation**: Complete Windows Add/Remove Programs registration with thorough removal.
- **Silent Installation**: Enterprise support using `/VERYSILENT /NORESTART` command line flags.

---

## 🚀 Remote Build & CI/CD (GitHub Actions)

AeroCapture is built and packaged automatically on GitHub Windows runners:
1. Pushes to `main` trigger the automated build workflow.
2. The workflow restores dependencies, compiles the self-contained WinUI 3 x64 application, downloads the latest static FFmpeg binary, and compiles the Inno Setup installer.
3. Downloadable artifacts (`AeroCapture-Setup-x64.exe` and `AeroCapture-Portable-win-x64.zip`) are generated and attached to each run and GitHub Release.

---

## 📄 License
This project is licensed under the MIT License.
