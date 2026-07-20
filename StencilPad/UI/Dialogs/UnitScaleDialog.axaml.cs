using Avalonia.Interactivity;
using StencilPad.Spatial;
using StencilPad.ViewModels.Dialogs;

namespace StencilPad.UI.Dialogs;

public partial class UnitScaleDialog : DialogWindowBase
{
    public UnitScaleDialogViewModel ViewModel { get; }

    public UnitScaleDialog(Fraction current)
    {
        InitializeComponent();
        ViewModel = new UnitScaleDialogViewModel(current);
        DataContext = ViewModel;

        Loaded += (s, e) =>
        {
            NumeratorField.Focus();
            NumeratorField.SelectAll();
        };
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        Result = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }
}
