using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using StencilPad.Spatial;

namespace StencilPad.Canvases.Tools.Widgets;

public class InlineTextField : UserControl
{
    private readonly TextBox _textBox;

    public string Text
    {
        get => _textBox.Text ?? string.Empty;
        set => _textBox.Text = value;
    }

    public double TextFontSize
    {
        get => _textBox.FontSize;
        set => _textBox.FontSize = value;
    }

    public FontFamily TextFontFamily
    {
        get => _textBox.FontFamily;
        set => _textBox.FontFamily = value;
    }

    public double Rotation
    {
        get => _rotation;
        set
        {
            _rotation = value;

            RenderTransform = new TransformGroup
            {
                Children =
                [
                    new TranslateTransform(-3, 0),
                    new RotateTransform(_rotation)
                ]
            };
        }
    }

    private double _rotation;

    public Unit2D TextSize => MeasureText();
    
    public event Action? Committed;
    public event Action? Cancelled;

    public InlineTextField()
    {
        _textBox = new TextBox
        {
            Background = Brushes.White,
            BorderBrush = Brushes.CornflowerBlue,
            Padding = new Thickness(2),
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap
        };

        // Avalonia's TextBox has no PreviewKeyDown; subscribe with Tunnel routing
        // to intercept Enter before the TextBox's own AcceptsReturn handling.
        _textBox.AddHandler(KeyDownEvent, OnTextBoxKeyDown, RoutingStrategies.Tunnel);
        _textBox.LostFocus += OnTextBoxLostFocus;

        Content = _textBox;

        Loaded += (s, e) => _textBox.Focus();
    }

    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if ((e.KeyModifiers & KeyModifiers.Shift) != 0)
            {
                var pos = _textBox.CaretIndex;
                
                _textBox.Text = Text.Insert(pos, Environment.NewLine);
                _textBox.CaretIndex = pos + Environment.NewLine.Length;
                
                e.Handled = true;
                return;
            }
            
            Committed?.Invoke();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Cancelled?.Invoke();
            e.Handled = true;
        }
    }

    private void OnTextBoxLostFocus(object? sender, Avalonia.Input.FocusChangedEventArgs e)
    {
        Committed?.Invoke();
    }

    public void Focus()
    {
        _textBox.Focus();
    }
    
    private Unit2D MeasureText()
    {
        if (string.IsNullOrEmpty(Text))
        {
            return Unit2D.Zero;
        }

        var fontFamily = TextFontFamily;

        var ft = new FormattedText(
            Text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(fontFamily, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal),
            Unit.FromFontSizePoints(FontSize).Millimeters,
            Brushes.Black);

        return Unit2D.FromMillimeters(ft.Width, ft.Height);
    }
}
