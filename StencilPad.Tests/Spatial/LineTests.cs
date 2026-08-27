namespace StencilPad.Tests.Spatial;

using StencilPad.Spatial;

public class LineTests
{
    private static Unit U(double v) => Unit.FromMillimeters(v);
    private static Unit2D U2(double x, double y) => new(U(x), U(y));

    [Test]
    public void Start_ReturnsConstructedStartPoint()
    {
        var line = new Line(U2(1, 2), U2(5, 6));
        Assert.Multiple(() =>
        {
            Assert.That(line.Start.X.Millimeters, Is.EqualTo(1));
            Assert.That(line.Start.Y.Millimeters, Is.EqualTo(2));
        });
    }

    [Test]
    public void End_ReturnsConstructedEndPoint()
    {
        var line = new Line(U2(1, 2), U2(5, 6));
        Assert.Multiple(() =>
        {
            Assert.That(line.End.X.Millimeters, Is.EqualTo(5));
            Assert.That(line.End.Y.Millimeters, Is.EqualTo(6));
        });
    }

    [Test]
    public void Length_HorizontalLine_ReturnsCorrectLength()
    {
        var line = new Line(U2(0, 0), U2(10, 0));
        Assert.That(line.Length.Millimeters, Is.EqualTo(10).Within(MathUtil.Epsilon));
    }

    [Test]
    public void Length_DiagonalLine_ReturnsPythagoreanLength()
    {
        // 3-4-5 right triangle
        var line = new Line(U2(0, 0), U2(3, 4));
        Assert.That(line.Length.Millimeters, Is.EqualTo(5).Within(MathUtil.Epsilon));
    }

    [Test]
    public void Length_ZeroLengthLine_ReturnsZero()
    {
        var line = new Line(U2(5, 5), U2(5, 5));
        Assert.That(line.Length.Millimeters, Is.EqualTo(0).Within(MathUtil.Epsilon));
    }

    [Test]
    public void Reversed_SwapsStartAndEnd()
    {
        var line = new Line(U2(1, 2), U2(5, 6));
        var reversed = line.Reversed;
        Assert.Multiple(() =>
        {
            Assert.That(reversed.Start.X.Millimeters, Is.EqualTo(5));
            Assert.That(reversed.Start.Y.Millimeters, Is.EqualTo(6));
            Assert.That(reversed.End.X.Millimeters, Is.EqualTo(1));
            Assert.That(reversed.End.Y.Millimeters, Is.EqualTo(2));
        });
    }

    [Test]
    public void At_Zero_ReturnsStart()
    {
        var line = new Line(U2(0, 0), U2(10, 20));
        var point = line.At(0.0);
        Assert.Multiple(() =>
        {
            Assert.That(point.X.Millimeters, Is.EqualTo(0).Within(MathUtil.Epsilon));
            Assert.That(point.Y.Millimeters, Is.EqualTo(0).Within(MathUtil.Epsilon));
        });
    }

    [Test]
    public void At_One_ReturnsEnd()
    {
        var line = new Line(U2(0, 0), U2(10, 20));
        var point = line.At(1.0);
        Assert.Multiple(() =>
        {
            Assert.That(point.X.Millimeters, Is.EqualTo(10).Within(MathUtil.Epsilon));
            Assert.That(point.Y.Millimeters, Is.EqualTo(20).Within(MathUtil.Epsilon));
        });
    }

    [Test]
    public void At_Half_ReturnsMidpoint()
    {
        var line = new Line(U2(0, 0), U2(10, 20));
        var point = line.At(0.5);
        Assert.Multiple(() =>
        {
            Assert.That(point.X.Millimeters, Is.EqualTo(5).Within(MathUtil.Epsilon));
            Assert.That(point.Y.Millimeters, Is.EqualTo(10).Within(MathUtil.Epsilon));
        });
    }

    [Test]
    public void At_Quarter_ReturnsCorrectPoint()
    {
        var line = new Line(U2(0, 0), U2(8, 4));
        var point = line.At(0.25);
        Assert.Multiple(() =>
        {
            Assert.That(point.X.Millimeters, Is.EqualTo(2).Within(MathUtil.Epsilon));
            Assert.That(point.Y.Millimeters, Is.EqualTo(1).Within(MathUtil.Epsilon));
        });
    }

