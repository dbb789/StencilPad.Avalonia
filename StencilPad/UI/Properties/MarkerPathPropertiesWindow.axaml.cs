using Avalonia.Controls;
using Avalonia.Interactivity;
using SkiaSharp;
using StencilPad.Common;
using StencilPad.Models;
using StencilPad.Services;
using StencilPad.UI.Widgets;
using StencilPad.ViewModels.Properties;

namespace StencilPad.UI.Properties;

public partial class MarkerPathPropertiesWindow : Window
{
    public MarkerPathPropertiesViewModel ViewModel { get; }

    public MarkerPathPropertiesWindow(Sheet sheet,
                                      ISettings settings,
                                      IResourceService resourceService,
                                      IOperationService operationService)
    {
        InitializeComponent();

        ViewModel = new MarkerPathPropertiesViewModel(sheet,
                                                      settings,
                                                      resourceService,
                                                      operationService);
        DataContext = ViewModel;

        var markerTypeItems = ViewModel.MarkerTypeIds.Select(
            id => new GeometryDropdownEntry(GetMarkerPath(resourceService, id))).ToList();

        MarkerTypeDropdown.Items = markerTypeItems;

        Loaded += (_, _) =>
        {
            MarkerColorField.DragBegin += ViewModel.DragBegin;
            MarkerColorField.DragEnd += ViewModel.DragEnd;
            LineColorField.DragBegin += ViewModel.DragBegin;
            LineColorField.DragEnd += ViewModel.DragEnd;
        };

        Unloaded += (_, _) =>
        {
            MarkerColorField.DragBegin -= ViewModel.DragBegin;
            MarkerColorField.DragEnd -= ViewModel.DragEnd;
            LineColorField.DragBegin -= ViewModel.DragBegin;
            LineColorField.DragEnd -= ViewModel.DragEnd;
        };
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

    private SKPath GetMarkerPath(IResourceService resourceService,
                                 GeometryResourceId resourceId)
    {
        return resourceService.Get(resourceId).Path;
    }
}
