using System.ComponentModel;
using Avalonia.Skia;
using SkiaSharp;
using StencilPad.Models;
using StencilPad.Spatial;

namespace StencilPad.Canvases.Tools.Overlays;

public class TextElementToolOverlayRenderer : IToolOverlayRenderer
{
    public static readonly IToolOverlayRendererFactory Factory = new FactoryImpl();
    
    private class FactoryImpl : IToolOverlayRendererFactory
    {
        public IToolOverlayRenderer? CreateOverlay(ISheetElement element)
        {
            if (element is TextElement textElement)
            {
                return new TextElementToolOverlayRenderer(textElement);
            }

            return null;
        }
    }
    
    private static SKPaint OutlinePaint = new SKPaint
    {
        Color = new SKColor(0, 0, 0, 128),
        StrokeWidth = 0.2f,
        IsStroke = true,
        PathEffect = SKPathEffect.CreateDash(new float[] { 0.2f, 0.2f }, 0)
    };

    private readonly TextElement _textElement;
    
    private SKRect _outline;
    private object _outlineLock;

    public event Action? RendererDirty;

    private TextElementToolOverlayRenderer(TextElement textElement)
    {
        _textElement = textElement;
        _textElement.GeometryChanged += GeometryChanged;
        _textElement.TransformChanged += OnTransformChanged;
        _textElement.PropertyChanged += OnPropertyChanged;

        _outline = SKRect.Empty;
        _outlineLock = new();
    }

    public void Dispose()
    {
        _textElement.GeometryChanged -= GeometryChanged;
        _textElement.TransformChanged -= OnTransformChanged;
        _textElement.PropertyChanged -= OnPropertyChanged;
    }

    public void PreRender()
    {
        var bounds = UnitBounds.FromMinMax(_textElement.Min, _textElement.Max);
        var matrix = _textElement.Transform.CreateMatrix();
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

    private void OnTransformChanged(ISheetElement element)
    {
        InvokeRendererDirty();
    }

    private void GeometryChanged(ISheetElement element)
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
