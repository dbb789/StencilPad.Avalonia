using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

namespace StencilPad.Rendering;

public class WalkerRendererDrawOperation : ICustomDrawOperation
{
    private readonly IWalkerRenderer _renderer;
    
    public Rect Bounds => new Rect(0, 0, 0, 0);

    public WalkerRendererDrawOperation(IWalkerRenderer renderer)
    {
        _renderer = renderer;
    }

    public void Dispose()
    {
        // This component is reusable - Dispose() is a no-op.
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
        
        _renderer.Render(canvas, grContext);
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
