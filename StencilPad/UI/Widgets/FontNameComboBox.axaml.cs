using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;

namespace StencilPad.UI.Widgets;

public partial class FontNameComboBox : UserControl
{
    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<FontNameComboBox, string?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    static FontNameComboBox()
    {
        ValueProperty.Changed.AddClassHandler<FontNameComboBox>((field, _) => field.SyncSelection());
    }

    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private readonly List<string> _sortedFonts =
        FontManager.Current.SystemFonts
            .Select(f => f.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public FontNameComboBox()
    {
        InitializeComponent();

        // NOTE: This prototype uses plain ComboBox selection instead of the
        // WPF editable-text font picker behavior.
        FontComboBox.ItemsSource = _sortedFonts;
        SyncSelection();
    }

    private void FontComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FontComboBox.SelectedItem is string fontName)
        {
            Value = fontName;
        }
    }

    private void SyncSelection()
    {
        if (!string.IsNullOrWhiteSpace(Value) &&
            !_sortedFonts.Contains(Value, StringComparer.OrdinalIgnoreCase))
        {
            _sortedFonts.Add(Value);
            _sortedFonts.Sort(StringComparer.OrdinalIgnoreCase);
            FontComboBox.ItemsSource = null;
            FontComboBox.ItemsSource = _sortedFonts;
        }

        FontComboBox.SelectedItem = _sortedFonts.FirstOrDefault(x => string.Equals(x, Value, StringComparison.OrdinalIgnoreCase));
    }
}
