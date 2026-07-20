using Avalonia;
using Avalonia.Media;
using StencilPad.Common;

namespace StencilPad.UI.Widgets;

public partial class ColorFieldBrightnessSlider : ColorFieldSliderBase
{
    protected override double DisplayScale => 100;

    public static readonly StyledProperty<double> HueProperty =
        AvaloniaProperty.Register<ColorFieldBrightnessSlider, double>(nameof(Hue), 0.0);

    public static readonly StyledProperty<double> SaturationProperty =
        AvaloniaProperty.Register<ColorFieldBrightnessSlider, double>(nameof(Saturation), 1.0);

    static ColorFieldBrightnessSlider()
    {
        HueProperty.Changed.AddClassHandler<ColorFieldBrightnessSlider>((slider, _) => slider.UpdateGradient());
        SaturationProperty.Changed.AddClassHandler<ColorFieldBrightnessSlider>((slider, _) => slider.UpdateGradient());
    }

    public double Hue
    {
        get => GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    public double Saturation
    {
        get => GetValue(SaturationProperty);
        set => SetValue(SaturationProperty, value);
    }

    public ColorFieldBrightnessSlider()
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
                new(ColorUtil.HsvToRgb(Hue, Saturation, 1, 1), 1)
            }
        };
    }
}
