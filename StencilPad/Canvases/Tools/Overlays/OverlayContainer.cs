using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace StencilPad.Canvases.Tools.Overlays;

public class OverlayContainer : Decorator
{
    public Control? ActiveOverlay
    {
        get => _child;
        set => SetChild(value);
    }

    private Control? _child;

    private void SetChild(Control? newChild)
    {
        if (_child is not null)
        {
            _child.Loaded -= ChildLoaded;
        }

        _child = newChild;
        Child = _child;

        if (_child is not null)
        {
            _child.Focusable = true;
            _child.Focus();
            _child.Loaded += ChildLoaded;
        }
    }

    private void ChildLoaded(object? sender, RoutedEventArgs e)
    {
        _child?.Focus();
    }
}
