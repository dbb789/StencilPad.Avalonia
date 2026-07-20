using Avalonia.Media;
using StencilPad.Models;

namespace StencilPad.Services;

public static class LineStyleResourceLibrary
{
    public static readonly IReadOnlyList<(LineStyleResourceId, DashStyle)> ResourceList =
        new List<(LineStyleResourceId, DashStyle)>
        {
            // NOTE: Avalonia has no DashStyles.Solid/Dash statics like WPF - a DashStyle with
            // no Dashes set renders as a solid line, so this reproduces the same two styles.
            ( LineStyleResourceId.Solid, new DashStyle() ),
            ( LineStyleResourceId.Dashes, new DashStyle(new double[] { 2, 2 }, 1) )
        };
}