    [Test]
    public void Deriv_ReturnsEndMinusStart()
    {
        var line = new Line(U2(1, 2), U2(7, 10));
        var deriv = line.Deriv(0.5);
        Assert.Multiple(() =>
        {
            Assert.That(deriv.X.Millimeters, Is.EqualTo(6).Within(MathUtil.Epsilon));
            Assert.That(deriv.Y.Millimeters, Is.EqualTo(8).Within(MathUtil.Epsilon));
        });
    }

    [Test]
    public void Deriv_IsConstantAlongLine()
    {
        var line = new Line(U2(0, 0), U2(4, 3));
        var d0 = line.Deriv(0.0);
        var d1 = line.Deriv(0.5);
        var d2 = line.Deriv(1.0);
        Assert.Multiple(() =>
        {
            Assert.That(d0, Is.EqualTo(d1));
            Assert.That(d1, Is.EqualTo(d2));
        });
    }

    [Test]
    public void DistanceTo_PointOnLine_ReturnsZero()
    {
        var line = new Line(U2(0, 0), U2(10, 0));
        Assert.That(line.DistanceTo(U2(5, 0)).Millimeters, Is.EqualTo(0).Within(MathUtil.Epsilon));
    }

    [Test]
    public void DistanceTo_PointPerpendicularToMidpoint_ReturnsPerpendicularDistance()
    {
        var line = new Line(U2(0, 0), U2(10, 0));
        Assert.That(line.DistanceTo(U2(5, 3)).Millimeters, Is.EqualTo(3).Within(MathUtil.Epsilon));
    }

    [Test]
    public void DistanceTo_PointBeyondEnd_ClampsToEndpoint()
    {
        // Closest point is the end vertex, not the infinite-line projection
        var line = new Line(U2(0, 0), U2(10, 0));
        Assert.That(line.DistanceTo(U2(15, 0)).Millimeters, Is.EqualTo(5).Within(MathUtil.Epsilon));
    }

    [Test]
    public void DistanceTo_PointBeyondStart_ClampsToStartpoint()
    {
        var line = new Line(U2(0, 0), U2(10, 0));
        Assert.That(line.DistanceTo(U2(-4, 0)).Millimeters, Is.EqualTo(4).Within(MathUtil.Epsilon));
    }

    [Test]
    public void DistanceTo_PointAtStart_ReturnsZero()
    {
        var line = new Line(U2(3, 7), U2(10, 7));
        Assert.That(line.DistanceTo(U2(3, 7)).Millimeters, Is.EqualTo(0).Within(MathUtil.Epsilon));
    }

    [Test]
    public void DistanceTo_ZeroLengthLine_ReturnsDistanceToStartPoint()
    {
        var line = new Line(U2(5, 5), U2(5, 5));
        // 3-4-5 triangle: distance from (5,5) to (8,9) = sqrt(9+16) = 5
        Assert.That(line.DistanceTo(U2(8, 9)).Millimeters, Is.EqualTo(5).Within(MathUtil.Epsilon));
    }

    [Test]
    public void DistanceTo_DiagonalLine_ReturnsCorrectPerpendicular()
    {
        // 45° line from (0,0) to (4,4); point (0,4) has perpendicular distance 4/sqrt(2)
        var line = new Line(U2(0, 0), U2(4, 4));
        double expected = 4.0 / Math.Sqrt(2);
        Assert.That(line.DistanceTo(U2(0, 4)).Millimeters, Is.EqualTo(expected).Within(MathUtil.Epsilon));
    }

    [Test]
    public void Intersection_PerpendicularCross_ReturnsTAtCrossing()
    {
        // Horizontal (0,0)→(10,0) crossed by vertical (5,-5)→(5,5)
        var horizontal = new Line(U2(0, 0), U2(10, 0));
        var vertical   = new Line(U2(5, -5), U2(5, 5));

        var t = horizontal.Intersection(vertical);

        Assert.That(t, Is.Not.Null);
        Assert.That(t!.Value, Is.EqualTo(0.5).Within(MathUtil.Epsilon));
    }

