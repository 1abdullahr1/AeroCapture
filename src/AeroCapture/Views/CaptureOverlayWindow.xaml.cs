using System;
using System.Drawing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using AeroCapture.Core.Capture;
using AeroCapture.Core.Interop;
using AeroCapture.Models;
using Windows.System;

namespace AeroCapture.Views;

public sealed partial class CaptureOverlayWindow : Window
{
    private bool _isDragging;
    private Windows.Foundation.Point _startPoint;
    private Rectangle _selectedBounds;
    private IntPtr _hoveredWindowHandle = IntPtr.Zero;

    public event EventHandler<Rectangle>? RegionSelectedForRecording;
    public event EventHandler<Rectangle>? RegionSelectedForScreenshot;
    public event EventHandler<IntPtr>? WindowSelectedForRecording;
    public event EventHandler<IntPtr>? WindowSelectedForScreenshot;

    private readonly CaptureTargetType _targetType;

    public CaptureOverlayWindow(CaptureTargetType targetType = CaptureTargetType.Screenshot)
    {
        InitializeComponent();
        _targetType = targetType;

        MakeFullscreenVirtual();
    }

    private void MakeFullscreenVirtual()
    {
        var virtualBounds = WindowEnumerationService.GetVirtualScreenBounds();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        // Remove titlebar / border and make topmost
        Win32.SetWindowPos(hwnd, Win32.HWND_TOPMOST, 
            virtualBounds.X, virtualBounds.Y, virtualBounds.Width, virtualBounds.Height, 
            Win32.SWP_SHOWWINDOW);
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var pt = e.GetCurrentPoint(OverlayCanvas).Position;
        _startPoint = pt;
        _isDragging = true;
        ActionBar.Visibility = Visibility.Collapsed;

        SelectionBox.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionBox, pt.X);
        Canvas.SetTop(SelectionBox, pt.Y);
        SelectionBox.Width = 0;
        SelectionBox.Height = 0;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(OverlayCanvas).Position;

        if (_isDragging)
        {
            double x = Math.Min(_startPoint.X, currentPoint.X);
            double y = Math.Min(_startPoint.Y, currentPoint.Y);
            double w = Math.Abs(_startPoint.X - currentPoint.X);
            double h = Math.Abs(_startPoint.Y - currentPoint.Y);

            Canvas.SetLeft(SelectionBox, x);
            Canvas.SetTop(SelectionBox, y);
            SelectionBox.Width = w;
            SelectionBox.Height = h;

            _selectedBounds = new Rectangle((int)x, (int)y, (int)w, (int)h);
            DimensionText.Text = $"{_selectedBounds.Width} × {_selectedBounds.Height}";
        }
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;

            if (_selectedBounds.Width > 15 && _selectedBounds.Height > 15)
            {
                ActionBar.Visibility = Visibility.Visible;

                // If user directly requested screenshot or recording without needing confirmation
                if (_targetType == CaptureTargetType.Screenshot)
                {
                    OnScreenshotClicked(this, new RoutedEventArgs());
                }
                else if (_targetType == CaptureTargetType.VideoRecording)
                {
                    OnRecordClicked(this, new RoutedEventArgs());
                }
            }
            else
            {
                // Single click on window: detect window under cursor
                var win = WindowEnumerationService.GetWindowUnderCursor();
                if (win != null && win.Bounds.Width > 20 && win.Bounds.Height > 20)
                {
                    _hoveredWindowHandle = win.Handle;
                    _selectedBounds = win.Bounds;
                    Canvas.SetLeft(SelectionBox, win.Bounds.X);
                    Canvas.SetTop(SelectionBox, win.Bounds.Y);
                    SelectionBox.Width = win.Bounds.Width;
                    SelectionBox.Height = win.Bounds.Height;
                    SelectionBox.Visibility = Visibility.Visible;
                    ActionBar.Visibility = Visibility.Visible;

                    if (_targetType == CaptureTargetType.Screenshot)
                    {
                        OnScreenshotClicked(this, new RoutedEventArgs());
                    }
                    else if (_targetType == CaptureTargetType.VideoRecording)
                    {
                        OnRecordClicked(this, new RoutedEventArgs());
                    }
                }
            }
        }
    }

    private void OnRecordClicked(object sender, RoutedEventArgs e)
    {
        if (_hoveredWindowHandle != IntPtr.Zero)
        {
            WindowSelectedForRecording?.Invoke(this, _hoveredWindowHandle);
        }
        else if (!_selectedBounds.IsEmpty)
        {
            RegionSelectedForRecording?.Invoke(this, _selectedBounds);
        }
        Close();
    }

    private void OnScreenshotClicked(object sender, RoutedEventArgs e)
    {
        if (_hoveredWindowHandle != IntPtr.Zero)
        {
            WindowSelectedForScreenshot?.Invoke(this, _hoveredWindowHandle);
        }
        else if (!_selectedBounds.IsEmpty)
        {
            RegionSelectedForScreenshot?.Invoke(this, _selectedBounds);
        }
        Close();
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            Close();
        }
    }
}
