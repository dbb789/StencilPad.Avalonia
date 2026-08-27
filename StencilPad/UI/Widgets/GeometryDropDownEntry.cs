using SkiaSharp;

namespace StencilPad.UI.Widgets;

public sealed record GeometryDropdownEntry(SKPath Path, SKPaint? Paint = null);
