using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Skia;
using SkiaSharp;
using StencilPad.Canvases.Common;
using StencilPad.Canvases.Tools.Actions;
using StencilPad.Canvases.Tools.Common;
using StencilPad.Canvases.Tools.Widgets;
using StencilPad.Collections;
using StencilPad.Common;
using StencilPad.Models;
using StencilPad.Models.Resolvers;
using StencilPad.Rendering;
using StencilPad.Spatial;

namespace StencilPad.Canvases.Tools.Overlays;

public class SelectionToolOverlay : Control, IUnitSnapContext, IDisposable
{
    private struct OverlayEntry
    {
        public SKRect Bounds;
        public SKRect ResizeHandleBounds;
        public SKRect RotateHandleBounds;
        public bool IsGroup;
        public bool IsFilled;
    }

    private class RenderedEntries : IDisposable
    {
        public List<OverlayEntry> Entries = new();

        public void Reset()
        {
            Entries.Clear();
        }

        public void Dispose()
        {
            Reset();
        }
    }

    private class RenderedPaint : IDisposable
    {
        public SKPaint ElementPen = new();
        public SKPaint ElementFill = new();
        public SKPaint GroupPen = new();
        public SKPaint GroupFill = new();

        public void Reset()
        {
            ElementPen.Reset();
            ElementFill.Reset();
            GroupPen.Reset();
            GroupFill.Reset();
        }

        public void Dispose()
        {
            ElementPen.Dispose();
            ElementFill.Dispose();
            GroupPen.Dispose();
            GroupFill.Dispose();
        }
    }

    public IViewport Viewport => _viewport;

    private readonly ISettings _settings;
    private readonly IViewport _viewport;
    private readonly IRenderHooks _renderHooks;
    private readonly IUnitSnap _unitSnap;
    private readonly SheetResolver _sheetResolver;
    private readonly Sheet _sheet;

    private readonly DragState<ISheetElement> _dragState;
    private readonly LockAxisState _lockAxisState;
    private readonly DragState<ISheetElement> _resizeDragState;
    private readonly DragState<ISheetElement> _rotateDragState;

    private Unit2D _resizeInitialNW;
    private Unit2D _resizeInitialSE;
    private double _resizeAspectRatio;
    private Unit2D _rotateInitialHandlePos;
    private Unit2D _rotateDragCenter;
    private double _lastRotateAngle;

    private double _resizeHandleSize;
    private double _rotateHandleRadius;
    private IPointer? _capturedPointer;
    
    private TripleBuffer<RenderedEntries> _renderedEntries;
    private TripleBuffer<RenderedPaint> _renderedPaint;
    private bool _overlayDirty;

    public event Action? SelectionDragStarted;
    public event Action<Unit2D, Unit2D>? SelectionDragged;
    public event Action? SelectionDragEnded;
    
    public event Action? SelectionResizeStarted;
    public event Action<ISheetElement, Unit2D>? SelectionResized;
    public event Action? SelectionResizeEnded;
    
    public event Action? SelectionRotateStarted;
    public event Action<double, double>? SelectionRotated;
    public event Action? SelectionRotateEnded;
    
    public event Action<ISheetElementAction>? ActionInvoked;

    public SelectionToolOverlay(ISettings settings,
                                IViewport viewport,
                                IRenderHooks renderHooks,
                                IUnitSnap unitSnap,
                                Sheet sheet,
                                SheetResolver sheetResolver,
                                SheetElementActionSet actionSet)
    {
        _settings = settings;
        _viewport = viewport;
        _renderHooks = renderHooks;
        _unitSnap = unitSnap;
        _sheetResolver = sheetResolver;
        _sheet = sheet;
        _dragState = new();
        _lockAxisState = new();
        _resizeDragState = new();
        _rotateDragState = new();

        _renderedEntries = new();
        _renderedPaint = new();
        _overlayDirty = true;

        BuildPens();
        BuildInputBindings(actionSet);

        _renderHooks.PreRenderHook += PreRender;
        _renderHooks.OverlayRenderHook += RenderOverlayGeometry;
        
        ContextMenu = new ContextMenu();
        ContextMenu.Opening += (_, e) =>
        {
            if (!BuildContextMenu(actionSet))
            {
                e.Cancel = true;
            }
        };
        
        _sheetResolver.SelectionChanged += OnSelectionChanged;

        foreach (var resolver in _sheetResolver.Selection)
        {
            OnSelectionAdded(resolver);
        }

        _settings.Changed += SettingsChanged;
        _viewport.ViewportChanged += InvalidateOverlay;
    }

