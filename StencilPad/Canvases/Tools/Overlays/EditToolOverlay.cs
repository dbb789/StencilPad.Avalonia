using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Skia;
using Avalonia.VisualTree;
using SkiaSharp;
using StencilPad.Canvases.Common;
using StencilPad.Canvases.Tools.Actions;
using StencilPad.Canvases.Tools.Common;
using StencilPad.Canvases.Tools.Widgets;
using StencilPad.Rendering;
using StencilPad.Common;
using StencilPad.Models;
using StencilPad.Spatial;

namespace StencilPad.Canvases.Tools.Overlays;

public class EditToolOverlay : Control, IUnitSnapContext, IDisposable
{
    private class RenderedEntries : IDisposable
    {
        public List<SKPoint> MoveHandlePoints = new();
        public List<SKPoint> AdjustHandlePoints = new();
        public List<SKPoint> MoveHandleSelectedPoints = new();
        public List<SKPoint> AdjustHandleSelectedPoints = new();
        public SKPoint? LockLineStart;
        public SKPoint? LockLineEnd;
        
        public void Reset()
        {
            MoveHandlePoints.Clear();
            AdjustHandlePoints.Clear();
            MoveHandleSelectedPoints.Clear();
            AdjustHandleSelectedPoints.Clear();
            LockLineStart = null;
            LockLineEnd = null;
        }
        
        public void Dispose()
        {
            Reset();
        }
    }
    
    private class RenderedPaint : IDisposable
    {
        public SKPaint SelectedPen = new();
        public SKPaint AxisLockPen = new();
        public SKPaint MoveBrush = new();
        public SKPaint AdjustBrush = new();
        public double HandleSize;
        
        public void Dispose()
        {
            SelectedPen.Dispose();
            AxisLockPen.Dispose();
            MoveBrush.Dispose();
            AdjustBrush.Dispose();
        }
    }
    
    private record struct HandleEntry(ISheetElement Element, Handle Handle);
    
    public IViewport Viewport => _viewport;
    
    public event Action<ISheetElement, Handle>? HandleDragBegin;
    public event Action<ISheetElement, Handle, Unit2D>? HandleDragged;
    public event Action? HandleDragEnd;
    public event Action<ISheetElement, Handle, bool>? HandleSelected;
    public event Action<ISheetElementAction>? ActionInvoked;

    private readonly Sheet _sheet;
    private readonly ISettings _settings;
    private readonly IViewport _viewport;
    private readonly IRenderHooks _renderHooks;
    private readonly IHandleMap _handleMap;
    private readonly IUnitSnap _unitSnap;
    private readonly IUnitSnapOverlay _unitSnapOverlay;
    private readonly SheetElementEditActionSet _actionSet;
    
    private List<IHandleMapEntry> _queryResults;
    private DragState<IHandleMapEntry> _dragState;
    private LockAxisState _lockAxisState;
    private IPointer? _capturedPointer;
    private double _handleSize;
    
    private readonly ToolOverlay _toolOverlay;
    private readonly TripleBuffer<RenderedEntries> _renderedEntries;
    private readonly TripleBuffer<RenderedPaint> _renderedPaint;
    private bool _overlayDirty;
    
    public EditToolOverlay(Sheet sheet,
                           ISettings settings,
                           IViewport viewport,
                           IRenderHooks renderHooks,
                           IHandleMap handleMap,
                           IUnitSnap unitSnap,
                           IUnitSnapOverlay unitSnapOverlay,
                           SheetElementEditActionSet actionSet)
    {
        _sheet = sheet;
        _settings = settings;
        _viewport = viewport;
        _renderHooks = renderHooks;
        _handleMap = handleMap;
        _unitSnap = unitSnap;
        _unitSnapOverlay = unitSnapOverlay;
        _actionSet = actionSet;
        
        _queryResults = new(128);
        _dragState = new();
        _lockAxisState = new();
        
        _renderedEntries = new();
        _renderedPaint = new();
        _overlayDirty = true;

        _viewport.ViewportChanged += ForceRedraw;
        _handleMap.SheetSelectionChanged += ForceRedraw;
        _handleMap.HandleAdded += OnHandleAdded;
        _handleMap.HandleRemoved += OnHandleRemoved;
        _handleMap.HandleMoved += OnHandleMoved;
        _handleMap.HandleSelectionChanged += ForceRedraw;

        BuildPens();
        
        _toolOverlay = new ToolOverlay(sheet, renderHooks, true);
        _toolOverlay.RegisterOverlay(PolygonToolOverlayRenderer.Factory);
        _toolOverlay.RegisterOverlay(TextElementToolOverlayRenderer.Factory);
        _toolOverlay.RegisterOverlay(ImageElementToolOverlayRenderer.Factory);

        BuildInputBindings(_actionSet);
        
        _renderHooks.PreRenderHook += PreRender;
        _renderHooks.OverlayRenderHook += RenderOverlayGeometry;

        ContextMenu = new ContextMenu();
        ContextMenu.Opening += (_, e) =>
        {
            if (!BuildContextMenu(_actionSet))
            {
                e.Cancel = true;
            }
        };

        _settings.Changed += SettingsChanged;
    }

