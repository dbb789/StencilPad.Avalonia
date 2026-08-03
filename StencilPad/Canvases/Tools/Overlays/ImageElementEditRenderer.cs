using System.ComponentModel;
using Avalonia.Skia;
using SkiaSharp;
using StencilPad.Models;
using StencilPad.Spatial;

namespace StencilPad.Canvases.Tools.Overlays;

public class ImageElementToolOverlayRenderer : IToolOverlayRenderer
{
    public static readonly IToolOverlayRendererFactory Factory = new FactoryImpl();
    
    private class FactoryImpl : IToolOverlayRendererFactory
    {
        public IToolOverlayRenderer? CreateOverlay(ISheetElement element)
        {
            if (element is ImageElement imageElement)
            {
                return new ImageElementToolOverlayRenderer(imageElement);
            }

            return null;
        }
    }
    
    private static SKPaint OutlinePaint = new SKPaint
    {
        Color = new SKColor(0, 0, 0, 128),
        StrokeWidth = 0.2f,
        IsStroke = true,
        PathEffect = SKPathEffect.CreateDash(new float[] { 2, 2 }, 0)
    };
    
    private readonly ImageElement _imageElement;

    private SKRect _outline;
    private object _outlineLock;

    public event Action? RendererDirty;

    private ImageElementToolOverlayRenderer(ImageElement imageElement)
    {
        _imageElement = imageElement;
        _imageElement.GeometryChanged += OnGeometryChanged;
        _imageElement.TransformChanged += OnTransformChanged;
        _imageElement.PropertyChanged += OnPropertyChanged;

        _outline = SKRect.Empty;
        _outlineLock = new();
    }

    public void Dispose()
    {
        _imageElement.GeometryChanged -= OnGeometryChanged;
        _imageElement.TransformChanged -= OnTransformChanged;
        _imageElement.PropertyChanged -= OnPropertyChanged;
    }

    public void PreRender()
    {
        var bounds = UnitBounds.FromMinMax(_imageElement.Min, _imageElement.Max);
        var matrix = _imageElement.Transform.CreateMatrix();
        var outline = bounds.Millimeters.ToSKRect();
        
        outline = matrix.MapRect(outline);

        lock (_outlineLock)
        {
            _outline = outline;
        }
    }

    public void Render(SKCanvas canvas, GRContext? context)
    {
        SKRect outline;

        lock (_outlineLock)
        {
            outline = _outline;
        }

        if (outline.IsEmpty)
        {
            return;
        }

        canvas.DrawRect(outline, OutlinePaint);
    }

    private void OnTransformChanged(ISheetElement _)
    {
        InvokeRendererDirty();
    }
    
    private void OnGeometryChanged(ISheetElement _)
    {
        InvokeRendererDirty();
    }
    
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvokeRendererDirty();
    }

    private void InvokeRendererDirty()
    {
        RendererDirty?.Invoke();
    }
}
