using Avalonia.Controls;
using Avalonia.Interactivity;
using StencilPad.Canvases.Common;
using StencilPad.Canvases.ViewModels.Properties;
using StencilPad.Common;
using StencilPad.Models;
using StencilPad.Services;

namespace StencilPad.Canvases.UI.Properties;

public partial class HandlePropertiesWindow : Window
{
    public HandlePropertiesViewModel ViewModel { get; }

    public HandlePropertiesWindow(Sheet sheet,
                                  IHandleMap handleMap,
                                  IOperationService operationService,
                                  ISettings settings)
    {
        InitializeComponent();

        ViewModel = new HandlePropertiesViewModel(sheet, handleMap, operationService, settings);
        DataContext = ViewModel;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        ViewModel.Dispose();
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
