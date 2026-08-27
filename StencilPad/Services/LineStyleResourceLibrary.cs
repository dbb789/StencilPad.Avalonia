using StencilPad.Models;
using StencilPad.Spatial;

namespace StencilPad.Services;

public static class LineStyleResourceLibrary
{
    public static readonly IReadOnlyList<LineStyle> ResourceList =
        [
            LineStyle.Solid,
            new LineStyle(Unit.FromMillimeters(1), Unit.FromMillimeters(1)),
            new LineStyle(Unit.FromMillimeters(0.5), Unit.FromMillimeters(0.5)),
            new LineStyle(Unit.FromMillimeters(0.25), Unit.FromMillimeters(0.25)),
            new LineStyle(Unit.FromMillimeters(2), Unit.FromMillimeters(1)),
            new LineStyle(Unit.FromMillimeters(2), Unit.FromMillimeters(2)),
            new LineStyle(Unit.FromMillimeters(3), Unit.FromMillimeters(1)),
            new LineStyle(Unit.FromMillimeters(3), Unit.FromMillimeters(3)),
            new LineStyle(Unit.FromMillimeters(1), Unit.FromMillimeters(2)),
            new LineStyle(Unit.FromMillimeters(0.5), Unit.FromMillimeters(0.5), Unit.FromMillimeters(1), Unit.FromMillimeters(0.5))
        ];
}
