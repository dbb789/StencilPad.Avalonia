using StencilPad.ViewModels;
using StencilPad.UI;
using StencilPad.Services;

namespace StencilPad.Controllers;

public class AppController
{
    private readonly MainWindow _mainWindow;
    private readonly MainWindowController _mainWindowController;
    private readonly IDialogService _dialogService;
    private bool _closeConfirmed;
    
    public AppController(MainWindow mainWindow,
                         MainWindowViewModel mainWindowViewModel,
                         MainWindowController mainWindowController,
                         IDialogService dialogService)
    {
        _mainWindow = mainWindow;
        _mainWindow.DataContext = mainWindowViewModel;
        _mainWindowController = mainWindowController;
        _dialogService = dialogService;

        // NOTE: Window.Closing can't await a dialog and decide synchronously
        // like the WPF version did (blocking on the confirmation dialog here
        // deadlocks - see WpfDialogService's async conversion). Instead we
        // always cancel the first Closing, run the confirmation
        // asynchronously, and if confirmed, re-invoke Close() with a flag set
        // so the second Closing pass goes through.
        mainWindow.Closing += (_, e) =>
        {
            if (_closeConfirmed)
            {
                return;
            }

            e.Cancel = true;

            ConfirmCloseThenClose();
        };
    }

    public void Initialize()
    {
        _mainWindowController.Initialize();
        _mainWindow.Show();
    }

    public async void OpenFile(string filename)
    {
        await _mainWindowController.OpenProject(filename);
    }

    private async void ConfirmCloseThenClose()
    {
        if (!await ConfirmCloseAsync())
        {
            return;
        }

        _closeConfirmed = true;
        _mainWindow.Close();
    }
    
    public async Task<bool> ConfirmCloseAsync()
    {
        if (_mainWindowController.SaveState)
        {
            return true;
        }

        return await _dialogService.ShowConfirmationAsync(
            "You have unsaved changes. Are you sure you want to close without saving?",
            "Unsaved Changes",
            false);
    }
}
