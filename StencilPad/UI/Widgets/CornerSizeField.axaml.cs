using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using StencilPad.Spatial;

namespace StencilPad.UI.Widgets;

public enum CornerSizeField_Mode
{
    Millimeters,
    Inches,
    Proportion
}

public class CornerSizeField_Item
{
    public CornerSizeField_Mode Value { get; set; }
    public string Description { get; set; } = string.Empty;
}

public partial class CornerSizeField : UserControl
{
    public static readonly StyledProperty<CornerSize> ValueProperty =
        AvaloniaProperty.Register<CornerSizeField, CornerSize>(nameof(Value), CornerSize.Zero,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<CornerSizeField_Mode> SizeModeProperty =
        AvaloniaProperty.Register<CornerSizeField, CornerSizeField_Mode>(nameof(SizeMode), CornerSizeField_Mode.Millimeters,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> TextValueProperty =
        AvaloniaProperty.Register<CornerSizeField, string>(nameof(TextValue), "0",
            defaultBindingMode: BindingMode.TwoWay);

    private static readonly IReadOnlyList<CornerSizeField_Item> SizeModes =
    [
        new() { Value = CornerSizeField_Mode.Millimeters, Description = "mm" },
        new() { Value = CornerSizeField_Mode.Inches, Description = "in" },
        new() { Value = CornerSizeField_Mode.Proportion, Description = "%" }
    ];

    private bool _isUpdating;

    static CornerSizeField()
    {
        ValueProperty.Changed.AddClassHandler<CornerSizeField>((field, e) => field.OnValueChanged(e));
        SizeModeProperty.Changed.AddClassHandler<CornerSizeField>((field, e) => field.OnValueChanged(e));
        TextValueProperty.Changed.AddClassHandler<CornerSizeField>((field, e) => field.OnValueChanged(e));
    }

    public CornerSize Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public CornerSizeField_Mode SizeMode
    {
        get => GetValue(SizeModeProperty);
        set => SetValue(SizeModeProperty, value);
    }

    public string TextValue
    {
        get => GetValue(TextValueProperty);
        set => SetValue(TextValueProperty, value);
    }

    public CornerSizeField()
    {
        InitializeComponent();
        SizeModeComboBox.ItemsSource = SizeModes;
        SyncControls();
    }

    private void ValueField_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyTextValue();
        }
    }

    private void ValueField_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ApplyTextValue();
    }

    private void SizeModeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdating)
        {
            return;
        }

        if (SizeModeComboBox.SelectedItem is CornerSizeField_Item item)
        {
            SizeMode = item.Value;
        }
    }

    private void ApplyTextValue()
    {
        TextValue = ValueField.Text ?? string.Empty;
    }

    private void OnValueChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (_isUpdating)
        {
            return;
        }

        _isUpdating = true;

        try
        {
            if (e.Property == ValueProperty || e.Property == SizeModeProperty)
            {
                var size = Value;

                if (SizeMode == CornerSizeField_Mode.Proportion)
                {
                    if (size.IsProportion)
                    {
                        TextValue = (size.Proportion * 100).ToString("0.###", CultureInfo.InvariantCulture);
                    }
                }
                else
                {
                    var unitType = SizeMode == CornerSizeField_Mode.Inches ? UnitType.Inches : UnitType.Millimeters;

                    if (size.IsUnit)
                    {
                        TextValue = size.Unit.ToType(unitType).ToString("0.###", CultureInfo.InvariantCulture);
                    }
                }
            }

            if (e.Property == TextValueProperty || e.Property == SizeModeProperty)
            {
                if (SizeMode == CornerSizeField_Mode.Proportion)
                {
                    if (double.TryParse(TextValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var pct))
                    {
                        Value = CornerSize.FromProportion(pct / 100.0);
                    }
                }
                else
                {
                    var unitType = SizeMode == CornerSizeField_Mode.Inches ? UnitType.Inches : UnitType.Millimeters;

                    if (Unit.TryParse(TextValue, unitType, out var parsedUnit))
                    {
                        Value = CornerSize.FromUnit(parsedUnit);
                    }
                }
            }

            SyncControls();
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void SyncControls()
    {
        ValueField.Text = TextValue;
        SizeModeComboBox.SelectedItem = SizeModes.FirstOrDefault(x => x.Value == SizeMode);
    }
}
