using Avalonia;
using Avalonia.Media;

namespace StencilPad.UI.Widgets;

public partial class ColorFieldAlphaSlider : ColorFieldSliderBase
{
    protected override double DisplayScale => 255;

    public static readonly StyledProperty<Color> BaseColorProperty =
        AvaloniaProperty.Register<ColorFieldAlphaSlider, Color>(nameof(BaseColor), Colors.Black);

    static ColorFieldAlphaSlider()
    {
        BaseColorProperty.Changed.AddClassHandler<ColorFieldAlphaSlider>((slider, _) => slider.UpdateGradient());
    }

    public Color BaseColor
    {
        get => GetValue(BaseColorProperty);
        set => SetValue(BaseColorProperty, value);
    }

    public ColorFieldAlphaSlider()
    {
        InitializeComponent();

        // NOTE: The WPF checkerboard transparency background was simplified to
        // a flat grey surface for this prototype.
        UpdateGradient();
        InitializeSlider(DragCanvas, Marker);
    }

    protected override void UpdateGradient()
    {
        var c = BaseColor;

        GradientRect.Fill = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new(Color.FromArgb(0, c.R, c.G, c.B), 0),
                new(Color.FromArgb(255, c.R, c.G, c.B), 1)
            }
        };
    }
}
