using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using StencilPad.Spatial;
using StencilPad.Canvases.Tools.Common;

namespace StencilPad.Canvases.Tools.Overlays;

public class RubberBandEventPanel : ContentControl, IRubberBand
{
    public RubberBandRenderPanel? RenderPanel;

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value)
            {
                _isActive = value;

                if (!_isActive)
                {
                    _rubberBandHandle.DragEnd();
                }
                
                UpdatePanel();
            }
        }
    }
    
    private readonly IViewport _viewport;
    private readonly RubberBandHandle _rubberBandHandle;
    private bool _isActive;
    private IPointer? _capturedPointer;
    
    public event Action<UnitBounds, bool>? BoundsSelected;
    public event Action<Unit2D, bool>? PointSelected;

    public RubberBandEventPanel(IViewport viewport)
    {
        _viewport = viewport;
        _rubberBandHandle = new RubberBandHandle();
        _isActive = false;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!_isActive || e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
        {
            return;
        }
        
        var mousePosition = e.GetPosition(this);

        _rubberBandHandle.DragBegin(mousePosition);

        _capturedPointer = e.Pointer;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_isActive)
        {
            return;
        }

        if (_rubberBandHandle.DragUpdate(e.GetPosition(this)))
        {
            if (_rubberBandHandle.IsDragging)
            {
                UpdatePanel();
            }

            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!_isActive || e.InitialPressMouseButton != MouseButton.Left)
        {
            return;
        }

        _capturedPointer?.Capture(null);
        _capturedPointer = null;

        if (_rubberBandHandle.IsDragging)
        {
            var rect = _rubberBandHandle.DragBounds;

            BoundsSelected?.Invoke(
                UnitBounds.FromMinMax(_viewport.FromPoint(rect.TopLeft),
                                      _viewport.FromPoint(rect.BottomRight)),
                ModifierUtil.IsModifyingSelection(e));
        }
        else
        {           
            PointSelected?.Invoke(_viewport.FromPoint(e.GetPosition(this)),
                                  ModifierUtil.IsModifyingSelection(e));
        }
        
        _rubberBandHandle.DragEnd();

        UpdatePanel();
        e.Handled = true;
    }

    private void UpdatePanel()
    {
        RenderPanel?.Updated(GetDragRegion());
    }

    private Rect? GetDragRegion()
    {
        if (!_rubberBandHandle.IsDragging)
        {
            return null;
        }
        
        return _rubberBandHandle.DragBounds;
    }

    public override void Render(DrawingContext dc)
    {
        dc.DrawRectangle(Brushes.Transparent,
                         null,
                         new Rect(0, 0, Bounds.Width, Bounds.Height));
    }
}
