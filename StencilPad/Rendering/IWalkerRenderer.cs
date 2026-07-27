using SkiaSharp;

namespace StencilPad.Rendering;

public interface IWalkerRenderer : IDisposable
{
    event Action? RendererDirty;
    
    void Render(SKCanvas canvas, GRContext? context);
}
