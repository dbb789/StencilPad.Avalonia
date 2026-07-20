using Avalonia;
using Avalonia.Media;

namespace StencilPad.UI.Widgets;

public partial class ColorFieldHueSlider : ColorFieldSliderBase
{
    protected override double DisplayScale => 360;

    public ColorFieldHueSlider()
    {
        InitializeComponent();

        GradientRect.Fill = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new(Colors.Red, 0),
                new(Colors.Yellow, 1.0 / 6.0),
                new(Colors.Lime, 2.0 / 6.0),
                new(Colors.Cyan, 3.0 / 6.0),
                new(Colors.Blue, 4.0 / 6.0),
                new(Colors.Magenta, 5.0 / 6.0),
                new(Colors.Red, 1)
            }
        };

        InitializeSlider(DragCanvas, Marker);
    }

    protected override void UpdateGradient()
    {
    }
}
