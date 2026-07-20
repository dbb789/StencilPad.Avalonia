using Avalonia.Media;

namespace StencilPad.Rendering;

public interface IWalkerRenderer : IDisposable
{
    event Action? RendererDirty;
    
    void Render(DrawingContext dc);
}
