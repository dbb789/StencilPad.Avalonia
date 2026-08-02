using SkiaSharp;
using StencilPad.Collections;
using StencilPad.Models;
using StencilPad.Rendering;

namespace StencilPad.Canvases.Tools.Overlays;

public class ToolOverlay
{
    protected Sheet Sheet => _sheet;
    
    private readonly Sheet _sheet;
    private readonly IRenderHooks _renderHooks;
    private readonly bool _selectionOnly;
    private readonly List<IToolOverlayRendererFactory> _factories;
    private readonly Dictionary<ISheetElement, IToolOverlayRenderer> _renderers;
    
    public ToolOverlay(Sheet sheet,
                       IRenderHooks renderHooks,
                       bool selectionOnly)
    {
        _sheet = sheet;
        _renderHooks = renderHooks;
        _selectionOnly = selectionOnly;
        _factories = new();
        _renderers = new();

        foreach (var element in GetElements())
        {
            AddRenderer(element);
        }

        _renderHooks.PreRenderHook += PreRender;
        _renderHooks.ViewportRenderHook += RenderOverlay;
        
        GetList().ListChanged += ElementsChanged;        
    }

    public virtual void Dispose()
    {
        GetList().ListChanged -= ElementsChanged;
        
        _renderHooks.PreRenderHook -= PreRender;
        _renderHooks.ViewportRenderHook -= RenderOverlay;

        foreach (var (_, renderer) in _renderers)
        {
            renderer.RendererDirty -= ForceRedraw;
            renderer.Dispose();
        }

        _renderers.Clear();
    }

    private IEnumerable<ISheetElement> GetElements()
    {
        return _selectionOnly ? _sheet.Selection : _sheet.Elements;
    }
    
    private IObservableList<ISheetElement> GetList()
    {
        return _selectionOnly ? _sheet.Selection : _sheet.Elements;
    }

    public void RegisterOverlay(IToolOverlayRendererFactory factory)
    {
        _factories.Add(factory);

        foreach (var element in GetElements())
        {
            if (!_renderers.ContainsKey(element))
            {
                var renderer = factory.CreateOverlay(element);

                if (renderer is not null)
                {
                    renderer.RendererDirty += ForceRedraw;
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

    private void PreRender()
    {
        foreach (var (_, renderer) in _renderers)
        {
            renderer.PreRender();
        }
    }
    
    private void RenderOverlay(SKCanvas canvas, GRContext? context)
    {
        foreach (var (_, renderer) in _renderers)
        {
            renderer.Render(canvas, context);
        }
    }
    
    private void ElementsChanged(ObservableListChangedArgs<ISheetElement> e)
    {
        // NOTE: We're currently ignoring ordering here as it generally shouldn't matter.
        switch (e.Action)
        {
        case ObservableListChangedAction.Add:
            AddRenderer(e.Item);
            break;
            
        case ObservableListChangedAction.Remove:
            RemoveRenderer(e.Item);
            break;
        }
    }
    
    private void AddRenderer(ISheetElement element)
    {
        IToolOverlayRenderer? renderer;
        
        if (element is ElementGroup group)
        {
            // Special case for groups.
            renderer = new GroupToolOverlayRenderer(group, _factories);
        }
        else
        {
            renderer = CreateRenderer(element);
        }
        
        if (renderer is null)
        {
            return;
        }

        renderer.RendererDirty += ForceRedraw;

        _renderers.Add(element, renderer);

        ForceRedraw();
    }

    private void RemoveRenderer(ISheetElement element)
    {
        if (!_renderers.TryGetValue(element, out var renderer))
        {
            return;
        }
        
        renderer.RendererDirty -= ForceRedraw;
        renderer.Dispose();
        _renderers.Remove(element);

        ForceRedraw();
    }
    
    private IToolOverlayRenderer? CreateRenderer(ISheetElement element)
    {
        foreach (var factory in _factories)
        {
            var renderer = factory.CreateOverlay(element);

            if (renderer is not null)
            {
                return renderer;
            }
        }

        return null;
    }
    
    private void ForceRedraw()
    {
        _renderHooks.Redraw();
    }
}
