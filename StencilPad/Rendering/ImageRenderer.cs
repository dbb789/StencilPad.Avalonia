using Avalonia.Media;
using Avalonia.Media.Imaging;
using SkiaSharp;
using StencilPad.Models.Resolvers;
using StencilPad.Spatial;

namespace StencilPad.Rendering;

public class ImageRenderer : IImageWalker, IWalkerRenderer
{
    private static readonly Transform FlipY;

    static ImageRenderer()
    {
        FlipY = new ScaleTransform(1, -1);
    }
    
    private UnitBounds? _bounds;
    private Bitmap? _bitmap;
    private double _opacity = 1.0;

    public event Action? RendererDirty;
    
    public ImageRenderer()
    {
        _bounds = null;
        _bitmap = null;
    }

    public void Dispose()
    {
        // ...
    }

    public void SetBounds(UnitBounds? bounds)
    {
        _bounds = bounds;
        InvokeRendererDirty();
    }
    
    public void SetImageData(byte[] imageData)
    {
        if (imageData.Length == 0)
        {
            _bitmap = null;
            InvokeRendererDirty();
            return;
        }
        
        _bitmap = new Bitmap(new MemoryStream(imageData));

        InvokeRendererDirty();
    }

    public void SetOpacity(double opacity)
    {
        _opacity = opacity;
        
        InvokeRendererDirty();
    }

    public void Render(SKCanvas canvas)
    {
        
    }

    // public void Render(DrawingContext dc)
    // {
    //     if (_bitmap is null || _bounds is null)
    //     {
    //         return;
    //     }

    //     var flippedBounds = UnitBounds.FromCenterSize(new Unit2D(_bounds.Value.Center.X, -_bounds.Value.Center.Y),
    //                                                   _bounds.Value.Size);

    //     var rect = flippedBounds.Millimeters;

    //     if (rect.Width <= 0 || rect.Height <= 0)
    //     {
    //         return;
    //     }
        
    //     // Account for WPF's inverted Y-axis by flipping the Y-axis for image rendering.
    //     using var flipState = dc.PushTransform(FlipY.Value);
    //     using var opacityState = dc.PushOpacity(_opacity);

    //     dc.DrawImage(_bitmap, rect);
    // }
    
    private void InvokeRendererDirty()
    {
        RendererDirty?.Invoke();
    }
}
