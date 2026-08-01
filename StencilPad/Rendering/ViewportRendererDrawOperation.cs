using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace StencilPad.Rendering;

public class ViewportRendererDrawOperation : ICustomDrawOperation
{
    private readonly IViewportRenderer _renderer;
    private readonly SKMatrix _matrix;
    
    public Rect Bounds => new Rect(0, 0, 0, 0);

    public ViewportRendererDrawOperation(IViewportRenderer renderer,
                                         SKMatrix matrix)
    {
        _renderer = renderer;
        _matrix = matrix;
    }

    public void Dispose()
    {
        // ...
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
        var grContext = lease.GrContext;
        
        _renderer.Render(canvas, grContext, _matrix);
    }

    public bool HitTest(Point p)
    {
        return Bounds.Contains(p);
    }
    
    public bool Equals(ICustomDrawOperation? other)
    {
        return Object.ReferenceEquals(this, other);
    }
}
