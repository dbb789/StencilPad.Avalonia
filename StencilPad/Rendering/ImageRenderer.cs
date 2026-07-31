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

    private UnitBounds? _bounds;
    private double _opacity = 1.0;

    private TripleBuffer<RenderedImage> _renderedImage;
    private TripleBuffer<RenderedProperties> _renderedProperties;
    
    public event Action? RendererDirty;
    
    public ImageRenderer()
    {
        _renderedImage = new();
        _renderedProperties = new();
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
        using var imageHandle = _renderedImage.Write();

        imageHandle.Buffer.Image = ((imageData.Length > 0) ? SKImage.FromEncodedData(imageData) : null);

        InvokeRendererDirty();
    }

    public void SetOpacity(double opacity)
    {
        _opacity = opacity;
        
        InvokeRendererDirty();
    }
    
    private SKImage? _image;
    private SKImage? _renderImage;
    private object _renderImageLock = new();
    
    public void Render(SKCanvas canvas, GRContext? context)
    {
        using var imageHandle = _renderedImage.Read();

        var image = imageHandle.Buffer;

        // Lock should be unnecessary here - this is really just for safety.
        lock (_renderImageLock)
        {
            if (image.Image is null)
            {
                _renderImage?.Dispose();
                return;
            }

            if (_image != image.Image)
            {
                _image = image.Image;
                _renderImage?.Dispose();
                _renderImage = (context is not null) ?
                    _image.ToTextureImage(context) : _image.ToRasterImage();
            }

            if (_renderImage is null)
            {
                return;
            }

            using var propertiesHandle = _renderedProperties.Read();

            var properties = propertiesHandle.Buffer;

            var bounds = properties.Bounds;
            var rect = new SKRect((float)bounds.SW.X.Millimeters,
                                  -(float)bounds.NE.Y.Millimeters,
                                  (float)bounds.NE.X.Millimeters,
                                  -(float)bounds.SW.Y.Millimeters);

            canvas.Save();
            canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, SKMatrix.CreateScale(1, -1)));
            canvas.DrawImage(_renderImage, rect, properties.Paint);
            canvas.Restore();
        }
    }

    private void InvokeRendererDirty()
    {
        using var propertiesHandle = _renderedProperties.Write();
        
        var properties = propertiesHandle.Buffer;

        properties.Bounds = _bounds ?? UnitBounds.Empty;
        properties.Paint.Color = new SKColor(255, 255, 255, (byte)(Math.Clamp(_opacity, 0, 1) * 255));
        properties.Paint.IsAntialias = true;
        
        RendererDirty?.Invoke();
    }
}
