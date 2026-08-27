using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace StencilPad.UI.Widgets;

public partial class GeometryDropdown : UserControl
{
    public static readonly StyledProperty<IList<GeometryDropdownEntry>?> ItemsProperty =
        AvaloniaProperty.Register<GeometryDropdown, IList<GeometryDropdownEntry>?>(nameof(Items));

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<GeometryDropdown, int>(nameof(SelectedIndex), 0,
            defaultBindingMode: BindingMode.TwoWay);
    
    public IList<GeometryDropdownEntry>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public GeometryDropdown()
    {
        InitializeComponent();
    }
}
