using Avalonia;
using Avalonia.Controls;

namespace StencilPad.UI;

public partial class GridButtons : UserControl
{
    public static readonly StyledProperty<bool> ShowGridProperty =
        AvaloniaProperty.Register<GridButtons, bool>(nameof(ShowGrid), defaultValue: true,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool> SnapToGridProperty =
        AvaloniaProperty.Register<GridButtons, bool>(nameof(SnapToGrid), defaultValue: true,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool> SnapToPointProperty =
        AvaloniaProperty.Register<GridButtons, bool>(nameof(SnapToPoint), defaultValue: true,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public bool ShowGrid
    {
        get => GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    public bool SnapToGrid
    {
        get => GetValue(SnapToGridProperty);
        set => SetValue(SnapToGridProperty, value);
    }

    public bool SnapToPoint
    {
        get => GetValue(SnapToPointProperty);
        set => SetValue(SnapToPointProperty, value);
    }
    
    public GridButtons()
    {
        InitializeComponent();
    }
}
