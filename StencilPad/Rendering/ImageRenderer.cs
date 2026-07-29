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
        public SKImage? RenderImage;

        public void Dispose()
        {
            Image?.Dispose();
            Image = null;

            RenderImage?.Dispose();
            RenderImage = null;
        }
    }
    
    private class RenderedProperties : IDisposable
    {
        public UnitBounds Bounds = UnitBounds.Empty;
        public SKPaint Paint = new();
        
        public void Dispose()
        {
            Paint?.Dispose();
            Paint = null!;
        }
    }

    private UnitBounds? _bounds;
    private double _opacity = 1.0;

    private SharedDisposable<RenderedImage> _renderedImage;
    private SharedDisposable<RenderedProperties> _renderedProperties;

    public event Action? RendererDirty;
    
    public ImageRenderer()
    {
        _renderedImage = new(new());
        _renderedProperties = new(new());
    }

    public void Dispose()
    {
        _renderedImage.Dispose();
        _renderedProperties.Dispose();
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

    public void Render(SKCanvas canvas, GRContext? context)
    {
        using var imageHandle = _renderedImage.Get();

        var image = imageHandle.Value;

        if (image.Image is null)
        {
            return;
        }

        if (image.RenderImage is null)
        {
            image.RenderImage = (context is not null) ?
                image.Image.ToTextureImage(context) : image.Image.ToRasterImage();
        }

        if (image.RenderImage is null)
        {
            return;
        }
        
        using var propertiesHandle = _renderedProperties.Get();
        
        var properties = propertiesHandle.Value;
        
        var bounds = properties.Bounds;
        var rect = new SKRect((float)bounds.SW.X.Millimeters,
                              -(float)bounds.NE.Y.Millimeters,
                              (float)bounds.NE.X.Millimeters,
                              -(float)bounds.SW.Y.Millimeters);
        
        canvas.Save();
        canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, SKMatrix.CreateScale(1, -1)));
        canvas.DrawImage(image.RenderImage, rect, properties.Paint);
        canvas.Restore();
    }

    private void InvokeRendererDirty()
    {
        _renderedProperties.SetValue(new RenderedProperties
        {
            Bounds = _bounds ?? UnitBounds.Empty,
            Paint = new SKPaint
            {
                Color = new SKColor(255, 255, 255, (byte)(Math.Clamp(_opacity, 0, 1) * 255)),
                IsAntialias = true
            }
        });
        
        RendererDirty?.Invoke();
    }
}
