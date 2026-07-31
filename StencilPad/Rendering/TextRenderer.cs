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
        public SKPoint Point = new SKPoint(0, 0);
        public SKFont Font = new();
        public SKTextAlign Align = SKTextAlign.Left;
        public SKPaint Paint = new();
        public string [] Lines = Array.Empty<string>();

        public void Reset()
        {
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
    
    private SKMatrix? _matrix;
    private TextStyle _style;
    private UnitBounds? _bounds;
    private string _text;
    
    private TripleBuffer<RenderedText> _renderedText;

    public event Action? RendererDirty;
    
    public TextRenderer()
    {
        _style = new TextStyle();
        _text = "";
        _renderedText = new();
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

    public void Render(SKCanvas canvas, GRContext? context)
    {
        canvas.Save();
        
        if (_matrix is not null)
        {
            canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, _matrix.Value));
        }

        canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, SKMatrix.CreateScale(1, -1)));

        using var textHandle = _renderedText.Read();

        var text = textHandle.Buffer;
        var point = text.Point;
        
        foreach (var line in text.Lines)
        {
            point.Y += text.Font.Size;
            canvas.DrawText(line, point, text.Align, text.Font, text.Paint);
        }

        canvas.Restore();
    }

    private void InvokeRendererDirty()
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

        using var textHandle = _renderedText.Write();

        textHandle.Buffer.Reset();
        textHandle.Buffer.Point = point;
        textHandle.Buffer.Font = font;
        textHandle.Buffer.Align = align;
        textHandle.Buffer.Paint = paint;
        textHandle.Buffer.Lines = lines;
        
        RendererDirty?.Invoke();
    }
}
