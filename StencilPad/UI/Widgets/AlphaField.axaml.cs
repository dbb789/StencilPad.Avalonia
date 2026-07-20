using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace StencilPad.UI.Widgets;

public partial class AlphaField : UserControl
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<AlphaField, double>(nameof(Value), 1.0,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<AlphaField, string>(nameof(Label), "A");

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public event Action? DragBegin;
    public event Action? DragEnd;

    public AlphaField()
    {
        InitializeComponent();

        AlphaSlider.DragBegin += () => DragBegin?.Invoke();
        AlphaSlider.DragEnd += () => DragEnd?.Invoke();
    }
}
