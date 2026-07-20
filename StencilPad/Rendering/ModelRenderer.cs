using Avalonia.Media;
using StencilPad.Models.Resolvers;
using StencilPad.Spatial;

namespace StencilPad.Rendering;

public class ModelRenderer : IModelWalker, IWalkerRenderer
{
    private readonly IResourceSet _resourceSet;
    private readonly List<IWalkerRenderer> _renderers;
    private Transform? _transform;

    public event Action? RendererDirty;

    public ModelRenderer(IResourceSet resourceSet)
    {
        _resourceSet = resourceSet;
        _renderers = new();
    }

    public void Dispose()
    {
        foreach (var renderer in _renderers)
        {
            renderer.RendererDirty -= InvokeRendererDirty;
            renderer.Dispose();
        }

        _renderers.Clear();
    }

    public IModelWalker CreateModelWalker()
    {
        var renderer = new ModelRenderer(_resourceSet);
        
        renderer.RendererDirty += InvokeRendererDirty;

        _renderers.Add(renderer);
        
        return renderer;
    }
    
    public IStyledGeometryWalker CreateStyledGeometryWalker()
    {
        var renderer = new StyledGeometryRenderer(_resourceSet);
        
        renderer.RendererDirty += InvokeRendererDirty;

        _renderers.Add(renderer);
        
        return renderer;
    }

    public ITextWalker CreateTextWalker()
    {
        var renderer = new TextRenderer();
        
        renderer.RendererDirty += InvokeRendererDirty;

        _renderers.Add(renderer);
        
        return renderer;
    }

    public IImageWalker CreateImageWalker()
    {
        var renderer = new ImageRenderer();
        
        renderer.RendererDirty += InvokeRendererDirty;

        _renderers.Add(renderer);
        
        return renderer;
    }

    public void SetTransform(UnitTransform transform)
    {
        _transform = transform.CreateGroupTransform();
        InvokeRendererDirty();
    }
    
    public void Render(DrawingContext dc)
    {
        using var state = _transform is not null ? dc.PushTransform(_transform.Value) : default;

        foreach (var renderer in _renderers)
        {
            renderer.Render(dc);
        }
    }

    private void InvokeRendererDirty()
    {
        RendererDirty?.Invoke();
    }
}
