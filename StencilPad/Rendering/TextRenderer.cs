using SkiaSharp;
using StencilPad.Models;
using StencilPad.Models.Resolvers;
using StencilPad.Spatial;

namespace StencilPad.Rendering;

public class TextRenderer : ITextWalker, IWalkerRenderer
{
    private static readonly string FallbackFont = "Arial";
    private static readonly string [] NewlineSplit = new[] { "\r\n", "\r", "\n" };

    private class RenderedText : IDisposable
    {
        public SKMatrix Matrix = SKMatrix.CreateIdentity();
        public SKPoint Point = new SKPoint(0, 0);
        public SKFont Font = new();
        public SKTextAlign Align = SKTextAlign.Left;
        public SKPaint Paint = new();
        public string [] Lines = Array.Empty<string>();

        public void Reset()
        {
            Matrix = SKMatrix.CreateIdentity();
            Point = new SKPoint(0, 0);
            Align = SKTextAlign.Left;
            Paint.Reset();
            Lines = Array.Empty<string>();
        }
        
        public void Dispose()
        {
            Font.Dispose();
            Paint.Dispose();
            Lines = Array.Empty<string>();
        }
    }

    private SKMatrix _matrix;
    private TextStyle _style;
    private UnitBounds? _bounds;
    private string _text;
    private bool _textDirty;
    
    private RenderBuffer<RenderedText> _renderedText;

    public event Action? RendererDirty;
    
    public TextRenderer()
    {
        _style = new TextStyle();
        _text = "";
        _renderedText = new();
        _matrix = SKMatrix.CreateScale(1, -1);
    }

    public void Dispose()
    {
        // ...
    }

    public void SetTransform(UnitTransform transform)
    {
        _matrix = SKMatrix.Concat(transform.CreateMatrix(),
                                  SKMatrix.CreateScale(1, -1));
        MarkTextDirty();
    }

    public void SetStyle(TextStyle style)
    {
        _style = style;
        MarkTextDirty();
    }

    public void SetBounds(UnitBounds? bounds)
    {
        _bounds = bounds;
        MarkTextDirty();
    }
    
    public void SetText(string text)
    {
        _text = text;
        MarkTextDirty();
    }

    public void PreRender()
    {
        if (_textDirty)
        {
            _textDirty = false;
            RebuildText();
        }
    }
    
    public void Render(SKCanvas canvas, GRContext? context)
    {
        using var textHandle = _renderedText.TryRead();

        if (!textHandle.IsValid)
        {
            return;
        }

        var text = textHandle.Buffer;
        var point = text.Point;
        
        canvas.Save();
        canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, text.Matrix));

        foreach (var line in text.Lines)
        {
            point.Y += text.Font.Size;
            canvas.DrawText(line, point, text.Align, text.Font, text.Paint);
        }

        canvas.Restore();
    }

    private void MarkTextDirty()
    {
        _textDirty = true;
        RendererDirty?.Invoke();
    }
    
    private void RebuildText()
    {
        var point = new SKPoint(0, 0);

        if (_bounds is not null)
        {
            point = new SKPoint((float)_bounds.Value.NW.X.Millimeters,
                                (float)-_bounds.Value.NW.Y.Millimeters);
        }
                
        var font = new SKFont(SKTypeface.FromFamilyName(_style.Font ?? FallbackFont),
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
            Color = new SKColor(_style.Color.R,
                                _style.Color.G,
                                _style.Color.B,
                                _style.Color.A),
                                
            IsAntialias = true,
            IsDither = true
        };

        var lines = _text.Split(NewlineSplit, StringSplitOptions.None);

        using var textHandle = _renderedText.TryWrite();

        if (!textHandle.IsValid)
        {
            return;
        }
        
        textHandle.Buffer.Reset();
        textHandle.Buffer.Matrix = _matrix;
        textHandle.Buffer.Point = point;
        textHandle.Buffer.Font = font;
        textHandle.Buffer.Align = align;
        textHandle.Buffer.Paint = paint;
        textHandle.Buffer.Lines = lines;
    }
}
