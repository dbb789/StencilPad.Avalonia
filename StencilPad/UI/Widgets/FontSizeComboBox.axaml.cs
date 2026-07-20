using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace StencilPad.UI.Widgets;

public partial class FontSizeComboBox : UserControl
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<FontSizeComboBox, double>(nameof(Value), 12.0,
            defaultBindingMode: BindingMode.TwoWay);

    static FontSizeComboBox()
    {
        ValueProperty.Changed.AddClassHandler<FontSizeComboBox>((field, _) => field.SyncSelection());
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private readonly List<double> _sizes = [4, 6, 8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 32, 36, 40, 48];

    public FontSizeComboBox()
    {
        InitializeComponent();

        // NOTE: This prototype uses plain ComboBox selection instead of the
        // WPF editable-text size picker behavior.
        SizeComboBox.ItemsSource = _sizes;
        SyncSelection();
    }

    private void SizeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SizeComboBox.SelectedItem is double size)
        {
            Value = size;
        }
        else if (SizeComboBox.SelectedItem is int intSize)
        {
            Value = intSize;
        }
    }

    private void SyncSelection()
    {
        if (!_sizes.Contains(Value))
        {
            _sizes.Add(Value);
            _sizes.Sort();
            SizeComboBox.ItemsSource = null;
            SizeComboBox.ItemsSource = _sizes;
        }

        SizeComboBox.SelectedItem = _sizes.FirstOrDefault(x => Math.Abs(x - Value) < 0.001);
    }
}
