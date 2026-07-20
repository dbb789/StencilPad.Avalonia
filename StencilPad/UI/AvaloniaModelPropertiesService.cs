using Avalonia.Controls;
using StencilPad.Common;
using StencilPad.Models;
using StencilPad.Services;
using StencilPad.UI.Properties;

namespace StencilPad.UI;

public class AvaloniaModelPropertiesService : IModelPropertiesService
{
    private readonly Window _owner;
    private readonly ISettings _settings;
    private readonly IResourceService _resourceService;
    private readonly IOperationService _operationService;
    private Window? _openWindow;

    public AvaloniaModelPropertiesService(IAvaloniaDialogParent parent,
                                          ISettings settings,
                                          IResourceService resourceService,
                                          IOperationService operationService)
    {
        _owner = parent.Window;
        _settings = settings;
        _resourceService = resourceService;
        _operationService = operationService;
    }

    public void CloseAll()
    {
        _openWindow?.Close();
        _openWindow = null;
    }

    public void ShowVertexCornerProperties(Sheet sheet)
    {
        _openWindow?.Close();

        var window = new VertexCornerPropertiesWindow(sheet, _settings, _operationService);

        _openWindow = window;
        window.Closed += (_, _) => _openWindow = null;

        PositionAndShow(window);
    }

    public void ShowMarkerPathProperties(Sheet sheet)
    {
        _openWindow?.Close();

        var window = new MarkerPathPropertiesWindow(sheet, _settings, _resourceService, _operationService);

        _openWindow = window;
        window.Closed += (_, _) => _openWindow = null;

        PositionAndShow(window);
    }

    public void ShowShapeProperties(Sheet sheet)
    {
        _openWindow?.Close();

        var window = new ShapePropertiesWindow(sheet, _settings, _resourceService, _operationService);

        _openWindow = window;
        window.Closed += (_, _) => _openWindow = null;

        PositionAndShow(window);
    }
    
    public void ShowTextProperties(Sheet sheet)
    {
        _openWindow?.Close();

        var window = new TextPropertiesWindow(sheet, _settings, _operationService);

        _openWindow = window;
        window.Closed += (_, _) => _openWindow = null;

        PositionAndShow(window);
    }
    
    public void ShowRulerProperties(Sheet sheet)
    {
        _openWindow?.Close();

        var window = new RulerPropertiesWindow(sheet, _settings, _operationService);

        _openWindow = window;
        window.Closed += (_, _) => _openWindow = null;

        PositionAndShow(window);
    }

    public void ShowImageProperties(Sheet sheet)
    {
        _openWindow?.Close();

        var window = new ImagePropertiesWindow(sheet, _settings, _operationService);

        _openWindow = window;
        window.Closed += (_, _) => _openWindow = null;

        PositionAndShow(window);
    }

    // NOTE: WPF positioned these small property windows anchored to the
    // current mouse cursor position (via Mouse.GetPosition/PresentationSource,
    // both WPF-only APIs with no direct Avalonia equivalent). That's a cosmetic
    // nicety, not core functionality, so simplified to Avalonia's built-in
    // owner-relative centering instead.
    private void PositionAndShow(Window window)
    {
        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        window.Show(_owner);
    }
}
