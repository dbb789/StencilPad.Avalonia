using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using StencilPad.Canvases.Common;
using StencilPad.Canvases.Tools.Actions;
using StencilPad.Canvases.Tools.Common;
using StencilPad.Canvases.Tools.Widgets;
using StencilPad.Common;
using StencilPad.Models;
using StencilPad.Spatial;

namespace StencilPad.Canvases.Tools.Overlays;

public class EditToolOverlay : ToolOverlay, IUnitSnapContext, IDisposable
{
    // Limit pointer move event handling to 60hz so we don't clog up the UI thread.
    private const long MouseMoveEventThrottleMs = 16;
    
    private record struct HandleEntry(ISheetElement Element, Handle Handle);
    
    public IViewport Viewport => _viewport;
    
    public event Action<ISheetElement, Handle>? HandleDragBegin;
    public event Action<ISheetElement, Handle, Unit2D>? HandleDragged;
    public event Action? HandleDragEnd;
    public event Action<ISheetElement, Handle>? HandleSelected;
    public event Action<ISheetElementAction>? ActionInvoked;

    private readonly Sheet _sheet;
    private readonly ISettings _settings;
    private readonly IViewport _viewport;
    private readonly IHandleMap _handleMap;
    private readonly IUnitSnap _unitSnap;
    private readonly IUnitSnapOverlay _unitSnapOverlay;
    
    private List<IHandleMapEntry> _queryResults;
    private DragState<IHandleMapEntry> _dragState;
    private LockAxisState _lockAxisState;
    private long _lastMouseMoveEvent;
    private IPointer? _capturedPointer;
    
    private double _handleSize;
    private Brush _moveBrush = null!;
    private Brush _adjustBrush = null!;
    private Pen _selectedPen = null!;
    private Pen _axisLockPen = null!;
    
    public EditToolOverlay(Sheet sheet,
                           ISettings settings,
                           IViewport viewport,
                           IHandleMap handleMap,
                           IUnitSnap unitSnap,
                           IUnitSnapOverlay unitSnapOverlay,
                           SheetElementEditActionSet actionSet)
        : base(viewport, sheet, true)
    {
        _sheet = sheet;
        _settings = settings;
        _viewport = viewport;
        _handleMap = handleMap;
        _unitSnap = unitSnap;
        _unitSnapOverlay = unitSnapOverlay;
        
        _queryResults = new(128);
        _dragState = new();
        _lockAxisState = new();
        
        _viewport.ViewportChanged += ForceRedraw;
        _handleMap.SheetSelectionChanged += ForceRedraw;
        _handleMap.HandleAdded += OnHandleAdded;
        _handleMap.HandleRemoved += OnHandleRemoved;
        _handleMap.HandleMoved += OnHandleMoved;
        _handleMap.HandleSelectionChanged += ForceRedraw;

        BuildPens();
        
        // NOTE: WPF's CommandBindings/CommandBinding/GlobalCommands (RoutedUICommand)
        // model has no Avalonia equivalent, and GlobalCommands was already removed
        // as a flagged non-mechanical item earlier in this port. Global Select
        // All/Clear Selection keyboard shortcuts are stubbed out (not wired to any
        // key) until a real command-routing redesign happens; SelectAll()/
        // ClearSelection() below remain available to call directly.

        RegisterOverlay(PolygonToolOverlayRenderer.Factory);
        RegisterOverlay(TextElementToolOverlayRenderer.Factory);
        RegisterOverlay(ImageElementToolOverlayRenderer.Factory);

        BuildInputBindings(actionSet);
        
        ContextMenu = new ContextMenu();
        ContextRequested += (_, e) =>
        {
            if (!BuildContextMenu(actionSet))
            {
                e.Handled = true;
            }
        };

        _settings.Changed += SettingsChanged;
    }

    public override void Dispose()
    {
        _settings.Changed -= SettingsChanged;
                
        _viewport.ViewportChanged -= ForceRedraw;

        _handleMap.SheetSelectionChanged -= ForceRedraw;
        _handleMap.HandleAdded -= OnHandleAdded;
        _handleMap.HandleRemoved -= OnHandleRemoved;
        _handleMap.HandleMoved -= OnHandleMoved;
        _handleMap.HandleSelectionChanged -= ForceRedraw;

        base.Dispose();
    }