    public void Dispose()
    {
        _settings.Changed -= SettingsChanged;
        _viewport.ViewportChanged -= ForceRedraw;

        _toolOverlay.Dispose();
        
        _renderHooks.PreRenderHook -= PreRender;
        _renderHooks.OverlayRenderHook -= RenderOverlayGeometry;

        _handleMap.SheetSelectionChanged -= ForceRedraw;
        _handleMap.HandleAdded -= OnHandleAdded;
        _handleMap.HandleRemoved -= OnHandleRemoved;
        _handleMap.HandleMoved -= OnHandleMoved;
        _handleMap.HandleSelectionChanged -= ForceRedraw;

        _renderedEntries.Dispose();
        _renderedPaint.Dispose();
    }

    public void DeleteSelection()
    {
        var action = _actionSet.DeletePoints;
        
        if (!action.IsEnabled(_sheet, _sheet.Selection) ||
            !action.IsVisible(_sheet, _sheet.Selection))
        {
            return;
        }
        
        action?.Invoke(_sheet, _sheet.Selection);
    }

    private void BuildPens()
    {
        var moveHandleColor = _settings.MoveHandleColor;
        var adjustHandleColor = _settings.AdjustHandleColor;
        var selectionColor = _settings.SelectionColor;
        var gridLineColor = _settings.GridLineColor;

        _handleSize = _settings.HandleSizePx;
        
        using var paintHandle = _renderedPaint.TryWrite();

        if (paintHandle.IsValid)
        {
            var paint = paintHandle.Buffer;

            paint.MoveBrush.Style = SKPaintStyle.Fill;
            paint.MoveBrush.Color = ColorUtil.WithAlpha(moveHandleColor, 128).ToSKColor();
            paint.MoveBrush.IsAntialias = true;
            paint.MoveBrush.IsDither = true;

            paint.AdjustBrush.Style = SKPaintStyle.Fill;
            paint.AdjustBrush.Color = ColorUtil.WithAlpha(adjustHandleColor, 128).ToSKColor();
            paint.AdjustBrush.IsAntialias = true;
            paint.AdjustBrush.IsDither = true;

            paint.SelectedPen.Style = SKPaintStyle.Stroke;
            paint.SelectedPen.Color = ColorUtil.WithAlpha(selectionColor, 255).ToSKColor();
            paint.SelectedPen.StrokeWidth = 2.0f;
            paint.SelectedPen.IsAntialias = true;
            paint.SelectedPen.IsDither = true;
            
            paint.AxisLockPen.Style = SKPaintStyle.Stroke;
            paint.AxisLockPen.Color = ColorUtil.WithAlpha(gridLineColor, 128).ToSKColor();
            paint.AxisLockPen.StrokeWidth = 2.0f;
            paint.AxisLockPen.IsAntialias = true;
            paint.AxisLockPen.IsDither = true;

            paint.HandleSize = _handleSize;
        }
    }
    
    private void SettingsChanged()
    {
        BuildPens();
        InvalidateVisual();
    }

    private void BuildInputBindings(SheetElementEditActionSet actionSet)
    {
        var builder = new InputBindingsBuilder(_sheet, InvokeAction, KeyBindings);

        builder.Add(Key.P, KeyModifiers.Control, actionSet.CornerProperties);
        builder.Add(Key.I, KeyModifiers.Control, actionSet.InsertPoint);
        builder.Add(Key.O, KeyModifiers.Control | KeyModifiers.Shift, actionSet.OpenPath);
        builder.Add(Key.C, KeyModifiers.Control | KeyModifiers.Shift, actionSet.ClosePath);
        builder.Add(Key.S, KeyModifiers.Control | KeyModifiers.Shift, actionSet.SetAsStraight);
        builder.Add(Key.U, KeyModifiers.Control | KeyModifiers.Shift, actionSet.SetAsCurve);
    }

    private void InvokeAction(ISheetElementAction action)
    {
        ActionInvoked?.Invoke(action);
    }
    
