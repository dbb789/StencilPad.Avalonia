using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using StencilPad.Spatial;

namespace StencilPad.UI.Widgets;

public partial class Unit2DField : UserControl
{
    public static readonly StyledProperty<Unit?> ValueXProperty =
        AvaloniaProperty.Register<Unit2DField, Unit?>(nameof(ValueX), Unit.Zero,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<Unit?> ValueYProperty =
        AvaloniaProperty.Register<Unit2DField, Unit?>(nameof(ValueY), Unit.Zero,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<UnitType> UnitTypeProperty =
        AvaloniaProperty.Register<Unit2DField, UnitType>(nameof(UnitType), UnitType.Millimeters,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<UnitSettings> UnitSettingsProperty =
        AvaloniaProperty.Register<Unit2DField, UnitSettings>(nameof(UnitSettings), UnitSettings.Default);

    public static readonly StyledProperty<Unit> MinimumProperty =
        AvaloniaProperty.Register<Unit2DField, Unit>(nameof(Minimum), Unit.FromMillimeters(-1000000));

    public static readonly StyledProperty<Unit> MaximumProperty =
        AvaloniaProperty.Register<Unit2DField, Unit>(nameof(Maximum), Unit.FromMillimeters(1000000));

    public static readonly StyledProperty<bool> ScaledProperty =
        AvaloniaProperty.Register<Unit2DField, bool>(nameof(Scaled), false);

    private static readonly IReadOnlyList<UnitTypeItem> UnitTypes =
    [
        new() { Value = UnitType.Millimeters, Description = "mm" },
        new() { Value = UnitType.Inches, Description = "in" }
    ];

    static Unit2DField()
    {
        UnitTypeProperty.Changed.AddClassHandler<Unit2DField>((field, _) => field.SyncUnitTypeSelection());
    }

    public Unit? ValueX
    {
        get => GetValue(ValueXProperty);
        set => SetValue(ValueXProperty, value);
    }

    public Unit? ValueY
    {
        get => GetValue(ValueYProperty);
        set => SetValue(ValueYProperty, value);
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

    public Unit2DField()
    {
        InitializeComponent();
        UnitTypeComboBox.ItemsSource = UnitTypes;
        SyncUnitTypeSelection();
    }

    private void UnitTypeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (UnitTypeComboBox.SelectedItem is UnitTypeItem item)
        {
            UnitType = item.Value;
        }
    }

    private void SyncUnitTypeSelection()
    {
        UnitTypeComboBox.SelectedItem = UnitTypes.FirstOrDefault(x => x.Value == UnitType);
    }
}
