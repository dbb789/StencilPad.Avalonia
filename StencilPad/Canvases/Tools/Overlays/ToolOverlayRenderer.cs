using SkiaSharp;
using StencilPad.Collections;
using StencilPad.Models;
using StencilPad.Rendering;

namespace StencilPad.Canvases.Tools.Overlays;

public class ToolOverlayRenderer
{
    protected Sheet Sheet => _sheet;
    
    private readonly Sheet _sheet;
    private readonly IRenderHooks _renderHooks;
    private readonly bool _selectionOnly;

    private readonly ConcurrentList<IToolOverlayRendererFactory> _factories;
    private readonly ConcurrentOrderedDictionary<ISheetElement, IToolOverlayRenderer> _renderers;
    
    public ToolOverlayRenderer(Sheet sheet,
                               IRenderHooks renderHooks,
                               bool selectionOnly,
                               IEnumerable<IToolOverlayRendererFactory> factories)
    {
        _sheet = sheet;
        _renderHooks = renderHooks;
        _selectionOnly = selectionOnly;
        _factories = new(factories);
        _renderers = new();

        foreach (var element in GetElements())
        {
            AddRenderer(element);
        }

        _renderHooks.PreRenderHook += PreRender;
        _renderHooks.OverlayRenderHook += RenderOverlay;
        
        GetList().ListChanged += ElementsChanged;        
    }

    public void Dispose()
    {
        GetList().ListChanged -= ElementsChanged;
        
        _renderHooks.PreRenderHook -= PreRender;
        _renderHooks.OverlayRenderHook -= RenderOverlay;

        foreach (var (_, renderer) in _renderers.GetClear())
        {
            renderer.RendererDirty -= ForceRedraw;
            renderer.Dispose();
        }
    }

    private IEnumerable<ISheetElement> GetElements()
    {
        return _selectionOnly ? _sheet.Selection : _sheet.Elements;
    }
    
    private IObservableList<ISheetElement> GetList()
    {
        return _selectionOnly ? _sheet.Selection : _sheet.Elements;
    }

    private void PreRender()
    {
        foreach (var  renderer in _renderers)
        {
            renderer.PreRender();
        }
    }
    
    private void RenderOverlay(SKCanvas canvas, GRContext? context, SKMatrix viewportMatrix)
    {
        foreach (var renderer in _renderers)
        {
            renderer.Render(canvas, context, viewportMatrix);
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
        if (_renderers.TryGetRemove(element, out var renderer))
        {
            renderer.RendererDirty -= ForceRedraw;
            renderer.Dispose();
        }

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