    [Test]
    public void Intersection_PerpendicularCross_PointMatchesAtT()
    {
        var horizontal = new Line(U2(0, 0), U2(10, 0));
        var vertical   = new Line(U2(5, -5), U2(5, 5));

        var t     = horizontal.Intersection(vertical);
        var point = horizontal.At(t!.Value);

        Assert.Multiple(() =>
        {
            Assert.That(point.X.Millimeters, Is.EqualTo(5).Within(MathUtil.Epsilon));
            Assert.That(point.Y.Millimeters, Is.EqualTo(0).Within(MathUtil.Epsilon));
        });
    }

    [Test]
    public void Intersection_Parallel_ReturnsNull()
    {
        var a = new Line(U2(0, 0), U2(10, 0));
        var b = new Line(U2(0, 5), U2(10, 5));

        Assert.That(a.Intersection(b), Is.Null);
    }

    [Test]
    public void Intersection_Collinear_ReturnsNull()
    {
        var a = new Line(U2(0, 0), U2(10, 0));
        var b = new Line(U2(2, 0), U2(8, 0));

        Assert.That(a.Intersection(b), Is.Null);
    }

    [Test]
    public void Intersection_CrossingBeyondThisEnd_ReturnsNull()
    {
        // 'other' crosses the infinite extension of 'this' (t > 1), not 'this' itself
        var a = new Line(U2(0, 0), U2(4, 0));
        var b = new Line(U2(6, -1), U2(6, 1));

        Assert.That(a.Intersection(b), Is.Null);
    }

    [Test]
    public void Intersection_CrossingBeforeThisStart_ReturnsNull()
    {
        // Intersection occurs at t < 0 along 'this'
        var a = new Line(U2(2, 0), U2(10, 0));
        var b = new Line(U2(0, -1), U2(0, 1));

        Assert.That(a.Intersection(b), Is.Null);
    }

    // --- Intersection: missing u-check bug ---
    // Intersection() only verifies that t ∈ [0,1] along 'this'; it never
    // computes or checks u along 'other'. These four tests will FAIL until
    // the fix `if (u < 0 || u > 1) return null;` is added.

    [Test]
    public void Intersection_OtherSegmentAboveThisLine_ReturnsNull()
    {
        // 'other' is a vertical segment entirely above the X-axis;
        // it would cross the infinite extension of 'this', but not 'this' itself.
        var a = new Line(U2(0, 0), U2(10, 0));  // horizontal on y=0
        var b = new Line(U2(5, 5), U2(5, 10));  // vertical above y=0

        Assert.That(a.Intersection(b), Is.Null);
    }

    [Test]
    public void Intersection_OtherSegmentBelowThisLine_ReturnsNull()
    {
        // Symmetric case: 'other' is entirely below the X-axis.
        var a = new Line(U2(0, 0), U2(10, 0));   // horizontal on y=0
        var b = new Line(U2(5, -10), U2(5, -5)); // vertical below y=0

        Assert.That(a.Intersection(b), Is.Null);
    }

    [Test]
    public void Intersection_OtherSegmentLeftOfThisLine_ReturnsNull()
    {
        // 'other' is a horizontal segment entirely to the left of the Y-axis;
        // the vertical 'this' would meet its infinite line, but not 'other' itself.
        var a = new Line(U2(0, 0), U2(0, 10));    // vertical on x=0
        var b = new Line(U2(-10, 5), U2(-1, 5));  // horizontal left of x=0

        Assert.That(a.Intersection(b), Is.Null);
    }

    [Test]
    public void Intersection_OtherSegmentRightOfThisLine_ReturnsNull()
    {
        // Symmetric: 'other' is entirely to the right of the Y-axis.
        var a = new Line(U2(0, 0), U2(0, 10));  // vertical on x=0
        var b = new Line(U2(1, 5), U2(10, 5));  // horizontal right of x=0

        Assert.That(a.Intersection(b), Is.Null);
    }

    [Test]
    public void Intersection_TAtExactlyZero_ReturnsZero()
    {
        // Intersection at the very start of 'this'
        var a = new Line(U2(0, 0), U2(10, 0));
        var b = new Line(U2(0, -5), U2(0, 5));

        var t = a.Intersection(b);
        Assert.That(t, Is.Not.Null);
        Assert.That(t!.Value, Is.EqualTo(0.0).Within(MathUtil.Epsilon));
    }

