using Avalonia.Controls;
using Avalonia.Interactivity;
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

    private List<GeometryDropdownEntry> _startCapItems;
    private List<GeometryDropdownEntry> _endCapItems;
    private List<GeometryDropdownEntry> _lineStyleItems;
    
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

        _startCapItems = new();
        _endCapItems = new();
        _lineStyleItems = new();

        
        Loaded += (_, _) =>
        {
            CreateDropDownItems(resourceService);

            FillColorField.DragBegin += ViewModel.DragBegin;
            FillColorField.DragEnd += ViewModel.DragEnd;
            LineColorField.DragBegin += ViewModel.DragBegin;
            LineColorField.DragEnd += ViewModel.DragEnd;

            DataContext = ViewModel;
        };

        Unloaded += (_, _) =>
        {
            DataContext = null;
            
            FillColorField.DragBegin -= ViewModel.DragBegin;
            FillColorField.DragEnd -= ViewModel.DragEnd;
            LineColorField.DragBegin -= ViewModel.DragBegin;
            LineColorField.DragEnd -= ViewModel.DragEnd;
            
            DestroyDropDownItems();
        };
    }

    private void CreateDropDownItems(IResourceService resourceService)
    {
        DestroyDropDownItems();
        
        _startCapItems = ViewModel.CapIds.Select(
            id => CreateCapPath(resourceService, id, true)).ToList();

        _endCapItems = ViewModel.CapIds.Select(
            id => CreateCapPath(resourceService, id, false)).ToList();

        _lineStyleItems = ViewModel.LineStyles.Select(
            lineStyle => CreateLineStylePath(lineStyle)).ToList();

        StartCapDropdown.Items = _startCapItems;
        EndCapDropdown.Items = _endCapItems;
        LineStyleDropdown.Items = _lineStyleItems;
    }
    
    private void DestroyDropDownItems()
    {
        StartCapDropdown.Items = [];
        EndCapDropdown.Items = [];
        LineStyleDropdown.Items = [];
        
        foreach (var item in _startCapItems)
        {
            item.Path.Dispose();
            item.Paint?.Dispose();
        }

        _startCapItems.Clear();
        
        foreach (var item in _endCapItems)
        {
            item.Path.Dispose();
            item.Paint?.Dispose();
        }

        _endCapItems.Clear();
        
        foreach (var item in _lineStyleItems)
        {
            item.Path.Dispose();
            item.Paint?.Dispose();
        }

        _lineStyleItems.Clear();
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

    private GeometryDropdownEntry CreateCapPath(IResourceService resourceService,
                                                 GeometryResourceId resourceId,
                                                 bool startCap)
    {
        var rotation = SKMatrix.CreateRotationDegrees(startCap ? -90 : 90);
        var path = new SKPath();

        var capResource = resourceService.Get(resourceId);

        capResource.Path.Transform(rotation, path);

        var capOffset = capResource.Size.Y.Millimeters;

        if (startCap)
        {
            path.AddPoly([new((float)capOffset, 0), new(4, 0)], false);
        }
        else
        {
            path.AddPoly([new(-(float)capOffset, 0), new(-4, 0)], false);
        }

        return new(path);
    }
    
    private GeometryDropdownEntry CreateLineStylePath(LineStyle lineStyle)
    {
        var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 0.2f,
            IsAntialias = true,
            PathEffect = lineStyle.IsSolid ?
                null : SKPathEffect.CreateDash(lineStyle.ToDashPattern(), 0)
        };
        
        var path = new SKPath();

        path.AddPoly([new(-7, 0), new(7, 0)], false);
        
        return new(path, paint);
    }
}
