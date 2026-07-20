using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using StencilPad.Common;
using StencilPad.Canvases.Common;
using StencilPad.Spatial;
using StencilPad.Services;

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
    
    private readonly ISettings _settings;
    private readonly IViewport _viewport;

    private Pen _pageOutlinePen = null!;
    private Pen _minorPen = null!;
    private Pen _majorPen = null!;
    private Pen _axisPen = null!;
    
    public CanvasGrid(ISettings settings,
                      IViewport viewport)
    {
        _settings = settings;
        _viewport = viewport;

        _pageOutlinePen = new Pen(Brushes.LightGray, 1);
        
        BuildPens();
        
        Loaded += (s, e) =>
        {
            _settings.Changed += SettingsChanged;
        };

        Unloaded += (s, e) =>
        {
            _settings.Changed -= SettingsChanged;
        };
    }

    private void BuildPens()
    {
        var gridLineColor = _settings.GridLineColor;
        
        var minorBrush = new SolidColorBrush(ColorUtil.WithAlpha(gridLineColor, 64));

        var majorBrush = new SolidColorBrush(ColorUtil.WithAlpha(gridLineColor, 128));

        var axisBrush = new SolidColorBrush(ColorUtil.WithAlpha(gridLineColor, 192));
        
        _minorPen = new Pen(minorBrush, 0.5);
        
        _majorPen = new Pen(majorBrush, 0.5);
        
        _axisPen  = new Pen(axisBrush, 1);
    }
    
    private void SettingsChanged()
    {
        BuildPens();
        InvalidateVisual();
    }
    
    public override void Render(DrawingContext dc)
    {
        double w = Bounds.Width;
        double h = Bounds.Height;
        
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

        // Clip everything else (grid/axes) to the paper boundary
        using var clipState = dc.PushClip(pageRect);

        var spacing = _settings.GridSpacing;
        var subdivisions = _settings.GridSubdivisions;
        
        var majorSpacingPixels = _viewport.ToPixels(spacing);
        var minorSpacingPixels = _viewport.ToPixels(spacing / subdivisions);
        var minSpacingPixels = _settings.GridMinSpacingPx;
        
        if (minorSpacingPixels > minSpacingPixels)
        {
            for (double x = 0; x <= xExtentsPixels; x += minorSpacingPixels)
            {
                dc.DrawLine(_minorPen, new Point(origin.X + x, pageRect.Top), new Point(origin.X + x, pageRect.Bottom));
                dc.DrawLine(_minorPen, new Point(origin.X - x, pageRect.Top), new Point(origin.X - x, pageRect.Bottom));
            }

            for (double y = 0; y <= yExtentsPixels; y += minorSpacingPixels)
            {
                dc.DrawLine(_minorPen, new Point(pageRect.Left, origin.Y + y), new Point(pageRect.Right, origin.Y + y));
                dc.DrawLine(_minorPen, new Point(pageRect.Left, origin.Y - y), new Point(pageRect.Right, origin.Y - y));
            }
        }

        for (double x = 0; x <= xExtentsPixels; x += majorSpacingPixels)
        {
            dc.DrawLine(_majorPen, new Point(origin.X + x, pageRect.Top), new Point(origin.X + x, pageRect.Bottom));
            dc.DrawLine(_majorPen, new Point(origin.X - x, pageRect.Top), new Point(origin.X - x, pageRect.Bottom));
        }

        for (double y = 0; y <= yExtentsPixels; y += majorSpacingPixels)
        {
            dc.DrawLine(_majorPen, new Point(pageRect.Left, origin.Y + y), new Point(pageRect.Right, origin.Y + y));
            dc.DrawLine(_majorPen, new Point(pageRect.Left, origin.Y - y), new Point(pageRect.Right, origin.Y - y));
        }

        dc.DrawLine(_axisPen, new Point(origin.X, pageRect.Top), new Point(origin.X, pageRect.Bottom));
        dc.DrawLine(_axisPen, new Point(pageRect.Left, origin.Y), new Point(pageRect.Right, origin.Y));
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
