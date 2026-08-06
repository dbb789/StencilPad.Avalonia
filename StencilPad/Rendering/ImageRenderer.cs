using SkiaSharp;
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
    
    private class CachedImage : IDisposable
    {
        public GRContext? SourceContext;
        public SKImage? SourceImage;
        public SKImage? TargetImage;

        public void Dispose()
        {
            TargetImage?.Dispose();
            TargetImage = null;
        }
    }

    private UnitBounds? _bounds;
    private double _opacity = 1.0;

    private RenderBuffer<RenderedImage> _renderedImage;
    private RenderBuffer<RenderedProperties> _renderedProperties;
    private RenderCache<CachedImage> _cachedImage;

    public event Action? RendererDirty;
    
    public ImageRenderer()
    {
        _renderedImage = new();
        _renderedProperties = new();
        _cachedImage = new();
    }

    public void Dispose()
    {
        _renderedImage.Dispose();
        _renderedProperties.Dispose();
        _cachedImage.Dispose();
    }

    public void SetBounds(UnitBounds? bounds)
    {
        _bounds = bounds;
        
        InvokeRendererDirty();
    }
    
    public void SetImageData(byte[] imageData)
    {
        using var imageHandle = _renderedImage.TryWrite();

        if (!imageHandle.IsValid)
        {
            return;
        }

        imageHandle.Buffer.Image?.Dispose();
        imageHandle.Buffer.Image = ((imageData.Length > 0) ? SKImage.FromEncodedData(imageData) : null);

        InvokeRendererDirty();
    }

    public void SetOpacity(double opacity)
    {
        _opacity = opacity;
        
        InvokeRendererDirty();
    }
    
    public void PreRender()
    {
        // Nothing to do here - the image is already prepared in SetImageData.
    }

    public void Render(SKCanvas canvas, GRContext? context)
    {
        using var imageHandle = _renderedImage.TryRead();
        
        if (!imageHandle.IsValid)
        {
            return;
        }
        
        var image = imageHandle.Buffer.Image;

        if (image is null)
        {
            return;
        }

        using var cacheHandle = _cachedImage.TryUpdate();

        if (!cacheHandle.IsValid)
        {
            return;
        }

        var cache = cacheHandle.Buffer;

        // NOTE: We're only comparing references here - the underlying source
        // image may have been disposed so we shouldn't read the content.
        if (cache.SourceImage != image || cache.SourceContext != context)
        {
            cache.SourceImage = image;
            cache.SourceContext = context;
            
            cache.TargetImage?.Dispose();
            cache.TargetImage = (context is not null) ?
                image.ToTextureImage(context) : image.ToRasterImage();
        }

        var targetImage = cacheHandle.Buffer.TargetImage;

        if (targetImage is null)
        {
            return;
        }
        
        using var propertiesHandle = _renderedProperties.TryRead();
        
        if (!propertiesHandle.IsValid)
        {
            return;
        }
        
        var properties = propertiesHandle.Buffer;
        
        var bounds = properties.Bounds;
        var rect = new SKRect((float)bounds.SW.X.Millimeters,
                              -(float)bounds.NE.Y.Millimeters,
                              (float)bounds.NE.X.Millimeters,
                              -(float)bounds.SW.Y.Millimeters);
        
        canvas.Save();
        canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, SKMatrix.CreateScale(1, -1)));
        canvas.DrawImage(targetImage, rect, properties.Paint);
        canvas.Restore();
    }

    private void InvokeRendererDirty()
    {
        using var propertiesHandle = _renderedProperties.TryWrite();

        if (!propertiesHandle.IsValid)
        {
            return;
        }
        
        var properties = propertiesHandle.Buffer;

        properties.Bounds = _bounds ?? UnitBounds.Empty;
        properties.Paint.Color = new SKColor(255, 255, 255, (byte)(Math.Clamp(_opacity, 0, 1) * 255));
        properties.Paint.IsAntialias = true;
        
        RendererDirty?.Invoke();
    }
}
