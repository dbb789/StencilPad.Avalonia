using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using StencilPad.Canvases.Common;
using StencilPad.Spatial;

namespace StencilPad.Canvases.Tools.Overlays;

public class UnitSnapOverlay : ContentControl, IUnitSnapOverlay
{
    private static readonly Pen IndicatorPen;

    private readonly IViewport _viewport;
    private readonly IUnitSnap _unitSnap;
    private readonly IUnitSnapContext _defaultContext;
    private IUnitSnapContext? _context;
    private Unit2D? _lastSnapPoint;
    private bool _isActive;
    
    static UnitSnapOverlay()
    {
        IndicatorPen = new Pen(new SolidColorBrush(Color.FromArgb(210, 0, 128, 255)), 1.5);
    }

    public UnitSnapOverlay(IViewport viewport, IUnitSnap unitSnap)
    {
        _viewport = viewport;
        _unitSnap = unitSnap;
        _defaultContext = new DefaultUnitSnapContext(_viewport);
        _context = null;

        AddHandler(PointerMovedEvent, HandlePointerMoved, handledEventsToo: true);
    }

    public void Begin(IUnitSnapContext? context = null)
    {
        _context = context;
        _lastSnapPoint = null;
        _isActive = true;
    }

    public void End()
    {
        _context = null;
        _lastSnapPoint = null;
        _isActive = false;

        // Redraw without indicator.
        InvalidateVisual();        
    }

    public Unit2D? UnitSnap(Unit2D mousePos)
    {
        var snapped = _unitSnap.UnitSnap(mousePos, _context ?? _defaultContext);

        if (_lastSnapPoint != snapped)
        {
            _lastSnapPoint = snapped;
            InvalidateVisual();
        }

        return snapped;
    }
    
    private void HandlePointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isActive)
        {
            return;
        }

        UnitSnap(_viewport.FromPoint(e.GetPosition(this)));
    }
    
    public override void Render(DrawingContext dc)
    {
        if (!_isActive)
        {
            return;
        }

        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, Bounds.Width, Bounds.Height));

        if (_lastSnapPoint is null)
        {
            return;
        }

        var lastSnapPixels = _viewport.ToPoint(_lastSnapPoint.Value);

        dc.DrawLine(IndicatorPen,
                    lastSnapPixels + new Vector(-5, -5), lastSnapPixels + new Vector(5, 5));
        dc.DrawLine(IndicatorPen,
                    lastSnapPixels + new Vector(-5, 5), lastSnapPixels + new Vector(5, -5));
    }
}
