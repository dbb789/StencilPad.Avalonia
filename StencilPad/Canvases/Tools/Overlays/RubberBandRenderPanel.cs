using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace StencilPad.Canvases.Tools.Overlays;

public class RubberBandRenderPanel : Control
{
    private static readonly Brush RubberBandFill;
    private static readonly Pen RubberBandBorder;

    static RubberBandRenderPanel()
    {
        RubberBandFill = new SolidColorBrush(Color.FromArgb(40, 0, 120, 215));
        RubberBandBorder = new Pen(new SolidColorBrush(Color.FromArgb(200, 0, 120, 215)), 1.0);
    }

    private Rect? _dragRegion;

    public void Updated(Rect? dragRegion)
    {
        _dragRegion = dragRegion;
        
        InvalidateVisual();
    }
    
    public override void Render(DrawingContext dc)
    {
        if (_dragRegion is null)
        {
            return;
        }

        dc.DrawRectangle(RubberBandFill, RubberBandBorder, _dragRegion.Value);
    }
}
