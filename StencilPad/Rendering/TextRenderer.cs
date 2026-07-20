using System.Globalization;
using Avalonia;
using Avalonia.Media;
using StencilPad.Models;
using StencilPad.Models.Resolvers;
using StencilPad.Spatial;

namespace StencilPad.Rendering;

public class TextRenderer : ITextWalker, IWalkerRenderer
{
    private static readonly FontFamily FallbackFont = new("Arial");
    private static readonly Transform FlipY;
    
    static TextRenderer()
    {
        FlipY = new ScaleTransform(1, -1);
    }
    
    private Transform? _transform;
    private TextStyle _style;
    private UnitBounds? _bounds;
    private string _text;
    private FormattedText? _formattedText;

    public event Action? RendererDirty;
    
    public TextRenderer()
    {
        _transform = null;
        _style = new TextStyle();
        _text = "";
    }

    public void Dispose()
    {
        // ...
    }

    public void SetTransform(UnitTransform transform)
    {
        _transform = transform.CreateGroupTransform();
        InvokeRendererDirty();
    }

    public void SetStyle(TextStyle style)
    {
        _style = style;
        RebuildFormattedText();
        InvokeRendererDirty();
    }

    public void SetBounds(UnitBounds? bounds)
    {
        _bounds = bounds;
        RebuildFormattedText();
        InvokeRendererDirty();
    }
    
    public void SetText(string text)
    {
        _text = text;
        RebuildFormattedText();
        InvokeRendererDirty();
    }
    
    public void Render(DrawingContext dc)
    {
        if (_formattedText is null || string.IsNullOrEmpty(_text))
        {
            return;
        }

        using var transformState = _transform is not null ? dc.PushTransform(_transform.Value) : default;

        // Account for WPF's inverted Y-axis by flipping the Y-axis for text rendering.
        using var flipState = dc.PushTransform(FlipY.Value);

        if (_bounds is not null)
        {
            var flippedBounds = UnitBounds.FromCenterSize(new Unit2D(_bounds.Value.Center.X, -_bounds.Value.Center.Y),
                                                          _bounds.Value.Size);
            var clipRect = flippedBounds.Millimeters;
            var height = _formattedText.Height;

            Point textPos;

            switch (_style.Justification)
            {
            case Justification.Center:
                textPos = new Point((clipRect.Left + clipRect.Right) / 2, clipRect.Top);
                break;
            case Justification.Right:
                textPos = new Point(clipRect.Right, clipRect.Top);
                break;
            case Justification.Left:
            default:
                textPos = new Point(clipRect.Left, clipRect.Top);
                break;
            }

            using var clipState = dc.PushClip(clipRect);
            dc.DrawText(_formattedText, textPos);
        }
        else
        {
            dc.DrawText(_formattedText, new Point(0, 0));
        }
    }
    
    private void RebuildFormattedText()
    {
        if (string.IsNullOrEmpty(_text))
        {
            _formattedText = null;
            return;
        }

        var fontFamily = ResolveFont(_style.Font);

        _formattedText = new FormattedText(
            _text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(fontFamily, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal),
            Unit.FromFontSizePoints(_style.Size).Millimeters,
            new SolidColorBrush(_style.Color))
        {
            Trimming = TextTrimming.None,
            TextAlignment = GetTextAlignment(_style.Justification)
        };
    }

    private static FontFamily ResolveFont(string fontName)
    {
        if (FontManager.Current.SystemFonts.Any(
                f => string.Equals(f.Name, fontName, StringComparison.OrdinalIgnoreCase)))
        {
            return new FontFamily(fontName);
        }

        return FallbackFont;
    }

    private static TextAlignment GetTextAlignment(Justification justification)
    {
        return justification switch
        {
            Justification.Left => TextAlignment.Left,
            Justification.Center => TextAlignment.Center,
            Justification.Right => TextAlignment.Right,
            _ => TextAlignment.Left
        };
    }
    
    private void InvokeRendererDirty()
    {
        RendererDirty?.Invoke();
    }
}
