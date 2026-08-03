using SkiaSharp;

namespace StencilPad.Canvases.Tools.Overlays;

public interface IToolOverlayRenderer : IDisposable
{
    event Action? RendererDirty;

    void PreRender();
    void Render(SKCanvas canvas, GRContext? context);
}
