using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
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
    public string Description { get; set; } = "";

    public override string ToString()
    {
        return Description;
    }
}

public partial class CornerSizeField : UserControl
{
    public static readonly StyledProperty<CornerSize> ValueProperty =
        AvaloniaProperty.Register<CornerSizeField, CornerSize>(nameof(Value), CornerSize.Zero,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<CornerSizeField_Mode> SizeModeProperty =
        AvaloniaProperty.Register<CornerSizeField, CornerSizeField_Mode>(nameof(SizeMode), CornerSizeField_Mode.Millimeters,
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
        ValueProperty.Changed.AddClassHandler<CornerSizeField>((field, _) => field.OnValueOrModeChanged());
        SizeModeProperty.Changed.AddClassHandler<CornerSizeField>((field, _) => field.OnValueOrModeChanged());
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

    public CornerSizeField()
    {
        InitializeComponent();
        SizeModeComboBox.ItemsSource = SizeModes;
        ValueField.PropertyChanged += ValueField_PropertyChanged;
        SyncControls();
    }

    private void ValueField_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_isUpdating)
        {
            return;
        }

        if (e.Property == BaseUnitField.ValueProperty && ValueField.Value is { } unit)
        {
            _isUpdating = true;

            try
            {
                Value = CornerSize.FromUnit(unit);
            }
            finally
            {
                _isUpdating = false;
            }
        }
    }

    private void PercentField_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_isUpdating)
        {
            return;
        }

        if (e.NewValue is { } percent)
        {
            _isUpdating = true;

            try
            {
                Value = CornerSize.FromProportion((double)percent / 100.0);
            }
            finally
            {
                _isUpdating = false;
            }
        }
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

    private void OnValueOrModeChanged()
    {
        if (_isUpdating)
        {
            return;
        }

        _isUpdating = true;

        try
        {
            SyncControls();
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void SyncControls()
    {
        SizeModeComboBox.SelectedItem = SizeModes.FirstOrDefault(x => x.Value == SizeMode);

        var isProportion = SizeMode == CornerSizeField_Mode.Proportion;

        ValueField.IsVisible = !isProportion;
        PercentField.IsVisible = isProportion;

        if (isProportion)
        {
            if (Value.IsProportion)
            {
                PercentField.Value = (decimal)(Value.Proportion * 100);
            }
        }
        else
        {
            ValueField.UnitType = SizeMode == CornerSizeField_Mode.Inches ? UnitType.Inches : UnitType.Millimeters;

            if (Value.IsUnit)
            {
                ValueField.Value = Value.Unit;
            }
        }
    }
}
