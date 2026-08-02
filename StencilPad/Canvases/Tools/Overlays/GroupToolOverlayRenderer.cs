using SkiaSharp;
using StencilPad.Models;

namespace StencilPad.Canvases.Tools.Overlays;

// This is a special case that's hardwired into ToolOverlay so that individual
// renderers don't have to worry about groups, and so that we can pass
// registered overlays down through the group hierarchy transparently.
public class GroupToolOverlayRenderer : IToolOverlayRenderer
{
    private readonly ElementGroup _group;
    private readonly List<IToolOverlayRendererFactory> _factories;

    private readonly Dictionary<ISheetElement, IToolOverlayRenderer> _renderers;
    private SKMatrix _matrix;
    private object _lock;
    
    public event Action? RendererDirty;
    
    public GroupToolOverlayRenderer(ElementGroup group,
                                    List<IToolOverlayRendererFactory> factories)
    {
        _group = group;
        _factories = new(factories);
        _renderers = new();
        _matrix = _group.Transform.CreateMatrix();
        _lock = new();
        
        _group.ChildrenChanged += BuildRenderers;
        _group.TransformChanged += TransformChanged;
        
        BuildRenderers();
    }

    public void Dispose()
    {
        ClearRenderers();
        
        _group.ChildrenChanged -= BuildRenderers;
        _group.TransformChanged -= TransformChanged;
    }

    public void PreRender()
    {
        lock (_lock)
        {
            foreach (var renderer in _renderers.Values)
            {
                renderer.PreRender();
            }
        }
    }

    public void Render(SKCanvas canvas, GRContext? context)
    {
        lock (_lock)
        {
            canvas.Save();
            canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, _matrix));

            foreach (var renderer in _renderers.Values)
            {
                renderer.Render(canvas, context);
            }

            canvas.Restore();
        }
    }
    
    public void RegisterOverlay(IToolOverlayRendererFactory factory)
    {
        lock (_lock)
        {
            _factories.Add(factory);

            foreach (var element in _group.Children)
            {
                if (!_renderers.ContainsKey(element))
                {
                    var renderer = factory.CreateOverlay(element);

                    if (renderer is not null)
                    {
                        renderer.RendererDirty += InvokeRendererDirty;
                        _renderers.Add(element, renderer);
                    }
                }
            }

            foreach (var renderer in _renderers.Values)
            {
                if (renderer is GroupToolOverlayRenderer groupRenderer)
                {
                    groupRenderer.RegisterOverlay(factory);
                }
            }
        }
    }
    
    private void TransformChanged(ISheetElement element)
    {
        lock (_lock)
        {
            _matrix = _group.Transform.CreateMatrix();
        }
        
        InvokeRendererDirty();
    }

    private void BuildRenderers()
    {
        lock (_lock)
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
        }

        InvokeRendererDirty();
    }

    private void ClearRenderers()
    {
        lock (_lock)
        {
            foreach (var renderer in _renderers.Values)
            {
                renderer.RendererDirty -= InvokeRendererDirty;
                renderer.Dispose();
            }

            _renderers.Clear();
        }
    }

    private void InvokeRendererDirty()
    {
        RendererDirty?.Invoke();
    }
}
