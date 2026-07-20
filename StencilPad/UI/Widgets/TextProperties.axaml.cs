using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace StencilPad.UI.Widgets;

public partial class TextProperties : UserControl
{
    public static readonly StyledProperty<string?> TextFontNameProperty =
        AvaloniaProperty.Register<TextProperties, string?>(nameof(TextFontName), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> TextFontSizeProperty =
        AvaloniaProperty.Register<TextProperties, double>(nameof(TextFontSize), 12.0,
            defaultBindingMode: BindingMode.TwoWay);

    public string? TextFontName
    {
        get => GetValue(TextFontNameProperty);
        set => SetValue(TextFontNameProperty, value);
    }

    public double TextFontSize
    {
        get => GetValue(TextFontSizeProperty);
        set => SetValue(TextFontSizeProperty, value);
    }

    public TextProperties()
    {
        InitializeComponent();
    }
}
