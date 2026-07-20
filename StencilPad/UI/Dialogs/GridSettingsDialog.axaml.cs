using Avalonia.Interactivity;
using StencilPad.Spatial;
using StencilPad.ViewModels.Dialogs;

namespace StencilPad.UI.Dialogs;

public partial class GridSettingsDialog : DialogWindowBase
{
    public GridSettingsDialogViewModel ViewModel { get; }

    public GridSettingsDialog(Unit currentSpacing,
                              int currentSubdivisions,
                              UnitSettings unitSettings)
    {
        InitializeComponent();
        ViewModel = new GridSettingsDialogViewModel(currentSpacing,
                                                    currentSubdivisions,
                                                    unitSettings);
        DataContext = ViewModel;

        Loaded += (s, e) =>
        {
            SpacingField.Focus();
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
