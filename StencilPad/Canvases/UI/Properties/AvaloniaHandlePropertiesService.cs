using Avalonia.Controls;
using StencilPad.Canvases.Common;
using StencilPad.Common;
using StencilPad.Models;
using StencilPad.Services;
using StencilPad.UI;

namespace StencilPad.Canvases.UI.Properties;

public class AvaloniaHandlePropertiesService : IHandlePropertiesService
{
    private readonly Window _owner;
    private readonly IOperationService _operationService;
    private readonly ISettings _settings;
    private Window? _openWindow;

    public AvaloniaHandlePropertiesService(IAvaloniaDialogParent parent,
                                           IOperationService operationService,
                                           ISettings settings)
    {
        _owner = parent.Window;
        _operationService = operationService;
        _settings = settings;
    }

    public void CloseAll()
    {
        _openWindow?.Close();
        _openWindow = null;
    }

    public void ShowHandleProperties(Sheet sheet, IHandleMap handleMap)
    {
        _openWindow?.Close();

        var window = new HandlePropertiesWindow(sheet, handleMap, _operationService, _settings);

        _openWindow = window;
        window.Closed += (_, _) => _openWindow = null;

        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        window.Show(_owner);
    }
}
