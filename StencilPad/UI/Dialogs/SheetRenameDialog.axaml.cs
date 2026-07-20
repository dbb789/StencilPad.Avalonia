using Avalonia.Interactivity;
using StencilPad.ViewModels.Dialogs;

namespace StencilPad.UI.Dialogs;

public partial class SheetRenameDialog : DialogWindowBase
{
    public SheetRenameDialogViewModel ViewModel { get; }

    public SheetRenameDialog(string currentName)
    {
        InitializeComponent();
        ViewModel = new SheetRenameDialogViewModel(currentName);
        DataContext = ViewModel;

        Loaded += (s, e) =>
        {
            NameBox.Focus();
            NameBox.SelectAll();
        };
    }

    private async void OK_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.Name))
        {
            await MessageBoxWindow.ShowAsync(this, "Sheet name cannot be empty.", "Rename Sheet",
                SimpleMessageBoxButtons.Ok);
            return;
        }

        Result = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }
}
