using Avalonia.Media;
using SkiaSharp;
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
    
    private SKMatrix? _matrix;
    private TextStyle _style;
    private UnitBounds? _bounds;
    private string _text;

    public event Action? RendererDirty;
    
    public TextRenderer()
    {
        _style = new TextStyle();
        _text = "";
    }

    public void Dispose()
    {
        // ...
    }

    public void SetTransform(UnitTransform transform)
    {
        _matrix = transform.CreateMatrix();
        InvokeRendererDirty();
    }

    public void SetStyle(TextStyle style)
    {
        _style = style;
        InvokeRendererDirty();
    }

    public void SetBounds(UnitBounds? bounds)
    {
        _bounds = bounds;
        InvokeRendererDirty();
    }
    
    public void SetText(string text)
    {
        _text = text;
        InvokeRendererDirty();
    }

    public void Render(SKCanvas canvas)
    {
        canvas.Save();
        
        if (_matrix is not null)
        {
            canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, _matrix.Value));
        }

        canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, SKMatrix.CreateScale(1, -1)));

        var point = new SKPoint(0, 0);
        var font = new SKFont(SKTypeface.FromFamilyName(_style.Font ?? FallbackFont.Name),
                              (float)Unit.FromFontSizePoints(_style.Size).Millimeters);

        var align = _style.Justification switch
        {
            Justification.Left => SKTextAlign.Left,
            Justification.Center => SKTextAlign.Center,
            Justification.Right => SKTextAlign.Right,
            _ => SKTextAlign.Left
        };
        
        var paint = new SKPaint
        {
            Color = new SKColor(_style.Color.R, _style.Color.G, _style.Color.B, _style.Color.A),
            IsAntialias = true,
            IsDither = true
        };
        
        canvas.DrawText(_text, point, align, font, paint);

        canvas.Restore();
    }

    // public void Render(DrawingContext dc)
    // {
    //     if (_formattedText is null || string.IsNullOrEmpty(_text))
    //     {
    //         return;
    //     }

    //     using var transformState = _transform is not null ? dc.PushTransform(_transform.Value) : default;

    //     // Account for WPF's inverted Y-axis by flipping the Y-axis for text rendering.
    //     using var flipState = dc.PushTransform(FlipY.Value);

    //     if (_bounds is not null)
    //     {
    //         var flippedBounds = UnitBounds.FromCenterSize(new Unit2D(_bounds.Value.Center.X, -_bounds.Value.Center.Y),
    //                                                       _bounds.Value.Size);
    //         var clipRect = flippedBounds.Millimeters;
    //         var height = _formattedText.Height;

    //         Point textPos;

    //         switch (_style.Justification)
    //         {
    //         case Justification.Center:
    //             textPos = new Point((clipRect.Left + clipRect.Right) / 2, clipRect.Top);
    //             break;
    //         case Justification.Right:
    //             textPos = new Point(clipRect.Right, clipRect.Top);
    //             break;
    //         case Justification.Left:
    //         default:
    //             textPos = new Point(clipRect.Left, clipRect.Top);
    //             break;
    //         }

    //         using var clipState = dc.PushClip(clipRect);
    //         dc.DrawText(_formattedText, textPos);
    //     }
    //     else
    //     {
    //         dc.DrawText(_formattedText, new Point(0, 0));
    //     }
    // }

    private void InvokeRendererDirty()
    {
        RendererDirty?.Invoke();
    }
}
