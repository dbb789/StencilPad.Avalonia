using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using SkiaSharp;
using StencilPad.Models;

namespace StencilPad.UI.Widgets;

public partial class GeometryDropdown : UserControl
{
    public sealed record Entry(SKPath Path, LineStyle? LineStyle = null);

    public static readonly StyledProperty<IList<Entry>?> ItemsProperty =
        AvaloniaProperty.Register<GeometryDropdown, IList<Entry>?>(nameof(Items));

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<GeometryDropdown, int>(nameof(SelectedIndex), 0,
            defaultBindingMode: BindingMode.TwoWay);
    
    static GeometryDropdown()
    {
        ItemsProperty.Changed.AddClassHandler<GeometryDropdown>((field, _) => field.SyncItems());
        SelectedIndexProperty.Changed.AddClassHandler<GeometryDropdown>((field, _) => field.SyncSelectedIndex());
    }

    public IList<Entry>? Items
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
        SyncItems();
        SyncSelectedIndex();
    }

    private void Dropdown_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedIndex = Dropdown.SelectedIndex;
    }

    private void SyncItems()
    {
        Dropdown.ItemsSource = Items;
    }

    private void SyncSelectedIndex()
    {
        Dropdown.SelectedIndex = SelectedIndex;
    }
}
