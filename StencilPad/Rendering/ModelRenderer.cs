using Avalonia.Rendering.SceneGraph;
using SkiaSharp;
using StencilPad.Collections;
using StencilPad.Models.Resolvers;
using StencilPad.Spatial;

namespace StencilPad.Rendering;

public class ModelRenderer : IModelWalker, IWalkerRenderer
{
    private readonly IResourceSet _resourceSet;

    private ConcurrentList<IWalkerRenderer> _renderers;
    
    private SKMatrix? _matrix;
    private readonly object _matrixLock;    

    public event Action? RendererDirty;

    public ModelRenderer(IResourceSet resourceSet)
    {
        _resourceSet = resourceSet;
        _renderers = new();
        _matrix = null;
        _matrixLock = new();
    }

    public void Dispose()
    {
        var renderers = _renderers.ToList();
        
        _renderers.Clear();
        
        foreach (var renderer in renderers)
        {
            renderer.RendererDirty -= InvokeRendererDirty;
            renderer.Dispose();
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

        _renderers.Add(renderer);

        return renderer;
    }
    
    public void SetTransform(UnitTransform transform)
    {
        lock (_matrixLock)
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
        foreach (var renderer in _renderers)
        {
            renderer.PreRender();
        }
    }
    
    public void Render(SKCanvas canvas, GRContext? context)
    {
        SKMatrix? matrix;

        lock (_matrixLock)
        {
            matrix = _matrix;
        }

        if (matrix is not null)
        {
            canvas.Save();
            canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, matrix.Value));
        }

        foreach (var renderer in _renderers)
        {
            renderer.Render(canvas, context);
        }

        if (matrix is not null)
        {
            canvas.Restore();
        }
    }
    
    private void InvokeRendererDirty()
    {
        RendererDirty?.Invoke();
    }
}
