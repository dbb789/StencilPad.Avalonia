using SkiaSharp;
using StencilPad.Common;
using StencilPad.Models.Resolvers;
using StencilPad.Spatial;

namespace StencilPad.Rendering;

public class ImageRenderer : IImageWalker, IWalkerRenderer
{
    private class RenderedImage : IDisposable
    {
        public SKImage? Image;
        public UnitBounds Bounds;
        
        public void Dispose()
        {
            Image?.Dispose();
            Image = null;
        }
    }

    private SKImage? _image;
    private UnitBounds? _bounds;
    private double _opacity = 1.0;

    private SharedDisposable<RenderedImage> _renderedImage;

    public event Action? RendererDirty;
    
    public ImageRenderer()
    {
        _renderedImage = new(new());
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
        _image = SKImage.FromBitmap(SKBitmap.Decode(imageData));

        System.Diagnostics.Debug.WriteLine($"Decoded image with dimensions: {_image.Width}x{_image.Height}");
        
        InvokeRendererDirty();
    }

    public void SetOpacity(double opacity)
    {
        _opacity = opacity;
        
        InvokeRendererDirty();
    }

    public void Render(SKCanvas canvas)
    {
        using var imageHandle = _renderedImage.Get();

        var image = imageHandle.Value;

        if (image.Image is null)
        {
            return;
        }

        var bounds = image.Bounds;
        var rect = SKRect.Create((float)bounds.Min.X.Millimeters,
                                 (float)bounds.Min.Y.Millimeters,
                                 (float)bounds.Size.X.Millimeters,
                                 (float)bounds.Size.Y.Millimeters);

        canvas.DrawImage(image.Image,
                         rect,
                         new SKPaint
                         {
                             Color = new SKColor(255, 255, 255, 255),
                             IsAntialias = true
                         });
    }

    private void InvokeRendererDirty()
    {
        _renderedImage.SetValue(new RenderedImage
        {
            Image = _image,
            Bounds = _bounds ?? UnitBounds.Empty
        });

        RendererDirty?.Invoke();
    }
}
