using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using AeroCapture.Core.Encoding;
using AeroCapture.Models;
using AeroCapture.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AeroCapture.ViewModels;

public partial class AnnotationViewModel : ObservableObject
{
    private Bitmap? _sourceBitmap;

    [ObservableProperty]
    private AnnotationToolType selectedTool = AnnotationToolType.Arrow;

    [ObservableProperty]
    private Color selectedColor = Color.FromArgb(239, 68, 68); // Fluent Modern Red

    [ObservableProperty]
    private float strokeWidth = 4.0f;

    [ObservableProperty]
    private float fontSize = 20.0f;

    [ObservableProperty]
    private int currentStepNumber = 1;

    public ObservableCollection<AnnotationElement> Elements { get; } = new();
    private readonly Stack<AnnotationElement> _undoStack = new();
    private readonly Stack<AnnotationElement> _redoStack = new();

    public Bitmap? SourceBitmap
    {
        get => _sourceBitmap;
        set => SetProperty(ref _sourceBitmap, value);
    }

    public event EventHandler? CloseRequested;
    public event EventHandler? RefreshCanvasRequested;

    public void LoadBitmap(Bitmap bmp)
    {
        SourceBitmap = bmp;
        Elements.Clear();
        _undoStack.Clear();
        _redoStack.Clear();
        CurrentStepNumber = 1;
        RefreshCanvasRequested?.Invoke(this, EventArgs.Empty);
    }

    public void AddElement(AnnotationElement element)
    {
        Elements.Add(element);
        _undoStack.Push(element);
        _redoStack.Clear();

        if (element.ToolType == AnnotationToolType.StepBadge)
        {
            CurrentStepNumber++;
        }

        RefreshCanvasRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Undo()
    {
        if (Elements.Count > 0)
        {
            var last = Elements[^1];
            Elements.RemoveAt(Elements.Count - 1);
            _redoStack.Push(last);

            if (last.ToolType == AnnotationToolType.StepBadge && CurrentStepNumber > 1)
            {
                CurrentStepNumber--;
            }

            RefreshCanvasRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void Redo()
    {
        if (_redoStack.Count > 0)
        {
            var element = _redoStack.Pop();
            Elements.Add(element);
            _undoStack.Push(element);

            if (element.ToolType == AnnotationToolType.StepBadge)
            {
                CurrentStepNumber++;
            }

            RefreshCanvasRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void Copy()
    {
        if (SourceBitmap == null) return;
        using var annotated = ScreenshotExporter.RenderAnnotations(SourceBitmap, Elements);
        ScreenshotExporter.CopyToClipboard(annotated);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Save()
    {
        if (SourceBitmap == null) return;
        var settings = SettingsManager.Instance.Settings;

        using var annotated = ScreenshotExporter.RenderAnnotations(SourceBitmap, Elements);
        string path = ScreenshotExporter.SaveScreenshot(
            annotated, 
            settings.ScreenshotsDirectory, 
            settings.ScreenshotFormat, 
            settings.JpegQuality);

        var fi = new System.IO.FileInfo(path);
        HistoryManager.Instance.AddScreenshot(new ScreenshotItem
        {
            FilePath = path,
            CreatedAt = fi.CreationTime,
            FileSizeBytes = fi.Length,
            Width = annotated.Width,
            Height = annotated.Height
        });

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Discard()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
