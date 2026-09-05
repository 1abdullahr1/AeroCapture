using System;
using System.Collections.Generic;
using System.Drawing;

namespace AeroCapture.Models;

public enum AnnotationToolType
{
    Select,
    Pen,
    Highlighter,
    Line,
    Arrow,
    Rectangle,
    RoundedRectangle,
    Ellipse,
    Text,
    StepBadge,
    Pixelate,
    Crop
}

public class AnnotationElement
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public AnnotationToolType ToolType { get; set; }
    public Color StrokeColor { get; set; } = Color.Red;
    public Color FillColor { get; set; } = Color.Transparent;
    public float StrokeWidth { get; set; } = 4f;
    public List<PointF> Points { get; set; } = new();
    public RectangleF Bounds { get; set; }
    public string Text { get; set; } = string.Empty;
    public float FontSize { get; set; } = 18f;
    public int StepNumber { get; set; } = 1;
    public int PixelateBlockSize { get; set; } = 12;

    public AnnotationElement Clone()
    {
        return new AnnotationElement
        {
            Id = Guid.NewGuid().ToString(),
            ToolType = ToolType,
            StrokeColor = StrokeColor,
            FillColor = FillColor,
            StrokeWidth = StrokeWidth,
            Points = new List<PointF>(Points),
            Bounds = Bounds,
            Text = Text,
            FontSize = FontSize,
            StepNumber = StepNumber,
            PixelateBlockSize = PixelateBlockSize
        };
    }
}