    public void Dispose()
    {
        _settings.Changed -= SettingsChanged;
        _viewport.ViewportChanged -= InvalidateOverlay;
        _sheetResolver.SelectionChanged -= OnSelectionChanged;

        _renderHooks.PreRenderHook -= PreRender;
        _renderHooks.OverlayRenderHook -= RenderOverlayGeometry;
        
        foreach (var resolver in _sheetResolver.Selection)
        {
            OnSelectionRemoved(resolver);
        }

        _renderedEntries.Dispose();
        _renderedPaint.Dispose();
    }

    private void BuildInputBindings(SheetElementActionSet actionSet)
    {
        var builder = new InputBindingsBuilder(_sheet, InvokeAction, KeyBindings);

        builder.Add(Key.P,
                    KeyModifiers.Control,
                    actionSet.ShapeProperties,
                    actionSet.MarkerPathProperties,
                    actionSet.TextProperties,
                    actionSet.RulerProperties,
                    actionSet.ImageProperties);

        builder.Add(Key.C,
                    KeyModifiers.Control | KeyModifiers.Shift,
                    actionSet.CombineShapes);

        builder.Add(Key.G,
                    KeyModifiers.Control,
                    actionSet.Group);

        builder.Add(Key.U,
                    KeyModifiers.Control,
                    actionSet.Ungroup);

        builder.Add(Key.H,
                    KeyModifiers.Control | KeyModifiers.Shift,
                    actionSet.FlipHorizontal);

        builder.Add(Key.V,
                    KeyModifiers.Control | KeyModifiers.Shift,
                    actionSet.FlipVertical);

        builder.Add(Key.F,
                    KeyModifiers.Control,
                    actionSet.BringToFront);

        builder.Add(Key.B,
                    KeyModifiers.Control,
                    actionSet.SendToBack);
    }

    private void InvokeAction(ISheetElementAction action)
    {
        ActionInvoked?.Invoke(action);
    }

    private bool BuildContextMenu(SheetElementActionSet actionSet)
    {
        if (ContextMenu is null)
        {
            return false;
        }

        if (_sheet.Selection.Count == 0)
        {
            return false;
        }

        ContextMenu.Items.Clear();
        
        var builder = new ContextMenuBuilder(_sheet, ActionInvoked);

        if (builder.AddContextMenuItemSet(
                ContextMenu.Items,
                (actionSet.ShapeProperties, "Shape Properties…", "Ctrl+P"),
                (actionSet.MarkerPathProperties, "Marker Path Properties…", "Ctrl+P"),
                (actionSet.TextProperties, "Text Properties…", "Ctrl+P"),
                (actionSet.RulerProperties, "Ruler Properties…", "Ctrl+P"),
                (actionSet.ImageProperties, "Image Properties…", "Ctrl+P"),
                (actionSet.CombineShapes, "Combine Shapes", "Ctrl+Shift+C")))
        {
            ContextMenu.Items.Add(new Separator());
        }

        builder.AddContextMenuItemSet(
            ContextMenu.Items,
            (actionSet.Group, "Group", "Ctrl+G"),
            (actionSet.Ungroup, "Ungroup", "Ctrl+U"));
        
        ContextMenu.Items.Add(new Separator());

        builder.AddContextMenuItemSet(
            ContextMenu.Items,
            (actionSet.FlipHorizontal, "Flip Horizontal", "Ctrl+Shift+H"),
            (actionSet.FlipVertical, "Flip Vertical", "Ctrl+Shift+V"));

        ContextMenu.Items.Add(new Separator());

        var justifyGroup = new MenuItem { Header = "Justify" };

        ContextMenu.Items.Add(justifyGroup);

        builder.AddContextMenuItemSet(
            justifyGroup.Items,
            (actionSet.JustifyLeft, "Left", ""),
            (actionSet.JustifyCenter, "Centre", ""),
            (actionSet.JustifyRight, "Right", ""));

        justifyGroup.Items.Add(new Separator());
        
        builder.AddContextMenuItemSet(
            justifyGroup.Items,
            (actionSet.JustifyTop, "Top", ""),
            (actionSet.JustifyMiddle, "Middle", ""),
            (actionSet.JustifyBottom, "Bottom", ""));
        
        ContextMenu.Items.Add(new Separator());

        builder.AddContextMenuItemSet(
            ContextMenu.Items,
            (actionSet.BringToFront, "Bring to Front", "Ctrl+F"),
            (actionSet.SendToBack, "Send to Back", "Ctrl+B"));

        return true;
    }
    