    private bool BuildContextMenu(SheetElementEditActionSet actionSet)
    {
        if (ContextMenu is null)
        {
            return false;
        }

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
                                   _dragState.DraggedElement.Handle,
                                   ModifierUtil.IsModifyingSelection(e));
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
        
        targetPosition = _lockAxisState.OnDragMove(ModifierUtil.IsLockToAxis(e),
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

    private void InvalidateOverlay()
    {
        _overlayDirty = true;
    }

    protected void ForceRedraw()
    {
        InvalidateOverlay();
        _renderHooks.Redraw();
    }
    
    private void PreRender()
    {
        if (_overlayDirty)
        {
            _overlayDirty = false;
            RebuildOverlay();
        }
    }

    private void RebuildOverlay()
    {
        using var entriesHandle = _renderedEntries.TryWrite();

        if (!entriesHandle.IsValid)
        {
            return;
        }

        var overlay = entriesHandle.Buffer;

        overlay.Reset();
        
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

            if (entry.Handle.Type == HandleType.Move)
            {
                if (entry.Selected)
                {
                    overlay.MoveHandleSelectedPoints.Add(point.ToSKPoint());
                }
                else
                {
                    overlay.MoveHandlePoints.Add(point.ToSKPoint());
                }
            }
            else
            {
                if (entry.Selected)
                {
                    overlay.AdjustHandleSelectedPoints.Add(point.ToSKPoint());
                }
                else
                {
                    overlay.AdjustHandlePoints.Add(point.ToSKPoint());
                }
            }
        }

        if (_lockAxisState.LockedAxis is not null && _lockAxisState.LockPosition is not null)
        {
            if (_lockAxisState.LockedAxis == UnitAxis.X)
            {
                var lockPoint = _viewport.ToPoint(new Unit2D(Unit.Zero, _lockAxisState.LockPosition.Value));

                overlay.LockLineStart = new SKPoint(0, (float)lockPoint.Y);
                overlay.LockLineEnd = new SKPoint((float)Bounds.Width, (float)lockPoint.Y);
            }
            else
            {
                var lockPoint = _viewport.ToPoint(new Unit2D(_lockAxisState.LockPosition.Value, Unit.Zero));

                overlay.LockLineStart = new SKPoint((float)lockPoint.X, 0);
                overlay.LockLineEnd = new SKPoint((float)lockPoint.X, (float)Bounds.Height);
            }
        }

    }

    private void RenderOverlayGeometry(SKCanvas canvas, GRContext? context)
    {
        using var entriesHandle = _renderedEntries.TryRead();
        using var paintHandle = _renderedPaint.TryRead();

        if (!entriesHandle.IsValid || !paintHandle.IsValid)
        {
            return;
        }

        var overlay = entriesHandle.Buffer;
        var paint = paintHandle.Buffer;

        foreach (var point in overlay.MoveHandlePoints)
        {
            var rect = new SKRect(point.X - (float)(paint.HandleSize / 2),
                                  point.Y - (float)(paint.HandleSize / 2),
                                  point.X + (float)(paint.HandleSize / 2),
                                  point.Y + (float)(paint.HandleSize / 2));
            
            canvas.DrawRect(rect, paint.MoveBrush);
        }

        foreach (var point in overlay.AdjustHandlePoints)
        {
            canvas.DrawCircle(point, (float)(paint.HandleSize / 2), paint.AdjustBrush);
        }
        
        foreach (var point in overlay.MoveHandleSelectedPoints)
        {
            var rect = new SKRect(point.X - (float)(paint.HandleSize / 2),
                                  point.Y - (float)(paint.HandleSize / 2),
                                  point.X + (float)(paint.HandleSize / 2),
                                  point.Y + (float)(paint.HandleSize / 2));
            
            canvas.DrawRect(rect, paint.MoveBrush);
            canvas.DrawRect(rect, paint.SelectedPen);
        }
        
        foreach (var point in overlay.AdjustHandleSelectedPoints)
        {
            canvas.DrawCircle(point, (float)(paint.HandleSize / 2), paint.AdjustBrush);
            canvas.DrawCircle(point, (float)(paint.HandleSize / 2), paint.SelectedPen);
        }

        if (overlay.LockLineStart is not null && overlay.LockLineEnd is not null)
        {
            canvas.DrawLine(overlay.LockLineStart.Value,
                            overlay.LockLineEnd.Value,
                            paint.AxisLockPen);
        }
    }

    public override void Render(DrawingContext dc)
    {
        base.Render(dc);

        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, Bounds.Width, Bounds.Height));
    }
}
