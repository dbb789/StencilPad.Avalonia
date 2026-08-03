using Avalonia.Rendering.SceneGraph;
using SkiaSharp;
using Microsoft.Extensions.Logging;
using StencilPad.Collections;
using StencilPad.Common;
using StencilPad.Models.Resolvers;

namespace StencilPad.Rendering;

public class SheetRenderer : IViewportRenderer, IDisposable, IRenderHooks
{
    public class Factory(ILogger<SheetRenderer> Logger,
                         ISettings Settings,
                         IResourceSet ResourceSet)
    {
        public SheetRenderer Create(SheetResolver resolver)
        {
            return new(Logger, resolver, Settings, ResourceSet);
        }
    }

    private readonly ILogger<SheetRenderer> _logger;
    private readonly SheetResolver _resolver;
    private readonly ISettings _settings;
    private readonly IResourceSet _resourceSet;
    private readonly ConcurrentOrderedDictionary<ISheetElementResolver, ModelRenderer> _renderers;

    public event Action? RendererDirty;

    public event Action? PreRenderHook;
    public event Action<SKCanvas, GRContext?>? ViewportRenderHook;
    public event Action<SKCanvas, GRContext?>? OverlayRenderHook;
    
    private SheetRenderer(ILogger<SheetRenderer> logger,
                          SheetResolver resolver,
                          ISettings settings,
                          IResourceSet resourceSet)
    {
        _logger = logger;
        _resolver = resolver;
        _settings = settings;
        _resourceSet = resourceSet;
        _renderers = new();

        int index = 0;
        
        foreach (var modelResolver in _resolver.Elements)
        {
            OnElementAdded(modelResolver, index++);
        }
        
        _resolver.ElementsChanged += OnElementsChanged;
    }

    public void Dispose()
    {
        foreach (var modelResolver in _resolver.Elements)
        {
            OnElementRemoved(modelResolver);
        }

        _resolver.ElementsChanged -= OnElementsChanged;
    }

    public ICustomDrawOperation CreateDrawOperation(SKMatrix viewportMatrix)
    {
        PreRender();
        
        return new ViewportRendererDrawOperation(this, viewportMatrix);
    }

    private void PreRender()
    {
        foreach (var renderer in _renderers)
        {
            renderer.PreRender();
        }

        PreRenderHook?.Invoke();
    }
    
    public void Render(SKCanvas canvas, GRContext? context, SKMatrix viewportMatrix)
    {
        canvas.Save();
        canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, viewportMatrix));
        
        foreach (var renderer in _renderers)
        {
            renderer.Render(canvas, context);
        }
        
        ViewportRenderHook?.Invoke(canvas, context);
        
        canvas.Restore();

        OverlayRenderHook?.Invoke(canvas, context);
    }
    
    private void OnElementsChanged(ObservableListChangedArgs<ISheetElementResolver> e)
    {
        switch (e.Action)
        {
        case ObservableListChangedAction.Add:
            OnElementAdded(e.Item, e.NewIndex);
            break;
            
        case ObservableListChangedAction.Remove:
            OnElementRemoved(e.Item);
            break;

        case ObservableListChangedAction.Move:
            OnElementMoved(e.OldIndex, e.NewIndex);
            break;
        }
    }

    private void OnElementAdded(ISheetElementResolver resolver, int index)
    {
        var renderer = new ModelRenderer(_resourceSet);

        renderer.RendererDirty += Redraw;
        resolver.Attach(renderer);
        _renderers.Insert(index, resolver, renderer);
        
        Redraw();
    }

    private void OnElementRemoved(ISheetElementResolver resolver)
    {   
        if (!_renderers.TryGetRemove(resolver, out var renderer))
        {
            _logger.LogError("Could not find renderer for resolver {ResolverType}.", resolver.GetType().Name);
            return;
        }

        renderer.RendererDirty -= Redraw;
        renderer.Dispose();
        
        Redraw();
    }

    private void OnElementMoved(int oldIndex, int newIndex)
    {
        _renderers.Move(oldIndex, newIndex);
        
        Redraw();
    }
    
    public void Redraw()
    {
        RendererDirty?.Invoke();
    }
}
