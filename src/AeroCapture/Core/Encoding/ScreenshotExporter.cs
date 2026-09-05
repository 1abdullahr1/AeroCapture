using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using AeroCapture.Models;

namespace AeroCapture.Core.Encoding;

public static class ScreenshotExporter
{
    public static string SaveScreenshot(Bitmap bitmap, string directory, ScreenshotFormat format, int jpegQuality = 90)
    {
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string ext = format switch
        {
            ScreenshotFormat.Png => ".png",
            ScreenshotFormat.Jpeg => ".jpg",
            ScreenshotFormat.WebP => ".webp",
            ScreenshotFormat.Bmp => ".bmp",
            _ => ".png"
        };

        string filename = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{ext}";
        string fullPath = Path.Combine(directory, filename);

        switch (format)
        {
            case ScreenshotFormat.Jpeg:
                SaveAsJpeg(bitmap, fullPath, jpegQuality);
                break;
            case ScreenshotFormat.Bmp:
                bitmap.Save(fullPath, ImageFormat.Bmp);
                break;
            case ScreenshotFormat.Png:
            case ScreenshotFormat.WebP:
            default:
                bitmap.Save(fullPath, ImageFormat.Png);
                break;
        }

        return fullPath;
    }

    public static void SaveAsJpeg(Bitmap bitmap, string path, int quality)
    {
        var encoderParameters = new EncoderParameters(1);
        encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, (long)Math.Clamp(quality, 10, 100));

        ImageCodecInfo? jpegCodec = GetEncoderInfo(ImageFormat.Jpeg);
        if (jpegCodec != null)
        {
            bitmap.Save(path, jpegCodec, encoderParameters);
        }
        else
        {
            bitmap.Save(path, ImageFormat.Jpeg);
        }
    }

    public static Bitmap RenderAnnotations(Bitmap original, IEnumerable<AnnotationElement> annotations)
    {
        var result = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb);

        using (var g = Graphics.FromImage(result))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(original, 0, 0, original.Width, original.Height);

            foreach (var element in annotations)
            {
                RenderElement(g, result, original, element);
            }
        }

        return result;
    }

    private static void RenderElement(Graphics g, Bitmap targetBitmap, Bitmap sourceBitmap, AnnotationElement elem)
    {
        switch (elem.ToolType)
        {
            case AnnotationToolType.Pen:
                if (elem.Points.Count > 1)
                {
                    using var pen = new Pen(elem.StrokeColor, elem.StrokeWidth)
                    {
                        StartCap = LineCap.Round,
                        EndCap = LineCap.Round,
                        LineJoin = LineJoin.Round
                    };
                    g.DrawCurve(pen, elem.Points.ToArray());
                }
                break;

            case AnnotationToolType.Highlighter:
                if (elem.Points.Count > 1)
                {
                    var highColor = Color.FromArgb(120, elem.StrokeColor.R, elem.StrokeColor.G, elem.StrokeColor.B);
                    using var highPen = new Pen(highColor, elem.StrokeWidth * 3)
                    {
                        StartCap = LineCap.Square,
                        EndCap = LineCap.Square,
                        LineJoin = LineJoin.Round
                    };
                    g.DrawCurve(highPen, elem.Points.ToArray());
                }
                break;

            case AnnotationToolType.Line:
                if (elem.Points.Count >= 2)
                {
                    using var pen = new Pen(elem.StrokeColor, elem.StrokeWidth);
                    g.DrawLine(pen, elem.Points[0], elem.Points[1]);
                }
                break;

            case AnnotationToolType.Arrow:
                if (elem.Points.Count >= 2)
                {
                    using var pen = new Pen(elem.StrokeColor, elem.StrokeWidth);
                    using var cap = new AdjustableArrowCap(elem.StrokeWidth * 1.5f, elem.StrokeWidth * 2f, true);
                    pen.CustomEndCap = cap;
                    g.DrawLine(pen, elem.Points[0], elem.Points[1]);
                }
                break;

            case AnnotationToolType.Rectangle:
                using (var pen = new Pen(elem.StrokeColor, elem.StrokeWidth))
                {
                    g.DrawRectangle(pen, elem.Bounds.X, elem.Bounds.Y, elem.Bounds.Width, elem.Bounds.Height);
                }
                break;

            case AnnotationToolType.Ellipse:
                using (var pen = new Pen(elem.StrokeColor, elem.StrokeWidth))
                {
                    g.DrawEllipse(pen, elem.Bounds.X, elem.Bounds.Y, elem.Bounds.Width, elem.Bounds.Height);
                }
                break;

            case AnnotationToolType.Text:
                if (!string.IsNullOrEmpty(elem.Text))
                {
                    using var font = new Font("Segoe UI", elem.FontSize, FontStyle.Bold);
                    using var brush = new SolidBrush(elem.StrokeColor);
                    g.DrawString(elem.Text, font, brush, elem.Bounds.Location);
                }
                break;

            case AnnotationToolType.StepBadge:
                float radius = 18f;
                float cx = elem.Bounds.X;
                float cy = elem.Bounds.Y;

                using (var circleBrush = new SolidBrush(elem.StrokeColor))
                {
                    g.FillEllipse(circleBrush, cx - radius, cy - radius, radius * 2, radius * 2);
                }
                using (var font = new Font("Segoe UI", 12f, FontStyle.Bold))
                using (var numBrush = new SolidBrush(Color.White))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(elem.StepNumber.ToString(), font, numBrush, new RectangleF(cx - radius, cy - radius, radius * 2, radius * 2), format);
                }
                break;

            case AnnotationToolType.Pixelate:
                ApplyPixelate(targetBitmap, Rectangle.Round(elem.Bounds), elem.PixelateBlockSize);
                break;
        }
    }

    private static void ApplyPixelate(Bitmap bmp, Rectangle rect, int blockSize)
    {
        rect.Intersect(new Rectangle(0, 0, bmp.Width, bmp.Height));
        if (rect.Width <= 0 || rect.Height <= 0 || blockSize < 2) return;

        for (int y = rect.Y; y < rect.Bottom; y += blockSize)
        {
            for (int x = rect.X; x < rect.Right; x += blockSize)
            {
                int blockW = Math.Min(blockSize, rect.Right - x);
                int blockH = Math.Min(blockSize, rect.Bottom - y);

                int r = 0, g = 0, b = 0, count = 0;
                for (int by = 0; by < blockH; by++)
                {
                    for (int bx = 0; bx < blockW; bx++)
                    {
                        var c = bmp.GetPixel(x + bx, y + by);
                        r += c.R;
                        g += c.G;
                        b += c.B;
                        count++;
                    }
                }

                var avgColor = Color.FromArgb(r / count, g / count, b / count);

                for (int by = 0; by < blockH; by++)
                {
                    for (int bx = 0; bx < blockW; bx++)
                    {
                        bmp.SetPixel(x + bx, y + by, avgColor);
                    }
                }
            }
        }
    }

    public static void CopyToClipboard(Bitmap bitmap)
    {
        try
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            if (hBitmap == IntPtr.Zero) return;

            if (OpenClipboard(IntPtr.Zero))
            {
                EmptyClipboard();
                SetClipboardData(CF_BITMAP, hBitmap);
                CloseClipboard();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Clipboard] Copy error: {ex.Message}");
        }
    }

    private const uint CF_BITMAP = 2;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    private static ImageCodecInfo? GetEncoderInfo(ImageFormat format)
    {
        var codecs = ImageCodecInfo.GetImageEncoders();
        foreach (var codec in codecs)
        {
            if (codec.FormatID == format.Guid)
                return codec;
        }
        return null;
    }
}