    [Test]
    public void Intersection_TAtExactlyOne_ReturnsOne()
    {
        // Intersection at the very end of 'this'
        var a = new Line(U2(0, 0), U2(10, 0));
        var b = new Line(U2(10, -5), U2(10, 5));

        var t = a.Intersection(b);
        Assert.That(t, Is.Not.Null);
        Assert.That(t!.Value, Is.EqualTo(1.0).Within(MathUtil.Epsilon));
    }

    [Test]
    public void Intersection_DiagonalLines_ReturnsCorrectT()
    {
        // y = x  from (0,0)→(10,10)
        // y = -x+10 from (0,10)→(10,0)
        // They meet at (5,5), which is t=0.5 for both.
        var a = new Line(U2(0, 0),  U2(10, 10));
        var b = new Line(U2(0, 10), U2(10, 0));

        var t = a.Intersection(b);
        Assert.That(t, Is.Not.Null);
        Assert.That(t!.Value, Is.EqualTo(0.5).Within(MathUtil.Epsilon));
    }

    // --- Subsegment ---

    [Test]
    public void Subsegment_ZeroToOne_ReturnsOriginalLine()
    {
        var line = new Line(U2(0, 0), U2(10, 20));
        var sub  = line.Subsegment(0.0, 1.0);
        Assert.Multiple(() =>
        {
            Assert.That(sub.Start.X.Millimeters, Is.EqualTo(0).Within(MathUtil.Epsilon));
            Assert.That(sub.Start.Y.Millimeters, Is.EqualTo(0).Within(MathUtil.Epsilon));
            Assert.That(sub.End.X.Millimeters,   Is.EqualTo(10).Within(MathUtil.Epsilon));
            Assert.That(sub.End.Y.Millimeters,   Is.EqualTo(20).Within(MathUtil.Epsilon));
        });
    }

    [Test]
    public void Subsegment_QuarterToThreeQuarters_ReturnsMiddleHalf()
    {
        var line = new Line(U2(0, 0), U2(10, 0));
        var sub  = line.Subsegment(0.25, 0.75);
        Assert.Multiple(() =>
        {
            Assert.That(sub.Start.X.Millimeters, Is.EqualTo(2.5).Within(MathUtil.Epsilon));
            Assert.That(sub.End.X.Millimeters,   Is.EqualTo(7.5).Within(MathUtil.Epsilon));
        });
    }

    [Test]
    public void Subsegment_StartAtZero_ClampsToOriginalStart()
    {
        var line = new Line(U2(2, 0), U2(12, 0));
        var sub  = line.Subsegment(0.0, 0.5);
        Assert.That(sub.Start.X.Millimeters, Is.EqualTo(2).Within(MathUtil.Epsilon));
    }

    [Test]
    public void Subsegment_EndAtOne_ClampsToOriginalEnd()
    {
        var line = new Line(U2(2, 0), U2(12, 0));
        var sub  = line.Subsegment(0.5, 1.0);
        Assert.That(sub.End.X.Millimeters, Is.EqualTo(12).Within(MathUtil.Epsilon));
    }

    [Test]
    public void Subsegment_ZeroToHalf_IsFirstHalf()
    {
        var line = new Line(U2(0, 0), U2(8, 4));
        var sub  = line.Subsegment(0.0, 0.5);
        Assert.Multiple(() =>
        {
            Assert.That(sub.Start.X.Millimeters, Is.EqualTo(0).Within(MathUtil.Epsilon));
            Assert.That(sub.End.X.Millimeters,   Is.EqualTo(4).Within(MathUtil.Epsilon));
            Assert.That(sub.End.Y.Millimeters,   Is.EqualTo(2).Within(MathUtil.Epsilon));
        });
    }

    [Test]
    public void Subsegment_PreservesLength()
    {
        var line = new Line(U2(0, 0), U2(10, 0));
        var sub  = line.Subsegment(0.2, 0.7);
        Assert.That(sub.Length.Millimeters, Is.EqualTo(5).Within(MathUtil.Epsilon));
    }

    // --- ToString ---

    [Test]
    public void ToString_ReturnsNonEmptyString()
    {
        var line = new Line(U2(1, 2), U2(3, 4));
        Assert.That(line.ToString(), Is.Not.Null.And.Not.Empty);
    }
}
