using StencilPad.Spatial;

namespace StencilPad.Canvases.Common;

public class EmptyUnitSnapContext : BaseUnitSnapContext
{
    public override IViewport Viewport { get; }
    
    public EmptyUnitSnapContext(IViewport viewport)
    {
        Viewport = viewport;
    }
}
