using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace StencilPad.UI.Widgets;

public class GeometryDropdownItem : Control
{
    private const double MmPerInch = 25.4;
    private const double Dpi = 96.0;
    
    private class DrawOperation : ICustomDrawOperation
    {
        public bool HitTest(Point p) => Bounds.Contains(p);
        public Rect Bounds => _bounds;

        private readonly SKPath _path;
        private readonly SKMatrix _matrix;
        private readonly SKPaint _paint;
        private readonly Rect _bounds;

        public DrawOperation(SKPath path, SKMatrix matrix, SKPaint? paint = null)
        {
            _path = new(path);
            _matrix = matrix;
            _paint = paint?.Clone() ?? new SKPaint()
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = 0.2f,
                IsAntialias = true
            };

            _bounds = matrix.MapRect(path.Bounds).ToAvaloniaRect().Inflate(1);
        }

        public void Dispose()
        {
            _path.Dispose();
            _paint.Dispose();
        }

        public void Render(ImmediateDrawingContext context)
        {
            var feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            
            if (feature is null)
            {
                return;
            }

            using var lease = feature.Lease();
        
            var canvas = lease.SkCanvas;

            canvas.Save();
            canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, _matrix));
            canvas.DrawPath(_path, _paint);
            canvas.Restore();
        }

        public bool Equals(ICustomDrawOperation? other)
        {
            return Object.ReferenceEquals(this, other);
        }
    }
    
    public static readonly StyledProperty<SKPath?> PathProperty =
        AvaloniaProperty.Register<GeometryDropdownItem, SKPath?>(nameof(Path));

    public static readonly StyledProperty<SKPaint?> PaintProperty =
        AvaloniaProperty.Register<GeometryDropdownItem, SKPaint?>(nameof(Paint));

    static GeometryDropdownItem()
    {
        AffectsRender<GeometryDropdownItem>(PathProperty, PaintProperty);
    }
    
    public SKPath? Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    public SKPaint? Paint
    {
        get => GetValue(PaintProperty);
        set => SetValue(PaintProperty, value);
    }
    
    public override void Render(DrawingContext dc)
    {
        if (Path is null)
        {
            return;
        }

        double renderScaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        float scale = (float)(renderScaling * Dpi / MmPerInch);
        var matrix = SKMatrix.CreateScale(scale, scale);

        var scaledBounds = matrix.MapRect(Path.Bounds);
        var offsetX = (float)(Bounds.Width / 2.0) - scaledBounds.MidX;
        var offsetY = (float)(Bounds.Height / 2.0) - scaledBounds.MidY;
        
        matrix = matrix.PostConcat(SKMatrix.CreateTranslation(offsetX, offsetY));

        dc.Custom(new DrawOperation(Path, matrix, Paint));
    }
}