    private void BuildPens()
    {
        var selectionColor = _settings.SelectionColor;
        var groupSelectionColor = _settings.GroupSelectionColor;

        using var paintHandle = _renderedPaint.TryWrite();

        if (paintHandle.IsValid)
        {
            var paint = paintHandle.Buffer;

            paint.ElementPen.Style = SKPaintStyle.Stroke;
            paint.ElementPen.Color = ColorUtil.WithAlpha(selectionColor, 128).ToSKColor();
            paint.ElementPen.StrokeWidth = 2;

            paint.ElementFill.Style = SKPaintStyle.Fill;
            paint.ElementFill.Color = ColorUtil.WithAlpha(selectionColor, 32).ToSKColor();

            paint.GroupPen.Style = SKPaintStyle.Stroke;
            paint.GroupPen.Color = ColorUtil.WithAlpha(groupSelectionColor, 128).ToSKColor();
            paint.GroupPen.StrokeWidth = 2;

            paint.GroupFill.Style = SKPaintStyle.Fill;
            paint.GroupFill.Color = ColorUtil.WithAlpha(groupSelectionColor, 32).ToSKColor();
        }

        _resizeHandleSize = _settings.HandleSizePx;
        _rotateHandleRadius = _settings.HandleSizePx / 2;
    }
    
