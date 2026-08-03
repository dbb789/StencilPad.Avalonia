using SkiaSharp;

namespace StencilPad.Rendering;

public class ConcurrentSKMatrix
{
    public SKMatrix Value
    {
        get
        {
            lock (_lock)
            {
                return _matrix;
            }
        }
        set
        {
            lock (_lock)
            {
                _matrix = value;
            }
        }
    }
    
    private SKMatrix _matrix;
    private readonly object _lock;

    public ConcurrentSKMatrix()
    {
        _matrix = SKMatrix.CreateIdentity();
        _lock = new();
    }

    public ConcurrentSKMatrix(SKMatrix matrix)
    {
        _matrix = matrix;
        _lock = new();
    }
}
