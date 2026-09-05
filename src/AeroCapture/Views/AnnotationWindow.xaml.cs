using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using AeroCapture.Models;
using AeroCapture.ViewModels;
using Windows.Storage.Streams;

namespace AeroCapture.Views;

public sealed partial class AnnotationWindow : Window
{
    private readonly AnnotationViewModel _viewModel = new();
    private bool _isDrawing = false;
    private Windows.Foundation.Point _startPoint;
    private readonly List<PointF> _currentPoints = new();

    public AnnotationWindow(Bitmap bitmap)
    {
        InitializeComponent();
        _viewModel.CloseRequested += (s, e) => Close();

        LoadSourceImage(bitmap);
    }

    private void LoadSourceImage(Bitmap bmp)
    {
        _viewModel.LoadBitmap(bmp);

        // Display bitmap in WinUI Image control via InMemoryRandomAccessStream
        using var ms = new MemoryStream();
        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        ms.Position = 0;

        var ras = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(ras.GetOutputStreamAt(0)))
        {
            writer.WriteBytes(ms.ToArray());
            writer.StoreAsync().AsTask().Wait();
            writer.FlushAsync().AsTask().Wait();
        }

        var bitmapImage = new BitmapImage();
        bitmapImage.SetSource(ras);
        BackgroundImageView.Source = bitmapImage;

        DrawingCanvas.Width = bmp.Width;
        DrawingCanvas.Height = bmp.Height;
    }

    private void OnToolChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ToolRadioGroup.SelectedItem is RadioButton rb && rb.Tag is string tag)
        {
            if (Enum.TryParse<AnnotationToolType>(tag, out var tool))
            {
                _viewModel.SelectedTool = tool;
            }
        }
    }

    private void OnColorSelected(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string hex)
        {
            var color = ColorTranslator.FromHtml(hex);
            _viewModel.SelectedColor = color;
        }
    }

    private void OnCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var pt = e.GetCurrentPoint(DrawingCanvas).Position;
        _startPoint = pt;
        _isDrawing = true;
        _currentPoints.Clear();
        _currentPoints.Add(new PointF((float)pt.X, (float)pt.Y));
    }

    private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing) return;

        var pt = e.GetCurrentPoint(DrawingCanvas).Position;
        _currentPoints.Add(new PointF((float)pt.X, (float)pt.Y));
    }

    private void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing) return;
        _isDrawing = false;

        var pt = e.GetCurrentPoint(DrawingCanvas).Position;
        _currentPoints.Add(new PointF((float)pt.X, (float)pt.Y));

        float x = (float)Math.Min(_startPoint.X, pt.X);
        float y = (float)Math.Min(_startPoint.Y, pt.Y);
        float w = (float)Math.Abs(_startPoint.X - pt.X);
        float h = (float)Math.Abs(_startPoint.Y - pt.Y);

        var element = new AnnotationElement
        {
            ToolType = _viewModel.SelectedTool,
            StrokeColor = _viewModel.SelectedColor,
            StrokeWidth = _viewModel.StrokeWidth,
            Points = new List<PointF>(_currentPoints),
            Bounds = new RectangleF(x, y, Math.Max(w, 2), Math.Max(h, 2)),
            StepNumber = _viewModel.CurrentStepNumber,
            FontSize = _viewModel.FontSize
        };

        _viewModel.AddElement(element);

        // Re-render annotated image preview
        if (_viewModel.SourceBitmap != null)
        {
            using var annotated = Core.Encoding.ScreenshotExporter.RenderAnnotations(_viewModel.SourceBitmap, _viewModel.Elements);
            LoadSourceImage(annotated);
        }
    }

    private void OnUndoClicked(object sender, RoutedEventArgs e)
    {
        _viewModel.UndoCommand.Execute(null);
    }

    private void OnRedoClicked(object sender, RoutedEventArgs e)
    {
        _viewModel.RedoCommand.Execute(null);
    }

    private void OnCopyClicked(object sender, RoutedEventArgs e)
    {
        _viewModel.CopyCommand.Execute(null);
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveCommand.Execute(null);
    }

    private void OnDiscardClicked(object sender, RoutedEventArgs e)
    {
        _viewModel.DiscardCommand.Execute(null);
    }
}
