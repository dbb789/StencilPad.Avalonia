using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace StencilPad.Rendering;

public class WalkerRendererDrawOperation : ICustomDrawOperation
{
    private readonly IWalkerRenderer _renderer;
    private readonly SKMatrix _matrix;

    public Rect Bounds => new Rect(0, 0, 0, 0);

    public WalkerRendererDrawOperation(IWalkerRenderer renderer,
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


        canvas.Save();
        canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, _matrix));

        _renderer.Render(canvas, grContext);
        
        canvas.Restore();
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
