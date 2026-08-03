using Avalonia;

namespace StencilPad.Common;

public static class DragUtil
{
    private const double DragThresholdSquared = 4.0; // 2 pixels squared
    
    public static bool DragThresholdExceeded(Point initialMousePosition, Point currentMousePosition)
    {
        var deltaX = currentMousePosition.X - initialMousePosition.X;
        var deltaY = currentMousePosition.Y - initialMousePosition.Y;
        var distanceSquared = (deltaX * deltaX) + (deltaY * deltaY);

        return distanceSquared >= DragThresholdSquared;
    }

    public static bool DragThresholdExceeded(Vector dragDelta)
    {
        var distanceSquared = (dragDelta.X * dragDelta.X) + (dragDelta.Y * dragDelta.Y);
        
        return distanceSquared >= DragThresholdSquared;
    }
}
