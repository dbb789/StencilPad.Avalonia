using Avalonia;
using StencilPad.Common;

namespace StencilPad.Canvases.Tools.Overlays;

public class RubberBandHandle
{
    public bool IsDragging => _isDragging;
    public Rect DragBounds => _dragStart is null ?
        default : NormalizeRect(_dragStart.Value, _dragCurrent);
    
    private Point? _dragStart;
    private Point _dragCurrent;
    private bool _isDragging;

    public void DragBegin(Point mousePosition)
    {
        _dragStart = mousePosition;
        _dragCurrent = _dragStart.Value;
        _isDragging = false;
    }

    public bool DragUpdate(Point mousePosition)
    {
        if (_dragStart is null)
        {
            return false;
        }

        _dragCurrent = mousePosition;

        if (!_isDragging)
        {
            if (DragUtil.DragThresholdExceeded(_dragStart.Value, _dragCurrent))
            {
                _isDragging = true;
            }
        }

        return true;
    }

    public void DragEnd()
    {
        _dragStart = null;
        _isDragging = false;
    }

    private Rect NormalizeRect(Point p1, Point p2)
    {
        var x = Math.Min(p1.X, p2.X);
        var y = Math.Min(p1.Y, p2.Y);
        var width = Math.Abs(p1.X - p2.X);
        var height = Math.Abs(p1.Y - p2.Y);

        return new Rect(x, y, width, height);
    }
}
