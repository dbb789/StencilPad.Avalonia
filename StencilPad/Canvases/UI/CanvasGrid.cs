using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using StencilPad.Common;
using StencilPad.Canvases.Common;
using StencilPad.Rendering;
using StencilPad.Spatial;

namespace StencilPad.Canvases.UI;

public class CanvasGrid : ContentControl, IUnitSnap
{
    public static readonly StyledProperty<bool> ShowGridProperty =
        AvaloniaProperty.Register<CanvasGrid, bool>(nameof(ShowGrid), defaultValue: true);

    static CanvasGrid()
    {
        AffectsRender<CanvasGrid>(ShowGridProperty);
    }

    public bool ShowGrid
    {
        get => GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    // The grid lines rarely change (only on pan/zoom or a settings change),
    // so the SKPath/SKPaint geometry is built once here into a TripleBuffer
    // and replayed by Skia every frame, rather than re-issuing a DrawLine
    // per grid line through Avalonia's DrawingContext on every render.
    private class RenderedGeometry : IDisposable
    {
        public SKPath MinorPath = new();
        public SKPath MajorPath = new();
        public SKPath AxisPath = new();
        public SKRect PageRect;

        public void Reset()
        {
            MinorPath.Reset();
            MajorPath.Reset();
            AxisPath.Reset();
        }

        public void Dispose()
        {
            MinorPath.Dispose();
            MajorPath.Dispose();
            AxisPath.Dispose();
        }
    }

    private class RenderedPaint : IDisposable
    {
        public SKPaint MinorPaint = new();
        public SKPaint MajorPaint = new();
        public SKPaint AxisPaint = new();

        public void Reset()
        {
            MinorPaint.Reset();
            MajorPaint.Reset();
            AxisPaint.Reset();
        }

        public void Dispose()
        {
            MinorPaint.Dispose();
            MajorPaint.Dispose();
            AxisPaint.Dispose();
        }
    }

    private class GridDrawOperation : ICustomDrawOperation
    {
        public Rect Bounds => new Rect(0, 0, 0, 0);

        private readonly TripleBuffer<RenderedGeometry> _renderedGeometry;
        private readonly TripleBuffer<RenderedPaint> _renderedPaint;

        public GridDrawOperation(TripleBuffer<RenderedGeometry> renderedGeometry,
                                 TripleBuffer<RenderedPaint> renderedPaint)
        {
            _renderedGeometry = renderedGeometry;
            _renderedPaint = renderedPaint;
        }

        public void Dispose()
        {
            // This component is reusable - Dispose() is a no-op.
        }

        public void Render(ImmediateDrawingContext context)
        {
            var feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();

            if (feature is null)
            {
                return;
            }

            using var lease = feature.Lease();

            var canvas = lease.SkCanvas;

            using var geometryHandle = _renderedGeometry.TryRead();
            using var paintHandle = _renderedPaint.TryRead();

            if (!geometryHandle.IsValid || !paintHandle.IsValid)
            {
                return;
            }

            var geometry = geometryHandle.Buffer;
            var paint = paintHandle.Buffer;

            canvas.Save();
            canvas.ClipRect(geometry.PageRect);

            if (!geometry.MinorPath.IsEmpty)
            {
                canvas.DrawPath(geometry.MinorPath, paint.MinorPaint);
            }

            if (!geometry.MajorPath.IsEmpty)
            {
                canvas.DrawPath(geometry.MajorPath, paint.MajorPaint);
            }

            if (!geometry.AxisPath.IsEmpty)
            {
                canvas.DrawPath(geometry.AxisPath, paint.AxisPaint);
            }

            canvas.Restore();
        }

        public bool HitTest(Point p)
        {
            return Bounds.Contains(p);
        }

        public bool Equals(ICustomDrawOperation? other)
        {
            return Object.ReferenceEquals(this, other);
        }
    }

    private readonly ISettings _settings;
    private readonly IViewport _viewport;

    private readonly Pen _pageOutlinePen;

    private TripleBuffer<RenderedGeometry> _renderedGeometry;
    private TripleBuffer<RenderedPaint> _renderedPaint;
    private readonly GridDrawOperation _gridDrawOperation;
    private bool _geometryDirty;
    private bool _paintDirty;

    public CanvasGrid(ISettings settings,
                      IViewport viewport)
    {
        _settings = settings;
        _viewport = viewport;

        _pageOutlinePen = new Pen(Brushes.LightGray, 1);

        _renderedGeometry = new();
        _renderedPaint = new();
        _gridDrawOperation = new GridDrawOperation(_renderedGeometry, _renderedPaint);
        _geometryDirty = true;
        _paintDirty = true;

        _viewport.ViewportChanged += OnViewportChanged;

        Loaded += (s, e) =>
        {
            _settings.Changed += SettingsChanged;
        };

        Unloaded += (s, e) =>
        {
            _settings.Changed -= SettingsChanged;
        };
    }

    private void OnViewportChanged()
    {
        _geometryDirty = true;
        InvalidateVisual();
    }

    private void SettingsChanged()
    {
        // Grid spacing/subdivisions are also settings-driven, so a settings
        // change may affect geometry as well as paint - mark both dirty
        // rather than trying to track which specific property changed.
        _geometryDirty = true;
        _paintDirty = true;
        InvalidateVisual();
    }

    public override void Render(DrawingContext dc)
    {
        var xExtentsPixels = _viewport.ToPixels(_viewport.SheetSize.X / 2);
        var yExtentsPixels = _viewport.ToPixels(_viewport.SheetSize.Y / 2);
        
        var origin = _viewport.ToPoint(Unit2D.Zero);

        var pageRect = new Rect(origin.X - xExtentsPixels,
                                origin.Y - yExtentsPixels,
                                xExtentsPixels * 2,
                                yExtentsPixels * 2);

        // Draw the physical paper background
        dc.DrawRectangle(Brushes.White, _pageOutlinePen, pageRect);
        
        if (!ShowGrid)
        {
            return;
        }

        if (_paintDirty)
        {
            _paintDirty = false;
            RebuildPaint();
        }

        if (_geometryDirty)
        {
            _geometryDirty = false;
            RebuildGeometry(origin, xExtentsPixels, yExtentsPixels);
        }

        // Everything else (grid/axes) is clipped to the paper boundary
        dc.Custom(_gridDrawOperation);
    }

    private void RebuildGeometry(Point origin, double xExtentsPixels, double yExtentsPixels)
    {
        using var handle = _renderedGeometry.TryWrite();

        if (!handle.IsValid)
        {
            return;
        }

        var geometry = handle.Buffer;

        geometry.Reset();

        var spacing = _settings.GridSpacing;
        var subdivisions = _settings.GridSubdivisions;

        var majorSpacingPixels = _viewport.ToPixels(spacing);
        var minorSpacingPixels = _viewport.ToPixels(spacing / subdivisions);
        var minSpacingPixels = _settings.GridMinSpacingPx;

        var top = (float)(origin.Y - yExtentsPixels);
        var bottom = (float)(origin.Y + yExtentsPixels);
        var left = (float)(origin.X - xExtentsPixels);
        var right = (float)(origin.X + xExtentsPixels);

        geometry.PageRect = new SKRect(left, top, right, bottom);

        if (minorSpacingPixels > minSpacingPixels)
        {
            for (double x = 0; x <= xExtentsPixels; x += minorSpacingPixels)
            {
                AddVerticalLine(geometry.MinorPath, origin.X + x, top, bottom);
                AddVerticalLine(geometry.MinorPath, origin.X - x, top, bottom);
            }

            for (double y = 0; y <= yExtentsPixels; y += minorSpacingPixels)
            {
                AddHorizontalLine(geometry.MinorPath, origin.Y + y, left, right);
                AddHorizontalLine(geometry.MinorPath, origin.Y - y, left, right);
            }
        }

        for (double x = 0; x <= xExtentsPixels; x += majorSpacingPixels)
        {
            AddVerticalLine(geometry.MajorPath, origin.X + x, top, bottom);
            AddVerticalLine(geometry.MajorPath, origin.X - x, top, bottom);
        }

        for (double y = 0; y <= yExtentsPixels; y += majorSpacingPixels)
        {
            AddHorizontalLine(geometry.MajorPath, origin.Y + y, left, right);
            AddHorizontalLine(geometry.MajorPath, origin.Y - y, left, right);
        }

        AddVerticalLine(geometry.AxisPath, origin.X, top, bottom);
        AddHorizontalLine(geometry.AxisPath, origin.Y, left, right);
    }

    private static void AddVerticalLine(SKPath path, double x, float top, float bottom)
    {
        path.MoveTo((float)x, top);
        path.LineTo((float)x, bottom);
    }

    private static void AddHorizontalLine(SKPath path, double y, float left, float right)
    {
        path.MoveTo(left, (float)y);
        path.LineTo(right, (float)y);
    }

    private void RebuildPaint()
    {
        using var handle = _renderedPaint.TryWrite();

        if (!handle.IsValid)
        {
            return;
        }

        var paint = handle.Buffer;

        paint.Reset();

        var gridLineColor = _settings.GridLineColor;

        paint.MinorPaint.Color = ColorUtil.ToSKColor(ColorUtil.WithAlpha(gridLineColor, 64));
        paint.MinorPaint.StrokeWidth = 0.5f;
        paint.MinorPaint.Style = SKPaintStyle.Stroke;
        paint.MinorPaint.IsAntialias = true;

        paint.MajorPaint.Color = ColorUtil.ToSKColor(ColorUtil.WithAlpha(gridLineColor, 128));
        paint.MajorPaint.StrokeWidth = 0.5f;
        paint.MajorPaint.Style = SKPaintStyle.Stroke;
        paint.MajorPaint.IsAntialias = true;

        paint.AxisPaint.Color = ColorUtil.ToSKColor(ColorUtil.WithAlpha(gridLineColor, 192));
        paint.AxisPaint.StrokeWidth = 1f;
        paint.AxisPaint.Style = SKPaintStyle.Stroke;
        paint.AxisPaint.IsAntialias = true;
    }

    public Unit2D? UnitSnap(Unit2D point, IUnitSnapContext context)
    {
        var spacing = _settings.GridSpacing;
        var subdivisions = _settings.GridSubdivisions;
        var minSpacingPixels = _settings.GridMinSpacingPx;

        var majorSpacing = spacing;
        var minorSpacing = spacing / subdivisions;
        
        var majorSnap = Unit2D.Snap(point, majorSpacing);
        var minorSnap = Unit2D.Snap(point, minorSpacing);
        var snap = point;

        bool hasMinorSpacing = _viewport.ToPixels(minorSpacing) > minSpacingPixels;
        
        if (!hasMinorSpacing || (point - majorSnap).SqrMagnitude < (point - minorSnap).SqrMagnitude)
        {
            snap = majorSnap;
        }
        else
        {
            snap = minorSnap;
        }
        
        return snap;
    }
}
