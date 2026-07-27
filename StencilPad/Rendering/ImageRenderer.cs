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
        
        public void Dispose()
        {
            Image?.Dispose();
            Image = null;
        }
    }
    
    private class RenderedImageProperties : IDisposable
    {
        public UnitBounds Bounds = UnitBounds.Empty;
        public double Opacity = 1.0;
        
        public void Dispose()
        {
            // ...
        }
    }

    private SKImage? _image;
    private UnitBounds? _bounds;
    private double _opacity = 1.0;

    private SharedDisposable<RenderedImage> _renderedImage;
    private SharedDisposable<RenderedImageProperties> _renderedImageProperties;

    public event Action? RendererDirty;
    
    public ImageRenderer()
    {
        _renderedImage = new(new());
        _renderedImageProperties = new(new());
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
        _renderedImage.SetValue(new RenderedImage
        {
            Image = ((imageData.Length > 0) ? SKImage.FromEncodedData(imageData) : default),
        });
        
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

        using var propertiesHandle = _renderedImageProperties.Get();
        
        var properties = propertiesHandle.Value;
        
        var bounds = properties.Bounds;
        var rect = new SKRect((float)bounds.SW.X.Millimeters,
                              -(float)bounds.NE.Y.Millimeters,
                              (float)bounds.NE.X.Millimeters,
                              -(float)bounds.SW.Y.Millimeters);
        
        canvas.Save();
        canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, SKMatrix.CreateScale(1, -1)));

        canvas.DrawImage(image.Image, rect, new SKPaint
        {
            Color = new SKColor(255, 255, 255, (byte)(properties.Opacity * 255)),
            IsAntialias = true
        });

        canvas.Restore();
    }

    private void InvokeRendererDirty()
    {
        _renderedImageProperties.SetValue(new RenderedImageProperties
        {
            Bounds = _bounds ?? UnitBounds.Empty,
            Opacity = _opacity
        });
        
        RendererDirty?.Invoke();
    }
}
