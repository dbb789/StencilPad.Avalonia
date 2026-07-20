using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;

namespace StencilPad.UI.Widgets;

public abstract class ColorFieldSliderBase : UserControl
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<ColorFieldSliderBase, double>(nameof(Value), 0.0,
            defaultBindingMode: BindingMode.TwoWay);

    static ColorFieldSliderBase()
    {
        ValueProperty.Changed.AddClassHandler<ColorFieldSliderBase>((slider, _) => slider.OnValueChanged());
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    protected abstract double DisplayScale { get; }

    private Canvas? _dragCanvas;
    private Rectangle? _marker;
    private TextBox? _entryBox;
    private bool _updatingBox;
    private bool _dragging;
    private IPointer? _capturedPointer;

    public event Action? DragBegin;
    public event Action? DragEnd;
    public event EventHandler? ValueChanged;

    protected void InitializeSlider(Canvas dragCanvas, Rectangle marker)
    {
        _dragCanvas = dragCanvas;
        _marker = marker;

        var sliderContent = Content;
        Content = null;

        _entryBox = new TextBox
        {
            Width = 40,
            VerticalContentAlignment = VerticalAlignment.Center,
            TextAlignment = Avalonia.Media.TextAlignment.Right,
            Padding = new Thickness(2, 1, 2, 1),
            Margin = new Thickness(4, 0, 0, 0)
        };
        _entryBox.TextChanged += EntryBox_TextChanged;

        var panel = new DockPanel();
        DockPanel.SetDock(_entryBox, Dock.Right);
        panel.Children.Add(_entryBox);

        if (sliderContent is Control sliderControl)
        {
            panel.Children.Add(sliderControl);
        }

        Content = panel;

        dragCanvas.PointerPressed += DragCanvas_PointerPressed;
        dragCanvas.PointerMoved += DragCanvas_PointerMoved;
        dragCanvas.PointerReleased += DragCanvas_PointerReleased;

        Loaded += (_, _) =>
        {
            UpdateGradient();
            UpdateMarkerPosition();
            UpdateEntryBox();
        };

        SizeChanged += (_, _) => UpdateMarkerPosition();
    }

    protected abstract void UpdateGradient();

    private void OnValueChanged()
    {
        UpdateMarkerPosition();
        UpdateEntryBox();
    }

    private void EntryBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_updatingBox || _entryBox is null)
        {
            return;
        }

        if (!double.TryParse(_entryBox.Text, out var parsed))
        {
            return;
        }

        Value = Math.Clamp(parsed / DisplayScale, 0.0, 1.0);
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateEntryBox()
    {
        if (_entryBox is null)
        {
            return;
        }

        _updatingBox = true;
        _entryBox.Text = ((int)Math.Round(Value * DisplayScale)).ToString();
        _updatingBox = false;
    }

    private void DragCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_dragCanvas is null || !e.GetCurrentPoint(_dragCanvas).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _dragging = true;
        _capturedPointer = e.Pointer;
        _capturedPointer.Capture(_dragCanvas);
        DragBegin?.Invoke();

        SetValueFromPoint(e.GetPosition(_dragCanvas));
        e.Handled = true;
    }

    private void DragCanvas_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging || _dragCanvas is null)
        {
            return;
        }

        SetValueFromPoint(e.GetPosition(_dragCanvas));
    }

    private void DragCanvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        _capturedPointer?.Capture(null);
        _capturedPointer = null;
        DragEnd?.Invoke();
        e.Handled = true;
    }

    private void SetValueFromPoint(Point p)
    {
        if (_dragCanvas is null || _marker is null)
        {
            return;
        }

        var halfMarker = _marker.Width / 2;
        var usable = _dragCanvas.Bounds.Width - _marker.Width;

        if (usable <= 0)
        {
            return;
        }

        Value = Math.Clamp((p.X - halfMarker) / usable, 0, 1);
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateMarkerPosition()
    {
        if (_dragCanvas is null || _marker is null || _dragCanvas.Bounds.Width <= 0)
        {
            return;
        }

        var halfMarker = _marker.Width / 2;
        var usable = _dragCanvas.Bounds.Width - _marker.Width;
        var x = halfMarker + Value * usable;

        Canvas.SetLeft(_marker, x - halfMarker);
        Canvas.SetTop(_marker, (_dragCanvas.Bounds.Height - _marker.Height) / 2);
    }
}
