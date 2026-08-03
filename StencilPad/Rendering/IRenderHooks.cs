using SkiaSharp;

namespace StencilPad.Rendering;

public interface IRenderHooks
{
    event Action? PreRenderHook;
    event Action<SKCanvas, GRContext?> ViewportRenderHook;
    event Action<SKCanvas, GRContext?, SKMatrix> OverlayRenderHook;

    void Redraw();
}
