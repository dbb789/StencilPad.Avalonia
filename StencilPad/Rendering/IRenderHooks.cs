using SkiaSharp;

namespace StencilPad.Rendering;

public interface IRenderHooks
{
    event Action? PreRenderHook;
    event Action<SKCanvas, GRContext?> OverlayRenderHook;

    void Redraw();
}
