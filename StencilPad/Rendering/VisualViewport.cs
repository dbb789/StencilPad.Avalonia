using Avalonia;
using Avalonia.Controls;
using SkiaSharp;
using StencilPad.Spatial;

namespace StencilPad.Rendering;

public class VisualViewport : IViewport
{
    private const double MmPerInch = 25.4;

    public Visual? Visual
    {
        get => _visual;
        set
        {
            if (_visual == value)
            {
                return;
            }

            _visual = value;

            // DPI is fixed at 96 for Avalonia, but we can adjust it based on
            // the render scaling factor of the top-level window.
            double renderScaling = 1.0;

            if (_visual is not null)
            {
                renderScaling = TopLevel.GetTopLevel(_visual)?.RenderScaling ?? 1.0;
            }
            
            _dpi = renderScaling * 96.0;
            
            OnViewportChanged();
        }
    }
    
    public Unit2D SheetSize
    {
        get => _sheetSize;
        set
        {
            if (_sheetSize == value)
            {
                return;
            }

            _sheetSize = value;
            OnViewportChanged();
        }
    }

    public Unit2D Size
    {
        get => _size;
        set
        {
            if (_size == value)
            {
                return;
            }

            if (value.X <= Unit.Zero || value.Y <= Unit.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Size dimensions must be positive.");
            }

            _size = value;
            
            OnViewportChanged();
        }
    }


    public double Zoom
    {
        get => _zoom;
        set
        {
            if (_zoom == value)
            {
                return;
            }

            if (value <= 0.0 || double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Zoom must be positive.");
            }

            _zoom = value;

            OnViewportChanged();
        }
    }
    
    public SKMatrix MillimetersToPixelsMatrix
    {
        get => _millimetersToPixelsMatrix;
    }
    
    private Visual? _visual = null;
    private Unit2D _sheetSize;
    private Unit2D _size;
    private Vector _halfPixelSize;
    private double _zoom;
    private double _dpi;
    private SKMatrix _millimetersToPixelsMatrix;
    
    public event Action? ViewportChanged;

    public VisualViewport()
    {
        _visual = null;
        _sheetSize = new Unit2D(Unit.FromMillimeters(210.0), Unit.FromMillimeters(297.0));
        _size = _sheetSize * 1.1;
        _zoom = 1.0;
        _dpi = 96.0;
        _millimetersToPixelsMatrix = GetMillimetersToPixelsMatrix();

        OnViewportChanged();
    }
    
    public double ToPixels(Unit unit)
    {
        return unit.Millimeters / MmPerInch * _dpi * Zoom;
    }

    public Point ToPoint(Unit2D position)
    {
        return new Point(ToPixels(position.X), ToPixels(-position.Y)) + _halfPixelSize;
    }

    public Rect ToRect(UnitBounds bounds)
    {
        var topLeft = ToPoint(bounds.NW);
        var bottomRight = ToPoint(bounds.SE);

        return new Rect(topLeft, bottomRight);
    }
    
    public Unit FromPixels(double pixels)
    {
        return Unit.FromMillimeters(pixels * MmPerInch / _dpi / Zoom);
    }

    public Unit2D FromVector(Vector vector)
    {
        return new Unit2D(FromPixels(vector.X),
                          -FromPixels(vector.Y));
    }

    public Unit2D FromPoint(Point point)
    {
        return new Unit2D(FromPixels(point.X - _halfPixelSize.X),
                          -FromPixels(point.Y - _halfPixelSize.Y));
    }

    private void OnViewportChanged()
    {
        _millimetersToPixelsMatrix = GetMillimetersToPixelsMatrix();
        _halfPixelSize = new Vector(ToPixels(_size.X) / 2.0, ToPixels(_size.Y) / 2.0);
        
        ViewportChanged?.Invoke();
    }

    private SKMatrix GetMillimetersToPixelsMatrix()
    {
        var scaleFactor = ToPixels(Unit.FromMillimeters(1.0));
        var translateX = _size.X.Millimeters / 2.0;
        var translateY = -_size.Y.Millimeters / 2.0;

        var scale = SKMatrix.CreateScale((float)scaleFactor, (float)-scaleFactor);
        var translate = SKMatrix.CreateTranslation((float)translateX, (float)translateY);

        return SKMatrix.Concat(scale, translate);
    }
}
