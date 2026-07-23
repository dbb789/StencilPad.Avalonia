using Avalonia;
using Avalonia.Media;
using SkiaSharp;

namespace StencilPad.Spatial;

public interface IViewport
{
    Unit2D SheetSize { get; }
    Unit2D Size { get; }
    double Zoom { get; }
    Transform MillimetersToPixelsTransform { get; }
    SKMatrix MillimetersToPixelsMatrix { get; }

    event Action? ViewportChanged;
    
    double ToPixels(Unit unit);
    Point ToPoint(Unit2D position);
    Rect ToRect(UnitBounds bounds);
    Unit FromPixels(double pixels);
    Unit2D FromVector(Vector vector);
    Unit2D FromPoint(Point point);
}
