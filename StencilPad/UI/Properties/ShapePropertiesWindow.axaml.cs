using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using SkiaSharp;
using StencilPad.Common;
using StencilPad.Models;
using StencilPad.Services;
using StencilPad.UI.Widgets;
using StencilPad.ViewModels.Properties;

namespace StencilPad.UI.Properties;

public partial class ShapePropertiesWindow : Window
{
    public ShapePropertiesViewModel ViewModel { get; }

    public ShapePropertiesWindow(Sheet sheet,
                                 ISettings settings,
                                 IResourceService resourceService,
                                 IOperationService operationService)
    {
        InitializeComponent();

        ViewModel = new ShapePropertiesViewModel(sheet,
                                                 settings,
                                                 resourceService,
                                                 operationService);
        DataContext = ViewModel;

        var startCapItems = ViewModel.CapIds.Select(
            id => new GeometryDropdown.Entry(CreateCapPath(resourceService, id, true))).ToList();
        StartCapDropdown.Items = startCapItems;

        var endCapItems = ViewModel.CapIds.Select(
            id => new GeometryDropdown.Entry(CreateCapPath(resourceService, id, false))).ToList();
        EndCapDropdown.Items = endCapItems;

        var lineStyleItems = ViewModel.LineStyles.Select(
            lineStyle => new GeometryDropdown.Entry(CreateLineStylePath(), lineStyle)).ToList();
        //LineStyleDropdown.Items = lineStyleItems;

        Loaded += (_, _) =>
        {
            FillColorField.DragBegin += ViewModel.DragBegin;
            FillColorField.DragEnd += ViewModel.DragEnd;
            LineColorField.DragBegin += ViewModel.DragBegin;
            LineColorField.DragEnd += ViewModel.DragEnd;
        };

        Unloaded += (_, _) =>
        {
            FillColorField.DragBegin -= ViewModel.DragBegin;
            FillColorField.DragEnd -= ViewModel.DragEnd;
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

    private SKPath CreateCapPath(IResourceService resourceService,
                                 GeometryResourceId resourceId,
                                 bool startCap)
    {

        return resourceService.Get(resourceId).Path;
    }

    private SKPath CreateLineStylePath()
    {
        return new SKPath();
    }
}
