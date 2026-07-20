using Avalonia;
using Avalonia.Media;

namespace StencilPad.UI.Widgets;

public partial class ColorFieldRGBSlider : ColorFieldSliderBase
{
    protected override double DisplayScale => 255;

    public static readonly StyledProperty<Color> ChannelColorProperty =
        AvaloniaProperty.Register<ColorFieldRGBSlider, Color>(nameof(ChannelColor), Colors.Red);

    static ColorFieldRGBSlider()
    {
        ChannelColorProperty.Changed.AddClassHandler<ColorFieldRGBSlider>((slider, _) => slider.UpdateGradient());
    }

    public Color ChannelColor
    {
        get => GetValue(ChannelColorProperty);
        set => SetValue(ChannelColorProperty, value);
    }

    public ColorFieldRGBSlider()
    {
        InitializeComponent();
        UpdateGradient();
        InitializeSlider(DragCanvas, Marker);
    }

    protected override void UpdateGradient()
    {
        GradientRect.Fill = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new(Colors.Black, 0),
                new(ChannelColor, 1)
            }
        };
    }
}