    private void SettingsChanged()
    {
        BuildPens();
        ForceRedraw();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
        {
            return;
        }

        var mousePosition = e.GetPosition(this);

        foreach (var resolver in _sheetResolver.Selection)
        {
            var element = resolver.Element;
            var unitBounds = element.GetBounds();
            var screenBounds = _viewport.ToRect(resolver.GetOutlineBounds());
            var resizeRect = ResizeHandleRect(screenBounds);

            if (resizeRect.Contains(mousePosition))
            {
                _resizeInitialNW = unitBounds.NW;
                _resizeInitialSE = unitBounds.SE;
                _resizeAspectRatio = unitBounds.Size.X.Millimeters / unitBounds.Size.Y.Millimeters;
                _resizeDragState.OnDragStart(mousePosition, element, _resizeInitialSE);

                _capturedPointer = e.Pointer;
                e.Pointer.Capture(this);
                e.Handled = true;
                return;
            }

            var rotateRect = RotateHandleRect(screenBounds);

            if (rotateRect.Contains(mousePosition))
            {
                _rotateDragCenter = unitBounds.Center;
                _rotateInitialHandlePos = _viewport.FromPoint(mousePosition);
                _lastRotateAngle = 0;
                _rotateDragState.OnDragStart(mousePosition, element, _rotateInitialHandlePos);

                _capturedPointer = e.Pointer;
                e.Pointer.Capture(this);
                e.Handled = true;
                return;
            }
        }

        var elementUnderMouse = PointOverSelection(_viewport.FromPoint(mousePosition));

        if (elementUnderMouse is not null)
        {
            var elementBounds = elementUnderMouse.GetBounds();

            _dragState.OnDragStart(mousePosition,
                                   elementUnderMouse,
                                   elementBounds.Center);
            _lockAxisState.OnDragStart();
            
            _capturedPointer = e.Pointer;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var mousePosition = e.GetPosition(this);

        if (_resizeDragState.DragStarted)
        {
            var result = _resizeDragState.OnDragMove(_viewport, mousePosition);

            if (result is not null)
            {
                if (result.Value.IsDragBeginning)
                {
                    SelectionResizeStarted?.Invoke();
                }
                
                var targetSE = _unitSnap.UnitSnap(result.Value.TargetElementPosition, this)
                               ?? result.Value.TargetElementPosition;

                if (ModifierUtil.IsLockAspect(e))
                {
                    targetSE = LockAspect(targetSE);
                }

                // Clamp target to not go past the initial NW corner - this
                // avoids both destroying all information about an object by
                // setting all it's points to converge on a single point, and
                // also avoids technically valid but weird and unexpected
                // behaviour where we resize in reverse across the NW corner.
                
                targetSE = new Unit2D(Unit.Max(targetSE.X, _resizeInitialNW.X + Unit.FromMillimeters(0.1)),
                                      Unit.Min(targetSE.Y, _resizeInitialNW.Y - Unit.FromMillimeters(0.1)));

                var size = Unit2D.Abs(targetSE - _resizeInitialNW);

                SelectionResized?.Invoke(_resizeDragState.DraggedElement, targetSE - _resizeInitialSE);
                e.Handled = true;
            }

            return;
        }

        if (_rotateDragState.DragStarted)
        {
            var result = _rotateDragState.OnDragMove(_viewport, mousePosition);

            if (result is not null)
            {
                if (result.Value.IsDragBeginning)
                {
                    SelectionRotateStarted?.Invoke();
                }

                var initialVec = _rotateInitialHandlePos - _rotateDragCenter;
                var currentVec = result.Value.TargetElementPosition - _rotateDragCenter;
                var totalAngle = Unit2D.SignedAngle(initialVec, currentVec);

                if (ModifierUtil.IsAngleSnap(e))
                {
                    var snapAngle = _settings.AngleSnapDegrees * MathUtil.Deg2Rad;
                    
                    totalAngle = Math.Round(totalAngle / snapAngle) * snapAngle;
                }
                
                var angleDelta = totalAngle - _lastRotateAngle;
                
                _lastRotateAngle = totalAngle;

                SelectionRotated?.Invoke(totalAngle, angleDelta);
                e.Handled = true;
            }

            return;
        }

        if (_dragState.DragStarted)
        {
            var elementBounds = _dragState.DraggedElement.GetBounds();
            var result = _dragState.OnDragMove(_viewport, mousePosition);

            if (result is not null)
            {
                if (result.Value.IsDragBeginning)
                {
                    SelectionDragStarted?.Invoke();
                }
                
                var targetPosition = result.Value.TargetElementPosition;
                var targetBounds = UnitBounds.FromCenterSize(targetPosition, elementBounds.Size);
                var snappedCenter = SnapBoundsCenter(targetBounds);

                snappedCenter = _lockAxisState.OnDragMove(ModifierUtil.IsLockToAxis(e),
                                                          _viewport.FromPixels(_resizeHandleSize),
                                                          _dragState.InitialElementPosition,
                                                          snappedCenter);

                var delta = snappedCenter - elementBounds.Center;
                var totalDelta = snappedCenter - _dragState.InitialElementPosition;
                
                SelectionDragged?.Invoke(totalDelta, delta);
                e.Handled = true;
            }
            
            return;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        // Capture drag state before we clear everything out.
        bool dragHandled = false;

        if (_dragState.IsDragging)
        {
            dragHandled = true;
            SelectionDragEnded?.Invoke();
        }

        if (_resizeDragState.IsDragging)
        {
            dragHandled = true;
            SelectionResizeEnded?.Invoke();
        }

        if (_rotateDragState.IsDragging)
        {
            dragHandled = true;
            SelectionRotateEnded?.Invoke();
        }

        // Holding down the left button over a draggable item without actually
        // moving the mouse can start the drag state, so make sure these are all
        // cleared out regardless.
        _dragState.OnDragEnd();
        _lockAxisState.OnDragEnd();
        _resizeDragState.OnDragEnd();
        _rotateDragState.OnDragEnd();

        _capturedPointer?.Capture(null);
        _capturedPointer = null;
        e.Handled = dragHandled;

        // Clear drag fill.
        ForceRedraw();

        // Control.OnPointerReleased is what raises ContextRequested on a
        // right-click release (via Avalonia's ContextMenuProperty machinery),
        // so we need to chain to it or right-click context menus never open.
        base.OnPointerReleased(e);
    }

    private Unit2D SnapBoundsCenter(UnitBounds bounds)
    {
        Span<Unit2D> points =
        [
            bounds.NW, bounds.NE, bounds.SW, bounds.SE, bounds.Center
        ];

        int closestIndex = -1;
        Unit2D smallestDelta = Unit2D.FromSquare(Unit.FromMillimeters(1000));

        for (int i = 0; i < points.Length; ++i)
        {
            var snapPosition = _unitSnap.UnitSnap(points[i], this);

            if (snapPosition.HasValue)
            {
                var delta = snapPosition.Value - points[i];

                if (delta.SqrMagnitude < smallestDelta.SqrMagnitude)
                {
                    smallestDelta = delta;
                    closestIndex = i;
                }
            }
        }

        if (closestIndex != -1)
        {
            return bounds.Center + smallestDelta;
        }

        return bounds.Center;
    }

    private ISheetElement? PointOverSelection(Unit2D point)
    {
        foreach (var resolver in _sheetResolver.Selection)
        {
            if (resolver.OutlineContainsPoint(point))
            {
                return resolver.Element;
            }
        }

        return null;
    }

    private void OnSelectionChanged(ObservableListChangedArgs<ISheetElementResolver> e)
    {
        // NOTE: Ordering shouldn't matter here, so it's ignored.
        switch (e.Action)
        {
        case ObservableListChangedAction.Add:
            OnSelectionAdded(e.Item);
            break;
            
        case ObservableListChangedAction.Remove:
            OnSelectionRemoved(e.Item);
            break;
        }
    }
    
    private void OnSelectionAdded(ISheetElementResolver resolver)
    {
        resolver.OutlineChanged += InvalidateOverlay;

        ForceRedraw();
    }

    private void OnSelectionRemoved(ISheetElementResolver resolver)
    {
        resolver.OutlineChanged -= InvalidateOverlay;

        ForceRedraw();
    }

    public bool CanUnitSnapTo(ISheetElement element)
    {
        return !_sheet.Selection.Contains(element);
    }

    public bool CanUnitSnapTo(Handle handle)
    {
        return true;
    }

    private Unit2D LockAspect(Unit2D targetSE)
    {
        var dx = targetSE.X - _resizeInitialNW.X;
        var dy = _resizeInitialNW.Y - targetSE.Y;

        var seAy = dx / _resizeAspectRatio;
        var seBx = dy * _resizeAspectRatio;

        if (Unit.Abs(seAy - dy) <= Unit.Abs(seBx - dx))
        {
            return new Unit2D(targetSE.X, _resizeInitialNW.Y - seAy);
        }
        else
        {
            return new Unit2D(_resizeInitialNW.X + seBx, targetSE.Y);
        }
    }
    
    private void InvalidateOverlay()
    {
        _overlayDirty = true;
    }

    private void ForceRedraw()
    {
        InvalidateOverlay();
        _renderHooks.Redraw();
    }
    
    public override void Render(DrawingContext dc)
    {
        base.Render(dc);

        dc.DrawRectangle(Brushes.Transparent, null, Bounds);
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
        
        foreach (var resolver in _sheetResolver.Selection)
        {
            var screenBounds = _viewport.ToRect(resolver.GetOutlineBounds());
            
            var element = resolver.Element;
            var isGroup = element is ElementGroup;

            var isFilled = _dragState.DraggedElement == element ||
                           _rotateDragState.DraggedElement == element ||
                           _resizeDragState.DraggedElement == element;

            overlay.Entries.Add(new OverlayEntry
            {
                Bounds = screenBounds.ToSKRect(),
                ResizeHandleBounds = ResizeHandleRect(screenBounds).ToSKRect(),
                RotateHandleBounds = RotateHandleRect(screenBounds).ToSKRect(),
                IsGroup = isGroup,
                IsFilled = isFilled
            });
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

        var paint = paintHandle.Buffer;

        foreach (var entry in entriesHandle.Buffer.Entries)
        {
            var pen = entry.IsGroup ? paint.GroupPen : paint.ElementPen;

            if (entry.IsFilled)
            {
                var fill = entry.IsGroup ? paint.GroupFill : paint.ElementFill;

                canvas.DrawRect(entry.Bounds, fill);
            }
            
            canvas.DrawRect(entry.Bounds, pen);
            canvas.DrawRect(entry.ResizeHandleBounds, pen);

            var rotateHandleRect = entry.RotateHandleBounds;

            canvas.DrawOval(rotateHandleRect.MidX,
                            rotateHandleRect.MidY,
                            rotateHandleRect.Width / 2,
                            rotateHandleRect.Height / 2,
                            pen);
        }
    }

    private Rect RotateHandleRect(Rect screenBounds)
    {
        return new Rect(screenBounds.TopRight + new Vector(0, -_resizeHandleSize),
                        new Size(_resizeHandleSize, _resizeHandleSize));
    }

    private Rect ResizeHandleRect(Rect screenBounds)
    {
        return new Rect(screenBounds.BottomRight,
                        new Size(_resizeHandleSize, _resizeHandleSize));
    }
}
