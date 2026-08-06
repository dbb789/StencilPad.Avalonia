using StencilPad.Models;
using StencilPad.Spatial;

namespace StencilPad.Services;

public static class LineStyleResourceLibrary
{
    public static readonly IReadOnlyList<LineStyle> ResourceList =
        [
            LineStyle.Solid,
            new LineStyle(Unit.FromMillimeters(1), Unit.FromMillimeters(1)),
        ];
}
