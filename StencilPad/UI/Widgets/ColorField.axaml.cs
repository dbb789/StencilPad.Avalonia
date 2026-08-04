using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using StencilPad.Common;

namespace StencilPad.UI.Widgets;

public partial class ColorField : UserControl
{
    public static readonly StyledProperty<Color> ValueProperty =
        AvaloniaProperty.Register<ColorField, Color>(nameof(Value), Colors.Black,
            defaultBindingMode: BindingMode.TwoWay);

    static ColorField()
    {
        ValueProperty.Changed.AddClassHandler<ColorField>((field, e) => field.OnValueChanged(e));
    }

    public Color Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private double _hue;
    private double _saturation;
    private double _brightness;
    private double _red;
    private double _green;
    private double _blue;
    private double _alpha;
    private Color _committedColor;
    private string _hexValue = string.Empty;

    public event Action? DragBegin;
    public event Action? DragEnd;

    public ColorField()
    {
        InitializeComponent();

        AttachDragHandlers(HueSlider);
        AttachDragHandlers(SaturationSlider);
        AttachDragHandlers(BrightnessSlider);
        AttachDragHandlers(RedSlider);
        AttachDragHandlers(GreenSlider);
        AttachDragHandlers(BlueSlider);
        AttachDragHandlers(AlphaSlider);

        SvPicker.DragBegin += () => DragBegin?.Invoke();
        SvPicker.DragEnd += () => DragEnd?.Invoke();

        HueSlider.ValueChanged += (_, _) =>
        {
            _hue = HueSlider.Value * 360;
            SnapAlpha();
            CommitHsv();
        };

        SaturationSlider.ValueChanged += (_, _) =>
        {
            _saturation = SaturationSlider.Value;
            SnapAlpha();
            CommitHsv();
        };

        BrightnessSlider.ValueChanged += (_, _) =>
        {
            _brightness = BrightnessSlider.Value;
            SnapAlpha();
            CommitHsv();
        };

        SvPicker.ValueChanged += (_, _) =>
        {
            _saturation = SvPicker.Saturation;
            _brightness = SvPicker.Brightness;
            SnapAlpha();
            CommitHsv();
        };

        RedSlider.ValueChanged += (_, _) =>
        {
            _red = RedSlider.Value;
            SnapAlpha();
            CommitRgb();
        };

        GreenSlider.ValueChanged += (_, _) =>
        {
            _green = GreenSlider.Value;
            SnapAlpha();
            CommitRgb();
        };

        BlueSlider.ValueChanged += (_, _) =>
        {
            _blue = BlueSlider.Value;
            SnapAlpha();
            CommitRgb();
        };

        AlphaSlider.ValueChanged += (_, _) =>
        {
            _alpha = AlphaSlider.Value;

            if (HsvRadio.IsChecked == true)
            {
                CommitHsv();
            }
            else
            {
                CommitRgb();
            }
        };

        Loaded += (_, _) => UpdateFromValue(Value);
    }

    private void HsvRadio_Checked(object? sender, RoutedEventArgs e)
    {
        if (HsvRadio.IsChecked != true)
        {
            return;
        }

        HsvPanel.IsVisible = true;
        RgbPanel.IsVisible = false;
    }

    private void RgbRadio_Checked(object? sender, RoutedEventArgs e)
    {
        if (RgbRadio.IsChecked != true)
        {
            return;
        }

        HsvPanel.IsVisible = false;
        RgbPanel.IsVisible = true;
    }

    private void OnValueChanged(AvaloniaPropertyChangedEventArgs e)
    {
        var newColor = e.NewValue is Color color ? color : Colors.Black;

        if (newColor == _committedColor)
        {
            return;
        }

        UpdateFromValue(newColor);
    }

    private void UpdateFromValue(Color color)
    {
        _committedColor = color;

        ColorUtil.RgbToHsv(color, out _hue, out _saturation, out _brightness);
        _red = color.R / 255.0;
        _green = color.G / 255.0;
        _blue = color.B / 255.0;
        _alpha = color.A / 255.0;

        UpdateSvPicker();
        UpdateHueSlider();
        UpdateSaturationSlider();
        UpdateBrightnessSlider();
        UpdateRgbSliders();
        UpdateAlphaSlider(color);
        UpdateHexTextBox(color);
        UpdatePreview(color);
    }

