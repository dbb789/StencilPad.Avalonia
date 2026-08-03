using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace StencilPad.Rendering;

public class SKPictureDrawOperation : ICustomDrawOperation
{
    public Rect Bounds => _bounds;

    private readonly SKPicture _picture;
    private readonly Rect _bounds;

    public SKPictureDrawOperation(SKPicture picture, Rect bounds)
    {
        _picture = picture;
        _bounds = bounds;
    }

    public void Dispose()
    {
        // _picture.Dispose();
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

        canvas.DrawPicture(_picture);
    }

    public bool HitTest(Point p)
    {
        return Bounds.Contains(p);
    }

    public bool Equals(ICustomDrawOperation? other)
    {
        return ReferenceEquals(this, other);
    }
}
