using StencilPad.Spatial;

namespace StencilPad.UI.Widgets;

public class UnitTypeItem
{
    public UnitType Value { get; set; }
    public string Description { get; set; } = "";

    public override string ToString()
    {
        return Description;
    }
}
