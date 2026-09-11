using System.Globalization;
using StencilPad.Spatial;

namespace StencilPad.Schemas;

public class UnitTransformSchema
{
    public Unit2D Pos { get; set; } = Unit2D.Zero;
    public string Ang { get; set; } = "0";

    public static UnitTransformSchema Pack(UnitTransform transform)
    {
        return new UnitTransformSchema
        {
            Pos = transform.Position,
            Ang = transform.Angle.ToString(CultureInfo.InvariantCulture)
        };
    }

    public static UnitTransform Unpack(UnitTransformSchema data)
    {
        if (!decimal.TryParse(data.Ang, NumberStyles.Float, CultureInfo.InvariantCulture, out var angle))
        {
            angle = 0;
        }
        
        return new UnitTransform
        {
            Position = data.Pos,
            Angle = angle
        };
    }
}
