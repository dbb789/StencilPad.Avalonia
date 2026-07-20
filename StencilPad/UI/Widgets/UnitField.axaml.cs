using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using StencilPad.Spatial;

namespace StencilPad.UI.Widgets;

public partial class UnitField : UserControl
{
    public static readonly StyledProperty<Unit?> ValueProperty =
        AvaloniaProperty.Register<UnitField, Unit?>(nameof(Value), Unit.Zero,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<UnitType> UnitTypeProperty =
        AvaloniaProperty.Register<UnitField, UnitType>(nameof(UnitType), UnitType.Millimeters,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<UnitSettings> UnitSettingsProperty =
        AvaloniaProperty.Register<UnitField, UnitSettings>(nameof(UnitSettings), UnitSettings.Default);

    public static readonly StyledProperty<Unit> MinimumProperty =
        AvaloniaProperty.Register<UnitField, Unit>(nameof(Minimum), Unit.FromMillimeters(0));

    public static readonly StyledProperty<Unit> MaximumProperty =
        AvaloniaProperty.Register<UnitField, Unit>(nameof(Maximum), Unit.FromMillimeters(1000000));

    public static readonly StyledProperty<bool> ScaledProperty =
        AvaloniaProperty.Register<UnitField, bool>(nameof(Scaled), false);

    private static readonly IReadOnlyList<UnitTypeItem> UnitTypes =
    [
        new() { Value = UnitType.Millimeters, Description = "mm" },
        new() { Value = UnitType.Inches, Description = "in" }
    ];

    static UnitField()
    {
        UnitTypeProperty.Changed.AddClassHandler<UnitField>((field, _) => field.SyncUnitTypeSelection());
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

    public UnitField()
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
