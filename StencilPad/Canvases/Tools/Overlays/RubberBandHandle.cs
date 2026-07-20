using Avalonia;

namespace StencilPad.Canvases.Tools.Overlays;

public class RubberBandHandle
{
    // NOTE: Avalonia has no SystemParameters equivalent for drag thresholds;
    // WPF's defaults for both axes are 4 device-independent pixels.
    private const double MinimumDragDistance = 4.0;

    public bool IsDragging => _isDragging;
    public Rect DragBounds => _dragStart is null ?
        default : new Rect(_dragStart.Value, _dragCurrent);
    
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
            var delta = _dragCurrent - _dragStart.Value;

            if (Math.Abs(delta.X) > MinimumDragDistance ||
                Math.Abs(delta.Y) > MinimumDragDistance)
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
}
