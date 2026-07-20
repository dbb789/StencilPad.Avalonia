using Avalonia;
using Avalonia.Media;
using StencilPad.Common;

namespace StencilPad.UI.Widgets;

public partial class ColorFieldSaturationSlider : ColorFieldSliderBase
{
    protected override double DisplayScale => 100;

    public static readonly StyledProperty<double> HueProperty =
        AvaloniaProperty.Register<ColorFieldSaturationSlider, double>(nameof(Hue), 0.0);

    public static readonly StyledProperty<double> BrightnessProperty =
        AvaloniaProperty.Register<ColorFieldSaturationSlider, double>(nameof(Brightness), 1.0);

    static ColorFieldSaturationSlider()
    {
        HueProperty.Changed.AddClassHandler<ColorFieldSaturationSlider>((slider, _) => slider.UpdateGradient());
        BrightnessProperty.Changed.AddClassHandler<ColorFieldSaturationSlider>((slider, _) => slider.UpdateGradient());
    }

    public double Hue
    {
        get => GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    public double Brightness
    {
        get => GetValue(BrightnessProperty);
        set => SetValue(BrightnessProperty, value);
    }

    public ColorFieldSaturationSlider()
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
                new(ColorUtil.HsvToRgb(Hue, 0, Brightness, 1), 0),
                new(ColorUtil.HsvToRgb(Hue, 1, Brightness, 1), 1)
            }
        };
    }
}
