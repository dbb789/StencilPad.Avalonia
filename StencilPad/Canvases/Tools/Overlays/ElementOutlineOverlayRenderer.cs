using System.ComponentModel;
using Avalonia.Skia;
using SkiaSharp;
using StencilPad.Models;
using StencilPad.Spatial;
using StencilPad.Rendering;

namespace StencilPad.Canvases.Tools.Overlays;

public abstract class ElementOutlineOverlayRenderer<TSheetElement> : IToolOverlayRenderer
    where TSheetElement : ISheetElement
{
    private static SKPaint OutlinePaint = new SKPaint
    {
        Color = new SKColor(0, 0, 0, 128),
        StrokeWidth = 2f,
        IsStroke = true,
        PathEffect = SKPathEffect.CreateDash(new float[] { 2f, 2f }, 0)
    };
    
    private class RenderedOutline : IDisposable
    {
        public SKMatrix Matrix = SKMatrix.Identity;
        public UnitBounds Bounds = UnitBounds.Empty;

        public void Dispose()
        {
            Matrix = SKMatrix.Identity;
            Bounds = UnitBounds.Empty;
        }
    }

    private readonly TSheetElement _element;

    private RenderBuffer<RenderedOutline> _renderedOutline;

    public event Action? RendererDirty;

    protected ElementOutlineOverlayRenderer(TSheetElement element)
    {
        _element = element;
        _element.GeometryChanged += GeometryChanged;
        _element.TransformChanged += OnTransformChanged;
        _element.PropertyChanged += OnPropertyChanged;
        _renderedOutline = new();
    }

    public void Dispose()
    {
        _element.GeometryChanged -= GeometryChanged;
        _element.TransformChanged -= OnTransformChanged;
        _element.PropertyChanged -= OnPropertyChanged;
        _renderedOutline.Dispose();
    }

    public void PreRender()
    {
        using var outlineHandle = _renderedOutline.TryWrite();

        if (!outlineHandle.IsValid)
        {
            return;
        }

        var outline = outlineHandle.Buffer;

        outline.Bounds = GetBounds(_element);
        outline.Matrix = _element.Transform.CreateMatrix();
    }

    public void Render(SKCanvas canvas, GRContext? context, SKMatrix transformMatrix)
    {
        using var outlineHandle = _renderedOutline.TryRead();

        if (!outlineHandle.IsValid)
        {
            return;
        }

        var outline = outlineHandle.Buffer;
        var rect = outline.Bounds.Millimeters.ToSKRect();

        rect = SKMatrix.Concat(transformMatrix, outline.Matrix).MapRect(rect);
        
        canvas.DrawRect(rect, OutlinePaint);
    }

    protected abstract UnitBounds GetBounds(TSheetElement element);
    
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
