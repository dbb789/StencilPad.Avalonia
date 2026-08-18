using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using StencilPad.Spatial;

namespace StencilPad.UI.Widgets;

public partial class BaseUnitField : UserControl
{
    public static readonly StyledProperty<Unit?> ValueProperty =
        AvaloniaProperty.Register<BaseUnitField, Unit?>(nameof(Value), Unit.Zero,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<UnitType> UnitTypeProperty =
        AvaloniaProperty.Register<BaseUnitField, UnitType>(nameof(UnitType), UnitType.Millimeters,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<UnitSettings> UnitSettingsProperty =
        AvaloniaProperty.Register<BaseUnitField, UnitSettings>(nameof(UnitSettings), UnitSettings.Default);

    public static readonly StyledProperty<Unit> MinimumProperty =
        AvaloniaProperty.Register<BaseUnitField, Unit>(nameof(Minimum), Unit.FromMillimeters(-1000000));

    public static readonly StyledProperty<Unit> MaximumProperty =
        AvaloniaProperty.Register<BaseUnitField, Unit>(nameof(Maximum), Unit.FromMillimeters(1000000));

    public static readonly StyledProperty<bool> ScaledProperty =
        AvaloniaProperty.Register<BaseUnitField, bool>(nameof(Scaled), false);

    static BaseUnitField()
    {
        ValueProperty.Changed.AddClassHandler<BaseUnitField>((field, _) => field.OnValueRelatedChanged());
        UnitTypeProperty.Changed.AddClassHandler<BaseUnitField>((field, _) => field.OnValueRelatedChanged());
        MinimumProperty.Changed.AddClassHandler<BaseUnitField>((field, _) => field.OnValueRelatedChanged());
        MaximumProperty.Changed.AddClassHandler<BaseUnitField>((field, _) => field.OnValueRelatedChanged());
        ScaledProperty.Changed.AddClassHandler<BaseUnitField>((field, _) => field.OnValueRelatedChanged());
        UnitSettingsProperty.Changed.AddClassHandler<BaseUnitField>((field, _) => field.OnUnitSettingsChanged());
    }

    public Unit? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public UnitType UnitType
    {
        get => GetValue(UnitTypeProperty);
        set => SetValue(UnitTypeProperty, value);
    }

    public UnitSettings UnitSettings
    {
        get => GetValue(UnitSettingsProperty);
        set => SetValue(UnitSettingsProperty, value);
    }

    public Unit Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public Unit Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public bool Scaled
    {
        get => GetValue(ScaledProperty);
        set => SetValue(ScaledProperty, value);
    }

    private string _textValue = string.Empty;

    public BaseUnitField()
    {
        InitializeComponent();
        UpdateTextValue();
        UpdateValidSpinDirection();
    }

    private void OnValueRelatedChanged()
    {
        if (Value is not null)
        {
            var clamped = ClampValue(Value.Value);

            if (clamped != Value.Value)
            {
                Value = clamped;
                return;
            }
        }

        UpdateTextValue();
        UpdateValidSpinDirection();
    }

    private void UpdateValidSpinDirection()
    {
        var direction = ValidSpinDirections.None;

        if (Value is { } value)
        {
            if (value < Maximum)
            {
                direction |= ValidSpinDirections.Increase;
            }

            if (value > Minimum)
            {
                direction |= ValidSpinDirections.Decrease;
            }
        }
        else
        {
            direction = ValidSpinDirections.Increase | ValidSpinDirections.Decrease;
        }

        Spinner.ValidSpinDirection = direction;
    }

    private void OnUnitSettingsChanged()
    {
        UnitType = UnitUtil.GetDefaultUnitType(UnitSettings);
        UpdateTextValue();
    }

    private void Spinner_Spin(object? sender, SpinEventArgs e)
    {
        ApplyValueField();

        var step = GetStep();

        if (e.Direction == SpinDirection.Decrease)
        {
            step = -step;
        }
        
        if (Value is not null)
        {
            Value = ClampValue(Value.Value + step);
        }

        e.Handled = true;
    }
    
    private void ValueField_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyValueField();
        }
    }

    private void ValueField_LostFocus(object? sender, RoutedEventArgs e)
    {
        ApplyValueField();
    }
    
    private Unit GetStep()
    {
        if (UnitType == UnitType.Millimeters)
        {
            return Unit.FromMillimeters(0.1);
        }

        return Unit.FromInches(0.0625);
    }

    private void ApplyValueField()
    {
        var currentText = ValueField.Text ?? "";

        if (_textValue == currentText)
        {
            return;
        }

        _textValue = currentText;

        if (Scaled)
        {
            if (Unit.TryParse(_textValue, UnitType, UnitSettings.Ratio, out var parsed))
            {
                Value = ClampValue(parsed);
            }
        }
        else if (Unit.TryParse(_textValue, UnitType, out var parsed))
        {
            Value = ClampValue(parsed);
        }
    }

    private void UpdateTextValue()
    {
        if (Value is null)
        {
            _textValue = "";
            ValueField.Text = "";
            return;
        }

        _textValue = Scaled
            ? UnitUtil.FormatScaled(Value.Value, UnitType, UnitSettings)
            : UnitUtil.Format(Value.Value, UnitType);

        ValueField.Text = _textValue;
    }

    private Unit ClampValue(Unit value)
    {
        return Unit.Clamp(value, Minimum, Maximum);
    }
}
