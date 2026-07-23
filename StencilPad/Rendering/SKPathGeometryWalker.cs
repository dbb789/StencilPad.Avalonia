using Avalonia.Media;
using SkiaSharp;
using StencilPad.Spatial;

namespace StencilPad.Rendering;

public class SKPathGeometryWalker() : IGeometryWalker
{
    public SKPath Path = null!;

    public Unit2D StartPosition => _startPosition;
    public Unit2D EndPosition => _endPosition;
    
    private bool _closed;
    private bool _figureStarted;
    private Unit2D _startPosition;
    private Unit2D _endPosition;
    
    public bool Begin(int segmentCount, bool closed)
    {
        _closed = closed;
        _figureStarted = false;

        return true;
    }

    public bool Segment(int segmentIndex, PolygonSegment segment)
    {
        if (segment.IsLine)
        {
            var line = segment.Line;

            EnsureFigure(line.Start, line.End);

            Path.LineTo(Point(line.End));

            return true;
        }

        if (segment.IsArc)
        {
            var arc = segment.Arc;

            EnsureFigure(arc.Start, arc.End);

            var radius = arc.Radius.Millimeters;
            var angle = MathUtil.SignedAngleDifference(arc.EndAngle, arc.StartAngle);
            var sweep = angle > 0 ? SKPathDirection.CounterClockwise : SKPathDirection.Clockwise;

            Path.ArcTo(new SKPoint((float)radius, (float)radius),
                       (float)angle,
                       SKPathArcSize.Small,
                       sweep,
                       Point(arc.End));

            return true;
        }

        if (segment.IsBezier)
        {
            var bezier = segment.Bezier;

            EnsureFigure(bezier.P0, bezier.P3);

            Path.CubicTo(Point(bezier.P1),
                         Point(bezier.P2),
                         Point(bezier.P3));

            return true;
        }

        throw new InvalidOperationException("Unknown polygon segment type.");
    }

    private void EnsureFigure(Unit2D from, Unit2D to)
    {
        _endPosition = to;
        
        if (_figureStarted)
        {
            return;
        }

        _startPosition = from;
        Path.MoveTo(Point(from));
        _figureStarted = true;
    }

    public void End()
    {
        if (_figureStarted)
        {
            if (_closed)
            {
                Path.Close();
            }
            
            _figureStarted = false;
        }
    }

    private SKPoint Point(Unit2D point)
    {
        return new SKPoint((float)point.X.Millimeters, (float)point.Y.Millimeters);
    }
}