    private void BuildPens()
    {
        var moveHandleColor = _settings.MoveHandleColor;
        var adjustHandleColor = _settings.AdjustHandleColor;
        var selectionColor = _settings.SelectionColor;
        var gridLineColor = _settings.GridLineColor;
        
        _moveBrush = new SolidColorBrush(ColorUtil.WithAlpha(moveHandleColor, 128));

        _adjustBrush = new SolidColorBrush(ColorUtil.WithAlpha(adjustHandleColor, 128));

        _selectedPen = new Pen(new SolidColorBrush(ColorUtil.WithAlpha(selectionColor, 255)), 2);

        _axisLockPen = new Pen(new SolidColorBrush(ColorUtil.WithAlpha(gridLineColor, 128)), 2);

        _handleSize = _settings.HandleSizePx;
    }
    
    private void SettingsChanged()
    {
        BuildPens();
        InvalidateVisual();
    }

    public void SelectAll()
    {
        if (_handleMap.SelectedHandles.Count == _handleMap.HandleCount)
        {
            _handleMap.ClearSelection();
            return;
        }
        
        _handleMap.SelectAll();
    }
    
    public void ClearSelection()
    {
        _handleMap.ClearSelection();
    }

    private void BuildInputBindings(SheetElementEditActionSet actionSet)
    {
        var builder = new InputBindingsBuilder(_sheet, ActionInvoked, KeyBindings);

        builder.Add(Key.P, KeyModifiers.Control, actionSet.CornerProperties);
        builder.Add(Key.I, KeyModifiers.Control, actionSet.InsertPoint);
        builder.Add(Key.Delete, KeyModifiers.None, actionSet.DeletePoints);
        builder.Add(Key.O, KeyModifiers.Control | KeyModifiers.Shift, actionSet.OpenPath);
        builder.Add(Key.C, KeyModifiers.Control | KeyModifiers.Shift, actionSet.ClosePath);
        builder.Add(Key.S, KeyModifiers.Control | KeyModifiers.Shift, actionSet.SetAsStraight);
        builder.Add(Key.U, KeyModifiers.Control | KeyModifiers.Shift, actionSet.SetAsCurve);
    }
    
    private bool BuildContextMenu(SheetElementEditActionSet actionSet)
    {
        if (_handleMap.SelectedHandles.Count == 0)
        {
            return false;
        }

        ContextMenu.Items.Clear();

        var builder = new ContextMenuBuilder(_sheet, ActionInvoked);
        
        if (builder.AddContextMenuItemSet(
                ContextMenu.Items,
                (actionSet.CornerProperties, "Corner Properties…", "Ctrl+P")))
        {
            ContextMenu.Items.Add(new Separator());
        }

        builder.AddContextMenuItemSet(
            ContextMenu.Items,
            (actionSet.InsertPoint, "Insert Point", "Ctrl+I"),
            (actionSet.DeletePoints, "Delete Point", "Delete"),
            (actionSet.OpenPath, "Open Path", "Ctrl+Shift+O"),
            (actionSet.ClosePath, "Close Path", "Ctrl+Shift+C"),
            (actionSet.SetAsStraight, "Set as Straight", "Ctrl+Shift+S"),
            (actionSet.SetAsCurve, "Set as Curve", "Ctrl+Shift+U"));

        return true;
    }
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
        {
            return;
        }

        var mousePosition = e.GetPosition(this.GetVisualParent());

        var clickPosition = _viewport.FromPoint(mousePosition);
        var clickSizeUnit = _viewport.FromPixels(_handleSize + 4);
        var clickSize = new Unit2D(clickSizeUnit, clickSizeUnit);

        var handle = _handleMap.GetClosestEditingHandle(UnitBounds.FromCenterSize(clickPosition, clickSize));

        if (handle is null)
        {
            return;
        }

        _dragState.OnDragStart(mousePosition,
                               handle,
                               handle.Position);
        _lockAxisState.OnDragStart();
        
