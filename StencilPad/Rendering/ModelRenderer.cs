using Avalonia.Rendering.SceneGraph;
using SkiaSharp;
using StencilPad.Models.Resolvers;
using StencilPad.Spatial;

namespace StencilPad.Rendering;

public class ModelRenderer : IRenderer, IModelWalker, IWalkerRenderer
{
    private readonly IResourceSet _resourceSet;
    private readonly List<IWalkerRenderer> _renderers;
    private readonly object _renderersLock = new();
    
    private SKMatrix? _matrix;

    public event Action? RendererDirty;

    public ModelRenderer(IResourceSet resourceSet)
    {
        _resourceSet = resourceSet;
        _renderers = new();
    }

    public void Dispose()
    {
        List<IWalkerRenderer> renderersList;
        
        lock (_renderersLock)
        {
            renderersList = _renderers.ToList();
            _renderers.Clear();
        }
        
        foreach (var renderer in renderersList)
        {
            renderer.RendererDirty -= InvokeRendererDirty;
            renderer.Dispose();
        }
    }
    
    public IModelWalker CreateModelWalker()
    {
        var renderer = new ModelRenderer(_resourceSet);
        
        renderer.RendererDirty += InvokeRendererDirty;

        lock (_renderersLock)
        {
            _renderers.Add(renderer);
        }
        
        return renderer;
    }
    
    public IStyledGeometryWalker CreateStyledGeometryWalker()
    {
        var renderer = new StyledGeometryRenderer(_resourceSet);
        
        renderer.RendererDirty += InvokeRendererDirty;

        lock (_renderersLock)
        {
            _renderers.Add(renderer);
        }
        
        return renderer;
    }

    public ITextWalker CreateTextWalker()
    {
        var renderer = new TextRenderer();
        
        renderer.RendererDirty += InvokeRendererDirty;

        lock (_renderersLock)
        {
            _renderers.Add(renderer);
        }
        
        return renderer;
    }

    public IImageWalker CreateImageWalker()
    {
        var renderer = new ImageRenderer();
        
        renderer.RendererDirty += InvokeRendererDirty;

        lock (_renderersLock)
        {
            _renderers.Add(renderer);
        }
        
        return renderer;
    }

    public void SetTransform(UnitTransform transform)
    {
        _matrix = transform.CreateMatrix();
        InvokeRendererDirty();
    }
    
    public ICustomDrawOperation CreateDrawOperation()
    {
        return new RendererDrawOperation(this);
    }

    public void Render(SKCanvas canvas, GRContext? context)
    {
        if (_matrix is not null)
        {
            canvas.Save();
            canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, _matrix.Value));
        }

        lock (_renderersLock)
        {
            foreach (var renderer in _renderers)
            {
                renderer.Render(canvas, context);
            }
        }

        if (_matrix is not null)
        {
            canvas.Restore();
        }
    }

    private void InvokeRendererDirty()
    {
        RendererDirty?.Invoke();
    }
}
