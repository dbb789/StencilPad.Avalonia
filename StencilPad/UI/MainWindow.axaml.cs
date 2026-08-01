using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using StencilPad.Common;
using StencilPad.ViewModels;

namespace StencilPad.UI;

public partial class MainWindow : Window, IAvaloniaDialogParent
{
    private static readonly DataFormat<TabItem> SheetTabFormat =
        DataFormat.CreateInProcessFormat<TabItem>("application/x-stencilpad-sheettab");

    private const double DragThreshold = 4;

    private TabItem? _pressedTabItem;
    private PointerPressedEventArgs? _pressedEventArgs;
    private Point _pressedPoint;
    private bool _dragStarted;

    public Window Window => this;
    
    public MainWindow()
    {
        InitializeComponent();

        SheetTabs.ContainerPrepared += OnSheetTabContainerPrepared;
    }

    private void OnSheetTabContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (e.Container is not TabItem tabItem)
        {
            return;
        }
        
        DragDrop.SetAllowDrop(tabItem, true);

        tabItem.AddHandler(InputElement.PointerPressedEvent, OnTabItemPointerPressed, handledEventsToo: true);
        tabItem.AddHandler(InputElement.PointerMovedEvent, OnTabItemPointerMoved, handledEventsToo: true);
        tabItem.AddHandler(InputElement.PointerReleasedEvent, OnTabItemPointerReleased, handledEventsToo: true);

        DragDrop.AddDragOverHandler(tabItem, OnTabItemDragOver);
        DragDrop.AddDropHandler(tabItem, OnTabItemDrop);
    }

    private void OnTabItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not TabItem tabItem ||
            !e.GetCurrentPoint(tabItem).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _pressedTabItem = tabItem;
        _pressedEventArgs = e;
        _pressedPoint = e.GetPosition(SheetTabs);
        _dragStarted = false;
    }

    private async void OnTabItemPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragStarted || _pressedTabItem is null || _pressedEventArgs is null ||
            !e.GetCurrentPoint(_pressedTabItem).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (!DragUtil.DragThresholdExceeded(_pressedPoint, e.GetPosition(SheetTabs)))
        {
            return;
        }

        var fromIndex = SheetTabs.IndexFromContainer(_pressedTabItem);

        if (fromIndex < 0)
        {
            return;
        }

        _dragStarted = true;

        var pressedEventArgs = _pressedEventArgs;

        var data = new DataTransfer();
        data.Add(DataTransferItem.Create(SheetTabFormat, _pressedTabItem));

        await DragDrop.DoDragDropAsync(pressedEventArgs, data, DragDropEffects.Move);

        _pressedTabItem = null;
        _pressedEventArgs = null;
        _dragStarted = false;
    }

    private void OnTabItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _pressedTabItem = null;
        _pressedEventArgs = null;
        _dragStarted = false;
    }

    private void OnTabItemDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(SheetTabFormat)
            ? DragDropEffects.Move
            : DragDropEffects.None;
    }

    private void OnTabItemDrop(object? sender, DragEventArgs e)
    {
        if (sender is not TabItem targetItem || !e.DataTransfer.Contains(SheetTabFormat))
        {
            return;
        }

        var sourceItem = e.DataTransfer.TryGetValue(SheetTabFormat);

        if (sourceItem is null)
        {
            return;
        }

        var fromIndex = SheetTabs.IndexFromContainer(sourceItem);
        var toIndex = SheetTabs.IndexFromContainer(targetItem);

        if (fromIndex < 0 || toIndex < 0 || toIndex == fromIndex)
        {
            return;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SheetTabReordered?.Invoke(fromIndex, toIndex);
        }
    }
}
