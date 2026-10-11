using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace StencilPad.UI;

// NOTE: WPF's MarkupExtension derives directly from System.Windows.Data.Binding
// so the extension IS a binding. Avalonia's XAML compiler uses a "duck-typed"
// markup extension convention instead (any type with a ProvideValue method),
// so this composes an Avalonia Binding rather than deriving from it.
public class EnumBindingExtension
{
    private class EnumToBoolConverter : IValueConverter
    {
        public object? Convert(object? value,
                               Type targetType,
                               object? parameter,
                               CultureInfo culture)
        {
            return value?.Equals(parameter) ?? false;
        }

        public object? ConvertBack(object? value,
                                   Type targetType,
                                   object? parameter,
                                   CultureInfo culture)
        {
            return value?.Equals(true) == true ? parameter : BindingOperations.DoNothing;
        }
    }

    public string Path { get; }

    public object? TargetValue { get; set; }

    public EnumBindingExtension(string path)
    {
        Path = path;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Enum radio-button binding uses a runtime string path, which requires a reflection " +
                        "binding. The bound enum properties live on preserved view models.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Enum radio-button binding uses a runtime string path, which requires a reflection " +
                        "binding. The bound enum properties live on preserved view models.")]
    public object ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding(Path)
        {
            Mode = BindingMode.TwoWay,
            Converter = new EnumToBoolConverter(),
            ConverterParameter = TargetValue
        };
    }
}
