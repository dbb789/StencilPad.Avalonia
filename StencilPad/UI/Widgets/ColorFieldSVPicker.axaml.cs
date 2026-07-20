using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;

namespace StencilPad.UI.Widgets;

public partial class ColorFieldSVPicker : UserControl
{
    public static readonly StyledProperty<double> SaturationProperty =
        AvaloniaProperty.Register<ColorFieldSVPicker, double>(nameof(Saturation), 0.0,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> BrightnessProperty =
        AvaloniaProperty.Register<ColorFieldSVPicker, double>(nameof(Brightness), 1.0,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<Color> HueColorProperty =
        AvaloniaProperty.Register<ColorFieldSVPicker, Color>(nameof(HueColor), Colors.Red);

    private bool _dragging;
    private IPointer? _capturedPointer;

    static ColorFieldSVPicker()
    {
        SaturationProperty.Changed.AddClassHandler<ColorFieldSVPicker>((picker, _) => picker.UpdateMarkerPosition());
        BrightnessProperty.Changed.AddClassHandler<ColorFieldSVPicker>((picker, _) => picker.UpdateMarkerPosition());
        HueColorProperty.Changed.AddClassHandler<ColorFieldSVPicker>((picker, _) => picker.UpdateGradient());
    }

    public double Saturation
    {
        get => GetValue(SaturationProperty);
        set => SetValue(SaturationProperty, value);
    }

    public double Brightness
    {
        get => GetValue(BrightnessProperty);
        set => SetValue(BrightnessProperty, value);
    }

    public Color HueColor
    {
        get => GetValue(HueColorProperty);
        set => SetValue(HueColorProperty, value);
    }

    public event Action? DragBegin;
    public event Action? DragEnd;
    public event EventHandler? ValueChanged;

    public ColorFieldSVPicker()
    {
        InitializeComponent();
        UpdateGradient();
        BrightnessRect.Fill = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new(Color.FromArgb(0, 0, 0, 0), 0),
                new(Colors.Black, 1)
            }
        };

        DragCanvas.PointerPressed += DragCanvas_PointerPressed;
        DragCanvas.PointerMoved += DragCanvas_PointerMoved;
        DragCanvas.PointerReleased += DragCanvas_PointerReleased;

        Loaded += (_, _) => UpdateMarkerPosition();
        SizeChanged += (_, _) => UpdateMarkerPosition();
    }

    private void DragCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(DragCanvas).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _dragging = true;
        _capturedPointer = e.Pointer;
        _capturedPointer.Capture(DragCanvas);
        DragBegin?.Invoke();

        SetValueFromPoint(e.GetPosition(DragCanvas));
        e.Handled = true;
    }

    private void DragCanvas_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragging)
        {
            SetValueFromPoint(e.GetPosition(DragCanvas));
        }
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
        if (DragCanvas.Bounds.Width <= 0 || DragCanvas.Bounds.Height <= 0)
        {
            return;
        }

        Saturation = Math.Clamp(p.X / DragCanvas.Bounds.Width, 0, 1);
        Brightness = Math.Clamp(1 - p.Y / DragCanvas.Bounds.Height, 0, 1);
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateGradient()
    {
        SaturationRect.Fill = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new(Colors.White, 0),
                new(HueColor, 1)
            }
        };
    }

    private void UpdateMarkerPosition()
    {
        if (DragCanvas.Bounds.Width <= 0 || DragCanvas.Bounds.Height <= 0)
        {
            return;
        }

        var x = Saturation * DragCanvas.Bounds.Width;
        var y = (1 - Brightness) * DragCanvas.Bounds.Height;

        Canvas.SetLeft(MarkerOuter, x - MarkerOuter.Width / 2);
        Canvas.SetTop(MarkerOuter, y - MarkerOuter.Height / 2);
        Canvas.SetLeft(Marker, x - Marker.Width / 2);
        Canvas.SetTop(Marker, y - Marker.Height / 2);
    }
}
