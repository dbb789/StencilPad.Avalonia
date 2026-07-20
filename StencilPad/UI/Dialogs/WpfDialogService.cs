using Avalonia.Controls;
using StencilPad.Services;
using StencilPad.Spatial;

namespace StencilPad.UI.Dialogs;

public class WpfDialogService : IDialogService
{
    private readonly Window _owner;

    public WpfDialogService(IAvaloniaDialogParent parent)
    {
        _owner = parent.Window;
    }

    public async Task<string?> ShowRenameDialogAsync(string currentName)
    {
        var dialog = new SheetRenameDialog(currentName);

        if (await ShowModalAsync(dialog))
        {
            return dialog.ViewModel.Name.Trim();
        }

        return null;
    }

    public async Task<(Unit Spacing, int Subdivisions)?> ShowGridSettingsDialogAsync(Unit currentSpacing,
                                                                                      int currentSubdivisions,
                                                                                      UnitSettings unitSettings)
    {
        var dialog = new GridSettingsDialog(currentSpacing,
                                            currentSubdivisions,
                                            unitSettings);

        if (await ShowModalAsync(dialog))
        {
            return (dialog.ViewModel.Spacing, dialog.ViewModel.Subdivisions);
        }

        return null;
    }

    public async Task<Fraction?> ShowUnitScaleDialogAsync(Fraction current)
    {
        var dialog = new UnitScaleDialog(current);

        if (await ShowModalAsync(dialog))
        {
            return dialog.ViewModel.Fraction;
        }

        return null;
    }

    public async Task<bool> ShowConfirmationAsync(string message, string title, bool defaultYes = true)
    {
        var result = await MessageBoxWindow.ShowAsync(_owner,
                                                       message,
                                                       title,
                                                       SimpleMessageBoxButtons.YesNo,
                                                       defaultYes ? SimpleMessageBoxResult.Yes : SimpleMessageBoxResult.No);

        return result == SimpleMessageBoxResult.Yes;
    }
    
    public Task ShowWarningAsync(string message, string title)
    {
        return MessageBoxWindow.ShowAsync(_owner, message, title, SimpleMessageBoxButtons.Ok);
    }

    public Task ShowErrorAsync(string message, string title)
    {
        return MessageBoxWindow.ShowAsync(_owner, message, title, SimpleMessageBoxButtons.Ok);
    }

    // NOTE: Avalonia's Window.ShowDialog() is Task-based and has no
    // WPF-style bool? DialogResult property, so callers await the dialog
    // closing rather than blocking the UI thread on it (blocking with
    // GetAwaiter().GetResult() previously deadlocked the app - Avalonia does
    // not pump a nested dispatcher frame the way WPF's synchronous
    // ShowDialog() does).
    private async Task<bool> ShowModalAsync(DialogWindowBase dialog)
    {
        await dialog.ShowDialog(_owner);
        return dialog.Result;
    }
}
