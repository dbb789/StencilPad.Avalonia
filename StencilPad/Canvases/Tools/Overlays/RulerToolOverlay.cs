using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using StencilPad.Common;
using StencilPad.Canvases.Common;
using StencilPad.Canvases.Tools.Common;
using StencilPad.Models;
using StencilPad.Models.Resolvers;
using StencilPad.Rendering;
using StencilPad.Services;
using StencilPad.Spatial;

namespace StencilPad.Canvases.Tools.Overlays;

public class RulerToolOverlay : Control, IDisposable
{
    private readonly IViewport _viewport;
    private readonly IUnitSnap _unitSnap;
    private readonly IUnitSnapContext _unitSnapContext;
    private readonly Ruler _ruler;
    private readonly RulerResolver _resolver;
    private readonly ModelRenderer _renderer;
    private readonly LockAxisState _lockAxisState;
    
    private Unit2D? _start;
    private Unit2D _currentSnappedMousePosition;

    public event Action<Unit2D, Unit2D>? OnRulerPlaced;

    public RulerToolOverlay(IViewport viewport,
                            IUnitSnap unitSnap,
                            ISettings settings,
                            IResourceService resourceService)
    {
        _viewport = viewport;
        _unitSnap = unitSnap;
        _unitSnapContext = new DefaultUnitSnapContext(viewport);
        _ruler = new Ruler { Color = Color.FromArgb(128, 0, 0, 0) };
        _resolver = new RulerResolver(_ruler, settings, resourceService);
        _renderer = new ModelRenderer(resourceService);

        _resolver.Attach(_renderer);
        _renderer.RendererDirty += RendererDirty;
        
        _lockAxisState = new();

        _viewport.ViewportChanged += InvalidateVisual;
    }

    public void Dispose()
    {
        _renderer.RendererDirty -= RendererDirty;
        _renderer.Dispose();
        _resolver.Detach();
        _resolver.Dispose();
        
        _viewport.ViewportChanged -= InvalidateVisual;
    }
    
    private void RendererDirty()
    {
        Dispatcher.Invoke(InvalidateVisual);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
        {
            return;
        }

        if (_start is null)
        {
            _start = _currentSnappedMousePosition;
        }
        else if ((_start.Value - _currentSnappedMousePosition).Magnitude > Unit.Epsilon)
        {
            OnRulerPlaced?.Invoke(_ruler.Min, _ruler.Max);
            _start = null;
        }

        UpdateRuler();
        
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var mousePosition = e.GetPosition(this);

        _currentSnappedMousePosition = CurrentSnappedMouseOverPosition(mousePosition, e);

        UpdateRuler();
    }

    public override void Render(DrawingContext dc)
    {
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, Bounds.Width, Bounds.Height));

        if (_start is null)
        {
            return;
        }

        using var state = dc.PushTransform(_viewport.MillimetersToPixelsTransform.Value);

        dc.Custom(_renderer.CreateDrawOperation());
    }

    private void UpdateRuler()
    {
        if (_start is null)
        {
            return;
        }
        
        var min = _start.Value;
        var max = _currentSnappedMousePosition;

        // Force the ruler to a sane consistent default orientation so that the
        // label isn't upside down.
        if (min.X.ApproximatelyEquals(max.X) && min.Y < max.Y)
        {
            (min, max) = (max, min);
        }
        else if (min.X > max.X)
        {
            (min, max) = (max, min);
        }

        _ruler.Min = min;
        _ruler.Max = max;
    }

    private Unit2D CurrentSnappedMouseOverPosition(Point mousePosition,
                                                   PointerEventArgs args)
    {
        var unitPosition = _viewport.FromPoint(mousePosition);
        var snapPosition = _unitSnap.UnitSnap(unitPosition, _unitSnapContext);
        
        if (snapPosition.HasValue)
        {
            unitPosition = snapPosition.Value;
        }
        
        if (_start is not null)
        {
            unitPosition = _lockAxisState.OnDragMove(ModifierUtil.IsLockToAxis(args),
                                                     _viewport.FromPixels(12),
                                                     _start.Value,
                                                     unitPosition);
        }

        return unitPosition;
    }
}
