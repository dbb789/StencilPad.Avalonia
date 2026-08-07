using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using SkiaSharp;

namespace StencilPad.UI.Widgets;

public partial class GeometryDropdown : UserControl
{
    public sealed record Entry(SKPath Path, SKPaint? Paint = null);

    public static readonly StyledProperty<IList<Entry>?> ItemsProperty =
        AvaloniaProperty.Register<GeometryDropdown, IList<Entry>?>(nameof(Items));

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<GeometryDropdown, int>(nameof(SelectedIndex), 0,
            defaultBindingMode: BindingMode.TwoWay);
    
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
    }
}