        _capturedPointer = e.Pointer;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left)
        {
            return;
        }

        if (_dragState.IsDragging)
        {
            HandleDragEnd?.Invoke();
            
            _unitSnapOverlay.End();
        }
        else if (_dragState.DraggedElement is not null)
        {
            HandleSelected?.Invoke(_dragState.DraggedElement.Element,
                                   _dragState.DraggedElement.Handle);
        }

        _dragState.OnDragEnd();
        _lockAxisState.OnDragEnd();

        ForceRedraw();

        _capturedPointer?.Capture(null);
        _capturedPointer = null;
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var now = Environment.TickCount;
        
        if (_lastMouseMoveEvent > (now - MouseMoveEventThrottleMs))
        {
            return;
        }
        
        _lastMouseMoveEvent = now;
        
        var mousePosition = e.GetPosition(this.GetVisualParent());

        if (!_dragState.DragStarted)
        {
            return;
        }
        
        var dragResult = _dragState.OnDragMove(_viewport,
                                               mousePosition);

        if (dragResult is null)
        {
            return;
        }

        if (dragResult.Value.IsDragBeginning)
        {
            HandleDragBegin?.Invoke(_dragState.DraggedElement.Element,
                                    _dragState.DraggedElement.Handle);

            _unitSnapOverlay.Begin(this);
        }

        var snappedTarget = _unitSnap.UnitSnap(dragResult.Value.TargetElementPosition, this);
        var targetPosition = snappedTarget ?? dragResult.Value.TargetElementPosition;
        
        targetPosition = _lockAxisState.OnDragMove(ModifierUtil.IsLockToAxis(),
                                                   _viewport.FromPixels(_handleSize),
                                                   _dragState.InitialElementPosition,
                                                   targetPosition);

        var delta = targetPosition - _dragState.DraggedElement.Position;

        HandleDragged?.Invoke(_dragState.DraggedElement.Element,
                              _dragState.DraggedElement.Handle,
                              delta);

        e.Handled = true;
    }

    private void OnHandleAdded(ISheetElement element, Handle handle, Unit2D position)
    {
        ForceRedraw();
    }

    private void OnHandleRemoved(ISheetElement element, Handle handle)
    {
        ForceRedraw();
    }

    private void OnHandleMoved(ISheetElement element, Handle handle, Unit2D position)
    {
        ForceRedraw();
    }

    public bool CanUnitSnapTo(ISheetElement element)
    {
        return true;
    }
    
    public bool CanUnitSnapTo(Handle handle)
    {
        if (_handleMap.TryGetHandleEntry(handle, out var entry))
        {
            return !entry.Selected;
        }

        return true;
    }

    protected override void RenderOverlayContent(DrawingContext dc)
    {
        
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, Bounds.Width, Bounds.Height));

        RenderOverlay(dc);

        _queryResults.Clear();
        _handleMap.QueryHandles(UnitBounds.FromCenterSize(Unit2D.Zero, _viewport.Size),
                                _queryResults);
        
        foreach (var entry in _queryResults)
        {
            if (!entry.Editing)
            {
                continue;
            }
            
            var point = _viewport.ToPoint(entry.Position);
            var pen = entry.Selected ? _selectedPen : null;
           
            if (entry.Handle.Type == HandleType.Move)
            {
                dc.DrawRectangle(_moveBrush,
                                 pen,
                                 new Rect(point.X - (_handleSize / 2),
                                          point.Y - (_handleSize / 2),
                                          _handleSize,
                                          _handleSize));
            }
            else
            {
                dc.DrawEllipse(_adjustBrush,
                               pen,
                               point,
                               _handleSize / 2,
                               _handleSize / 2);
            }
        }

        if (_lockAxisState.LockedAxis is not null && _lockAxisState.LockPosition is not null)
        {
            if (_lockAxisState.LockedAxis == UnitAxis.X)
            {
                var lockPoint = _viewport.ToPoint(new Unit2D(Unit.Zero, _lockAxisState.LockPosition.Value));
                
                dc.DrawLine(_axisLockPen,
                            new Point(0, lockPoint.Y),
                            new Point(Bounds.Width, lockPoint.Y));
            }
            else
            {
                var lockPoint = _viewport.ToPoint(new Unit2D(_lockAxisState.LockPosition.Value, Unit.Zero));

                dc.DrawLine(_axisLockPen,
                            new Point(lockPoint.X, 0),
                            new Point(lockPoint.X, Bounds.Height));
            }
        }
    }
}
