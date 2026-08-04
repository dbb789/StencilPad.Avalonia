using SkiaSharp;
using StencilPad.Collections;
using StencilPad.Models;
using StencilPad.Rendering;

namespace StencilPad.Canvases.Tools.Overlays;

// This is a special case that's hardwired into ToolOverlay so that individual
// renderers don't have to worry about groups, and so that we can pass
// registered overlays down through the group hierarchy transparently.
public class GroupToolOverlayRenderer : IToolOverlayRenderer
{
    private class RenderedMatrix : IDisposable
    {
        public SKMatrix Matrix = SKMatrix.Identity;

        public void Reset()
        {
            Matrix = SKMatrix.Identity;
        }

        public void Dispose()
        {
            Reset();
        }
    }
    
    private readonly ElementGroup _group;
    
    private readonly ConcurrentList<IToolOverlayRendererFactory> _factories;
    private readonly ConcurrentOrderedDictionary<ISheetElement, IToolOverlayRenderer> _renderers;
    private readonly RenderBuffer<RenderedMatrix> _renderedMatrix;

    public event Action? RendererDirty;
    
    public GroupToolOverlayRenderer(ElementGroup group,
                                    IEnumerable<IToolOverlayRendererFactory> factories)
    {
        _group = group;
        _factories = new(factories);
        _renderers = new();
        _renderedMatrix = new();
        
        _group.ChildrenChanged += BuildRenderers;
        _group.TransformChanged += TransformChanged;
        
        BuildRenderers();

        using var renderedMatrix = _renderedMatrix.TryWrite();

        if (renderedMatrix.IsValid)
        {
            renderedMatrix.Buffer.Matrix = _group.Transform.CreateMatrix();
        }
    }

    public void Dispose()
    {
        ClearRenderers();
        
        _group.ChildrenChanged -= BuildRenderers;
        _group.TransformChanged -= TransformChanged;
    }

    public void PreRender()
    {
        foreach (var renderer in _renderers)
        {
            renderer.PreRender();
        }
    }

    public void Render(SKCanvas canvas, GRContext? context)
    {
        using var renderedMatrix = _renderedMatrix.TryRead();

        if (!renderedMatrix.IsValid)
        {
            return;
        }
        
        canvas.Save();
        canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, renderedMatrix.Buffer.Matrix));
        
        foreach (var renderer in _renderers)
        {
            renderer.Render(canvas, context);
        }
        
        canvas.Restore();
    }
    
    private void TransformChanged(ISheetElement element)
    {
        using var renderedMatrix = _renderedMatrix.TryWrite();

        if (renderedMatrix.IsValid)
        {
            renderedMatrix.Buffer.Matrix = _group.Transform.CreateMatrix();
        }

        InvokeRendererDirty();
    }

    private void BuildRenderers()
    {
        ClearRenderers();
        
        foreach (var child in _group.Children)
        {
            foreach (var factory in _factories)
            {
                var renderer = factory.CreateOverlay(child);
                
                if (renderer is not null)
                {
                    renderer.RendererDirty += InvokeRendererDirty;
                    _renderers.Add(child, renderer);
                    break;
                }
            }
        }

        InvokeRendererDirty();
    }

    private void ClearRenderers()
    {
        foreach (var (_, renderer) in _renderers.GetClear())
        {
            renderer.RendererDirty -= InvokeRendererDirty;
            renderer.Dispose();
        }
    }

    private void InvokeRendererDirty()
    {
        RendererDirty?.Invoke();
    }
}
