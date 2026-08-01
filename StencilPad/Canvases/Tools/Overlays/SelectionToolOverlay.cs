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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SkiaSharp;

namespace StencilPad.Canvases.Tools.Overlays;

public class SelectionToolOverlay : Control, IUnitSnapContext, IDisposable
{
    private class RenderedPicture : IDisposable
    {
        public SKPicture? Picture;

        public void Dispose()
        {
            Picture?.Dispose();
            Picture = null;
        }
    }

    public IViewport Viewport => _viewport;

    private readonly ISettings _settings;
    private readonly IViewport _viewport;
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
    private SKPaint _elementPen = new();
    private SKPaint _elementFill = new();
    private SKPaint _groupPen = new();
    private SKPaint _groupFill = new();
    private IPointer? _capturedPointer;

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

    private TripleBuffer<RenderedPicture> _renderedPicture = new();

    public SelectionToolOverlay(ISettings settings,
                                IViewport viewport,
                                IUnitSnap unitSnap,
                                Sheet sheet,
                                SheetResolver sheetResolver,
                                SheetElementActionSet actionSet)
    {
        _settings = settings;
        _viewport = viewport;
        _unitSnap = unitSnap;
        _sheetResolver = sheetResolver;
        _sheet = sheet;
        _dragState = new();
        _lockAxisState = new();
        _resizeDragState = new();
        _rotateDragState = new();

        BuildPens();

        BuildInputBindings(actionSet);
        
        // NOTE: Wiring ContextMenu population via Control.ContextRequested
        // doesn't work reliably in Avalonia: setting ContextMenu subscribes
        // an internal handler to ContextRequested that opens the popup using
        // whatever is currently in ContextMenu.Items, and since that internal
        // subscription happens before ours (it's added the moment ContextMenu
        // is assigned above), it runs first and pops up with stale/empty
        // items - our rebuild would always run too late. ContextMenu.Opening
        // fires synchronously just before the popup is actually shown, so
        // rebuilding the items there (and cancelling if there's nothing to
        // show) works correctly.
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
    }

    public void Dispose()
    {
        _settings.Changed -= SettingsChanged;
        
        _sheetResolver.SelectionChanged -= OnSelectionChanged;

        foreach (var resolver in _sheetResolver.Selection)
        {
            OnSelectionRemoved(resolver);
        }
    }

    private void BuildInputBindings(SheetElementActionSet actionSet)
    {
        var builder = new InputBindingsBuilder(_sheet, ActionInvoked, KeyBindings);

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

        _elementPen.Style = SKPaintStyle.Stroke;
        _elementPen.Color = ColorUtil.ToSKColor(ColorUtil.WithAlpha(selectionColor, 128));
        _elementPen.StrokeWidth = 2;

        _elementFill.Style = SKPaintStyle.Fill;
        _elementFill.Color = ColorUtil.ToSKColor(ColorUtil.WithAlpha(selectionColor, 32));
        
        _groupPen.Style = SKPaintStyle.Stroke;
        _groupPen.Color = ColorUtil.ToSKColor(ColorUtil.WithAlpha(groupSelectionColor, 128));
        _groupPen.StrokeWidth = 2;

        _groupFill.Style = SKPaintStyle.Fill;
        _groupFill.Color = ColorUtil.ToSKColor(ColorUtil.WithAlpha(groupSelectionColor, 32));

        _resizeHandleSize = _settings.HandleSizePx;
        _rotateHandleRadius = _settings.HandleSizePx / 2;
    }
    
    private void SettingsChanged()
    {
        BuildPens();
        InvalidateVisual();
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
        resolver.OutlineChanged += ForceRedraw;

        ForceRedraw();
    }

    private void OnSelectionRemoved(ISheetElementResolver resolver)
    {
        resolver.OutlineChanged -= ForceRedraw;

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

    private void ForceRedraw()
    {
        using var recorder = new SKPictureRecorder();
        using var canvas = recorder.BeginRecording(new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height));

        foreach (var resolver in _sheetResolver.Selection)
        {
            var screenBoundsRect = _viewport.ToRect(resolver.GetOutlineBounds());
            var screenBounds = new SKRect((float)screenBoundsRect.Left,
                                          (float)screenBoundsRect.Top,
                                          (float)screenBoundsRect.Right,
                                          (float)screenBoundsRect.Bottom);
            
            var element = resolver.Element;

            var pen = (element is ElementGroup) ? _groupPen : _elementPen;
            SKPaint? fill = null;

            if (_dragState.DraggedElement == element ||
                _rotateDragState.DraggedElement == element ||
                _resizeDragState.DraggedElement == element)
            {
                fill = (element is ElementGroup) ? _groupFill : _elementFill;
            }

            if (fill is not null)
            {
                canvas.DrawRect(screenBounds, fill);
            }
            
            canvas.DrawRect(screenBounds, pen);
            canvas.DrawRect(ResizeHandleRect(screenBounds), pen);

            var rotateHandleRect = RotateHandleRect(screenBounds);

            canvas.DrawOval((float)(rotateHandleRect.Left + rotateHandleRect.Width / 2),
                            (float)(rotateHandleRect.Top + rotateHandleRect.Height / 2),
                            (float)(rotateHandleRect.Width / 2),
                            (float)(rotateHandleRect.Height / 2),
                            pen);
        }

        using var pictureHandle = _renderedPicture.TryWrite();

        if (!pictureHandle.IsValid)
        {
            return;
        }
        
        pictureHandle.Buffer.Picture?.Dispose();
        pictureHandle.Buffer.Picture = recorder.EndRecording();

        InvalidateVisual();
    }
    
    public override void Render(DrawingContext dc)
    {
        base.Render(dc);

        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, Bounds.Width, Bounds.Height));
        
        using var pictureHandle = _renderedPicture.TryRead();

        if (!pictureHandle.IsValid)
        {
            return;
        }
        
        var picture = pictureHandle.Buffer.Picture;
        
        if (picture is not null)
        {
            dc.Custom(new SKPictureDrawOperation(picture, new Rect(0, 0, Bounds.Width, Bounds.Height)));
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

    private SKRect RotateHandleRect(SKRect screenBounds)
    {
        return new SKRect(screenBounds.Right,
                          screenBounds.Top - (float)_resizeHandleSize,
                          screenBounds.Right + (float)_resizeHandleSize,
                          screenBounds.Top);
    }

    private SKRect ResizeHandleRect(SKRect screenBounds)
    {
        return new SKRect(screenBounds.Right,
                          screenBounds.Bottom,
                          screenBounds.Right + (float)_resizeHandleSize,
                          screenBounds.Bottom + (float)_resizeHandleSize);
    }
}
