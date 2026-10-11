using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace StencilPad.UI;

// Converts an enum value to a bool for a toggle/checkbox by comparing it against
// the ConverterParameter (the enum member the item represents). Checking an item
// writes that member back; unchecking is ignored so exactly one option stays set.
//
// Used with compiled bindings (see MainWindow.axaml) so the enum menu items stay
// reflection-free and AOT/trim safe.
public class EnumToBoolConverter : IValueConverter
{
    public static readonly EnumToBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.Equals(parameter) ?? false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.Equals(true) == true ? parameter : BindingOperations.DoNothing;
    }
}
