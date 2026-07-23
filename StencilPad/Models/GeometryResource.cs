using StencilPad.Spatial;
using SkiaSharp;

namespace StencilPad.Models;

public record GeometryResource
{
    public static readonly GeometryResource Empty =
        new GeometryResource(new SKPath(), new Shape(), Unit2D.Zero);
    
    public SKPath Path { get; init; }
    public Shape Shape { get; init; }
    public Unit2D Size { get; init; }

    public GeometryResource(SKPath path,
                            Shape shape,
                            Unit2D size)
    {
        Path = path;
        Shape = shape;
        Size = size;
    }
}