    private void CommitHsv()
    {
        var color = ColorUtil.HsvToRgb(_hue, _saturation, _brightness, _alpha);

        _red = color.R / 255.0;
        _green = color.G / 255.0;
        _blue = color.B / 255.0;
        _committedColor = color;
        Value = color;

        UpdateSvPicker();
        UpdateHueSlider();
        UpdateSaturationSlider();
        UpdateBrightnessSlider();
        UpdateRgbSliders();
        UpdateAlphaSlider(color);
        UpdateHexTextBox(color);
        UpdatePreview(color);
    }

    private void CommitRgb()
    {
        var color = Color.FromArgb(
            (byte)(_alpha * 255),
            (byte)(_red * 255),
            (byte)(_green * 255),
            (byte)(_blue * 255));

        ColorUtil.RgbToHsv(color, out _hue, out _saturation, out _brightness);
        _committedColor = color;
        Value = color;

        UpdateSvPicker();
        UpdateHueSlider();
        UpdateSaturationSlider();
        UpdateBrightnessSlider();
        UpdateRgbSliders();
        UpdateAlphaSlider(color);
        UpdateHexTextBox(color);
        UpdatePreview(color);
    }

    private void HexTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var currentText = HexTextBox.Text ?? string.Empty;

        if (currentText == _hexValue)
        {
            return;
        }

        _hexValue = currentText;

        if (!ColorUtil.TryParseHex(_hexValue, out var color))
        {
            return;
        }

        ColorUtil.RgbToHsv(color, out _hue, out _saturation, out _brightness);
        _red = color.R / 255.0;
        _green = color.G / 255.0;
        _blue = color.B / 255.0;
        _alpha = color.A / 255.0;
        _committedColor = color;
        Value = color;

        UpdateSvPicker();
        UpdateHueSlider();
        UpdateSaturationSlider();
        UpdateBrightnessSlider();
        UpdateRgbSliders();
        UpdateAlphaSlider(color);
        UpdatePreview(color);
    }

    private void UpdateSvPicker()
    {
        SvPicker.HueColor = ColorUtil.HsvToRgb(_hue, 1, 1, 1);
        SvPicker.Saturation = _saturation;
        SvPicker.Brightness = _brightness;
    }

    private void UpdateHueSlider()
    {
        HueSlider.Value = _hue / 360.0;
    }

    private void UpdateSaturationSlider()
    {
        SaturationSlider.Hue = _hue;
        SaturationSlider.Brightness = _brightness;
        SaturationSlider.Value = _saturation;
    }

    private void UpdateBrightnessSlider()
    {
        BrightnessSlider.Hue = _hue;
        BrightnessSlider.Saturation = _saturation;
        BrightnessSlider.Value = _brightness;
    }

    private void UpdateRgbSliders()
    {
        RedSlider.Value = _red;
        GreenSlider.Value = _green;
        BlueSlider.Value = _blue;
    }

    private void UpdateAlphaSlider(Color color)
    {
        AlphaSlider.BaseColor = Color.FromArgb(255, color.R, color.G, color.B);
        AlphaSlider.Value = color.A / 255.0;
    }

    private void UpdateHexTextBox(Color color)
    {
        _hexValue = ColorUtil.ToHexString(color);
        HexTextBox.Text = _hexValue;
    }

    private void UpdatePreview(Color color)
    {
        PreviewRect.Fill = new SolidColorBrush(color);
    }

    private void SnapAlpha()
    {
        if (_alpha == 0)
        {
            _alpha = 1;
        }
    }

    private void AttachDragHandlers(ColorFieldSliderBase slider)
    {
        slider.DragBegin += () => DragBegin?.Invoke();
        slider.DragEnd += () => DragEnd?.Invoke();
    }
}
