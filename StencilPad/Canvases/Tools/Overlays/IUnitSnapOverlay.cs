using StencilPad.Canvases.Common;
using StencilPad.Spatial;

namespace StencilPad.Canvases.Tools.Overlays;

public interface IUnitSnapOverlay
{
    void Begin(IUnitSnapContext? context = null);
    void End();

    Unit2D? UnitSnap(Unit2D point);
}
