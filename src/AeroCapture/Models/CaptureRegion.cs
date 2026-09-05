using System.Drawing;

namespace AeroCapture.Models;

public readonly record struct CaptureRegion(int X, int Y, int Width, int Height)
{
    public static CaptureRegion Empty => new(0, 0, 0, 0);

    public bool IsEmpty => Width <= 0 || Height <= 0;

    public Rectangle ToRectangle() => new(X, Y, Width, Height);

    public static CaptureRegion FromRectangle(Rectangle rect) => new(rect.X, rect.Y, rect.Width, rect.Height);

    public override string ToString() => $"{Width} × {Height} at ({X}, {Y})";
}
