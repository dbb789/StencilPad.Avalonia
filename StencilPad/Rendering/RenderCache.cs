namespace StencilPad.Rendering;

public class RenderCache<T> : IDisposable where T : class, IDisposable, new()
{
    public ref struct Context : IDisposable
    {
        public T Buffer => _buffer;
        public bool IsValid => _parent is not null;

        private RenderCache<T>? _parent;
        private T _buffer;

        public Context(RenderCache<T>? parent, T buffer)
        {
            _parent = parent;
            _buffer = buffer;
        }

        public void Dispose()
        {
            _parent?.ExitUpdateScope();
            _parent = null;
            _buffer = null!;
        }
    }
    
    private readonly T _buffer;
    private readonly object _lock;
    private bool _updating;
    private bool _disposed;

    public RenderCache()
    {
        _buffer = new();
        _lock = new();
        _disposed = false;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _buffer.Dispose();
        }
    }

    public Context TryUpdate()
    {
        var buffer = TryEnterUpdateScope();

        if (buffer is null)
        {
            return new Context(null, null!);
        }

        return new Context(this, buffer);
    }

    private T? TryEnterUpdateScope()
    {
        Monitor.Enter(_lock);

        if (_disposed)
        {
            Monitor.Exit(_lock);
            return null;
        }
        
        if (_updating)
        {
            Monitor.Exit(_lock);
            throw new InvalidOperationException("Cannot enter update scope while already updating.");
        }

        _updating = true;
        
        return _buffer;
    }

    private void ExitUpdateScope()
    {
        try
        {
            _updating = false;
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }
}
