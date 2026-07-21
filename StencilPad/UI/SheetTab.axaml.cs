using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using StencilPad.Canvases.UI;
using StencilPad.ViewModels;

namespace StencilPad.UI;

public partial class SheetTab : UserControl
{
    private const double ZoomStep = 1.1;
    private const double ZoomMin = 0.1;
    private const double ZoomMax = 5.0;

    private bool _showGrid = true;
    public bool ShowGrid
    {
        get => _showGrid;
        set => _showGrid = value;
    }

    private bool _snapToGrid = true;
    public bool SnapToGrid
    {
        get => _snapToGrid;
        set => _snapToGrid = value;
    }

    private bool _snapToPoint = false;
    public bool SnapToPoint
    {
        get => _snapToPoint;
        set => _snapToPoint = value;
    }

    private SheetTabViewModel? _viewModel;
    private bool _updatingZoom;

    private Point _lastMousePosition;
    private Vector _lastOffset;
    private IPointer? _capturedPointer;
    
    public SheetTab()
    {
        InitializeComponent();

        SheetCanvas.CanvasReady += SheetCanvasReady;
        
        Scroll.AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
        Scroll.PointerPressed += OnPointerPressed;
        Scroll.PointerMoved += OnPointerMoved;
        Scroll.PointerReleased += OnPointerReleased;

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.DetachCanvas();
        }

        _viewModel = DataContext as SheetTabViewModel;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.AttachCanvas(SheetCanvas);

            var viewModel = _viewModel;

            Dispatcher.UIThread.Post(() =>
            {
                // Potential race condition.
                if (_viewModel == viewModel)
                {
                    SetZoom(viewModel.Zoom);
                    CentreScroll();
                }
            });
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SheetTabViewModel.Zoom) && !_updatingZoom)
        {
            ApplyZoomCentred(_viewModel!.Zoom);
        }
    }

    private void SheetCanvasReady()
    {
        if (DataContext is SheetTabViewModel vm)
        {
            vm.AttachCanvas(SheetCanvas);
            _viewModel = vm;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        SheetCanvas.Viewport.ViewportChanged += () =>
        {
            Scroll.MaxWidth = SheetCanvas.Viewport.ToPixels(SheetCanvas.Viewport.Size.X) + 32;
            Scroll.MaxHeight = SheetCanvas.Viewport.ToPixels(SheetCanvas.Viewport.Size.Y) + 32;
        };

        Dispatcher.UIThread.Post(CentreScroll);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.MiddleButtonPressed)
        {
            _lastMousePosition = e.GetPosition(this);
            _lastOffset = Scroll.Offset;
            _capturedPointer = e.Pointer;
            e.Pointer.Capture(Scroll);
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_capturedPointer is not null)
        {
            var delta = e.GetPosition(this) - _lastMousePosition;
            Scroll.Offset = _lastOffset - new Vector(delta.X, delta.Y);
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_capturedPointer is not null)
        {
            _capturedPointer.Capture(null);
            _capturedPointer = null;
            e.Handled = true;
        }
    }

    private double ScrollableWidth => Math.Max(0, Scroll.Extent.Width - Scroll.Viewport.Width);
    private double ScrollableHeight => Math.Max(0, Scroll.Extent.Height - Scroll.Viewport.Height);

    private void ApplyZoomCentred(double targetZoom)
    {
        double hFraction = (ScrollableWidth > 0) ? Scroll.Offset.X / ScrollableWidth : 0.5;
        double vFraction = (ScrollableHeight > 0) ? Scroll.Offset.Y / ScrollableHeight : 0.5;

        SetZoom(Math.Clamp(targetZoom, ZoomMin, ZoomMax));

        void OnLayoutUpdated(object? s, EventArgs e)
        {
            Scroll.LayoutUpdated -= OnLayoutUpdated;
            Scroll.Offset = new Vector(hFraction * ScrollableWidth, vFraction * ScrollableHeight);
        }

        Scroll.LayoutUpdated += OnLayoutUpdated;
    }

    private void ApplyZoom(double targetZoom, double anchorX, double anchorY)
    {
        double newZoom = Math.Clamp(targetZoom, ZoomMin, ZoomMax);
        double actualFactor = newZoom / SheetCanvas.Zoom;

        double newHOffset = (Scroll.Offset.X + anchorX) * actualFactor - anchorX;
        double newVOffset = (Scroll.Offset.Y + anchorY) * actualFactor - anchorY;

        SetZoom(newZoom);

        void OnLayoutUpdated(object? s, EventArgs e)
        {
            Scroll.LayoutUpdated -= OnLayoutUpdated;
            Scroll.Offset = new Vector(newHOffset, newVOffset);
        }

        Scroll.LayoutUpdated += OnLayoutUpdated;
    }

    private void SetZoom(double zoom)
    {
        _updatingZoom = true;
        
        SheetCanvas.Zoom = zoom;

        if (_viewModel is not null)
        {
            _viewModel.Zoom = zoom;
        }
        
        _updatingZoom = false;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        double factor = e.Delta.Y > 0 ? ZoomStep : 1.0 / ZoomStep;
        var mousePos = e.GetPosition(Scroll);
        
        ApplyZoom(SheetCanvas.Zoom * factor, mousePos.X, mousePos.Y);

        e.Handled = true;
    }

    private void CentreScroll()
    {
        Scroll.Offset = new Vector(ScrollableWidth / 2.0, ScrollableHeight / 2.0);
    }
}
