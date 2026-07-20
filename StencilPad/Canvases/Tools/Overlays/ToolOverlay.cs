using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Reactive;
using StencilPad.Spatial;
using StencilPad.Models;
using StencilPad.Collections;

namespace StencilPad.Canvases.Tools.Overlays;

public abstract class ToolOverlay : Canvas
{
    // NOTE: Avalonia's Panel (Canvas's base class) seals Render(DrawingContext),
    // unlike WPF's FrameworkElement where OnRender is freely overridable on a
    // Canvas subclass. To let derived overlay classes still draw custom content
    // (grid lines, handles, previews, etc.) on top of/under their child widgets,
    // we add an internal full-size Control as the first child and route all
    // drawing through it via the virtual RenderOverlayContent() hook below -
    // derived classes should override that instead of Render/OnRender.
    private sealed class RenderSurface : Control
    {
        private readonly ToolOverlay _owner;

        public RenderSurface(ToolOverlay owner)
        {
            _owner = owner;
        }

        public override void Render(DrawingContext context)
        {
            _owner.RenderOverlayContent(context);
        }
    }

    private readonly RenderSurface _renderSurface;

    protected Sheet Sheet => _sheet;
    
    private readonly IViewport _viewport;
    private readonly Sheet _sheet;
    private readonly bool _selectionOnly;
    private readonly List<IToolOverlayRendererFactory> _factories;
    private readonly Dictionary<ISheetElement, IToolOverlayRenderer> _renderers;
    
    public ToolOverlay(IViewport viewport, Sheet sheet, bool selectionOnly)
    {
        _viewport = viewport;
        _sheet = sheet;
        _selectionOnly = selectionOnly;
        _factories = new();
        _renderers = new();

        _renderSurface = new RenderSurface(this);
        Children.Add(_renderSurface);

        this.GetObservable(BoundsProperty).Subscribe(new AnonymousObserver<Rect>(bounds =>
        {
            _renderSurface.Width = bounds.Width;
            _renderSurface.Height = bounds.Height;
        }));

        foreach (var element in GetElements())
        {
            AddRenderer(element);
        }

        GetList().ListChanged += ElementsChanged;        
    }

    /// <summary>
    /// Derived overlay classes should override this instead of Render/OnRender
    /// to draw custom content, since Avalonia's Canvas/Panel seals Render().
    /// </summary>
    protected virtual void RenderOverlayContent(DrawingContext dc)
    {
    }

    public virtual void Dispose()
    {
        GetList().ListChanged -= ElementsChanged;
        
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

    protected void RegisterOverlay(IToolOverlayRendererFactory factory)
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

    protected void RenderOverlay(DrawingContext dc)
    {
        using var state = dc.PushTransform(_viewport.MillimetersToPixelsTransform.Value);

        foreach (var (_, renderer) in _renderers)
        {
            renderer.Render(dc);
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

    protected new void InvalidateVisual()
    {
        _renderSurface.InvalidateVisual();
    }

    protected void ForceRedraw()
    {
        InvalidateVisual();
    }
}
