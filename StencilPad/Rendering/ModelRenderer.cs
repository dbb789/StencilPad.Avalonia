using Avalonia.Rendering.SceneGraph;
using SkiaSharp;
using StencilPad.Models.Resolvers;
using StencilPad.Spatial;

namespace StencilPad.Rendering;

public class ModelRenderer : IModelWalker, IWalkerRenderer
{
    private readonly IResourceSet _resourceSet;

    private List<IWalkerRenderer> _renderers;
    private SKMatrix? _matrix;
    private readonly object _lock;    

    public event Action? RendererDirty;

    public ModelRenderer(IResourceSet resourceSet)
    {
        _resourceSet = resourceSet;
        _renderers = new();
        _matrix = null;
        _lock = new();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var renderer in _renderers)
            {
                renderer.RendererDirty -= InvokeRendererDirty;
                renderer.Dispose();
            }
            
            _renderers.Clear();
        }
    }
    
    public IModelWalker CreateModelWalker()
    {
        return AddRenderer(new ModelRenderer(_resourceSet));
    }
    
    public IStyledGeometryWalker CreateStyledGeometryWalker()
    {
        return AddRenderer(new StyledGeometryRenderer(_resourceSet));
    }

    public ITextWalker CreateTextWalker()
    {
        return AddRenderer(new TextRenderer());
    }

    public IImageWalker CreateImageWalker()
    {
        return AddRenderer(new ImageRenderer());
    }

    private TWalkerRenderer AddRenderer<TWalkerRenderer>(TWalkerRenderer renderer)
        where TWalkerRenderer : IWalkerRenderer
    {
        renderer.RendererDirty += InvokeRendererDirty;

        lock (_lock)
        {
            _renderers.Add(renderer);
        }

        return renderer;
    }
    
    public void SetTransform(UnitTransform transform)
    {
        lock (_lock)
        {
            _matrix = transform.CreateMatrix();
        }
        
        InvokeRendererDirty();
    }
    
    public ICustomDrawOperation CreateDrawOperation()
    {
        PreRender();
        
        return new WalkerRendererDrawOperation(this);
    }

    public void PreRender()
    {
        lock (_lock)
        {
            foreach (var renderer in _renderers)
            {
                renderer.PreRender();
            }
        }
    }
    
    public void Render(SKCanvas canvas, GRContext? context)
    {
        lock (_lock)
        {
            if (_matrix is not null)
            {
                canvas.Save();
                canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, _matrix.Value));
            }

            foreach (var renderer in _renderers)
            {
                renderer.Render(canvas, context);
            }

            if (_matrix is not null)
            {
                canvas.Restore();
            }
        }
    }

    private void InvokeRendererDirty()
    {
        RendererDirty?.Invoke();
    }
}
