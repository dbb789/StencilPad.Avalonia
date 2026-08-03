using StencilPad.Canvases.Tools.Overlays;

namespace StencilPad.Canvases.Tools.Controllers;

public interface IToolFactory
{
    string IconResource { get; }
    string Tooltip { get; }
    
    ITool Create(IToolButton button);
}
