namespace StencilPad.Rendering;

public class TripleBuffer<T> : IDisposable where T : class, IDisposable, new()
{
    public ref struct WriteContext : IDisposable
    {
        public T Buffer => _buffer;
        
        private TripleBuffer<T> _parent;
        private T _buffer;

        public WriteContext(TripleBuffer<T> parent)
        {
            _parent = parent;
            _buffer = _parent.EnterWriteScope();
        }

        public void Dispose()
        {
            _parent.ExitWriteScope();
        }
    }
    
    public ref struct ReadContext : IDisposable
    {
        public T Buffer => _buffer;
        
        private TripleBuffer<T> _parent;
        private T _buffer;

        public ReadContext(TripleBuffer<T> parent)
        {
            _parent = parent;
            _buffer = _parent.EnterReadScope();
        }
        
        public void Dispose()
        {
            _parent.ExitReadScope();
        }
    }

    private T _write;
    private T _pending;
    private T _read;

    private readonly object _writeLock;
    private readonly object _pendingLock;
    private readonly object _readLock;

    private bool _writing;
    private bool _reading;
    private bool _pendingDirty;

    public TripleBuffer()
    {
        _write = new T();
        _pending = new T();
        _read = new T();

        _writeLock = new object();
        _pendingLock = new object();
        _readLock = new object();
    }

    public void Dispose()
    {
        lock (_writeLock)
        {
            lock (_readLock)
            {
                lock (_pendingLock)
                {
                    _write.Dispose();
                    _pending.Dispose();
                    _read.Dispose();
                }
            }
        }
    }

    public WriteContext Write()
    {
        return new WriteContext(this);
    }

    public ReadContext Read()
    {
        return new ReadContext(this);
    }

    private T EnterWriteScope()
    {
        Monitor.Enter(_writeLock);

        if (_writing)
        {
            Monitor.Exit(_writeLock);
            throw new InvalidOperationException("Cannot enter write scope while already writing.");
        }

        _writing = true;
        
        return _write;
    }

    private void ExitWriteScope()
    {
        try
        {
            lock (_pendingLock)
            {
                (_pending, _write) = (_write, _pending);
                _pendingDirty = true;
            }
            
            _writing = false;
        }
        finally
        {
            Monitor.Exit(_writeLock);
        }
    }

    private T EnterReadScope()
    {
        Monitor.Enter(_readLock);
        
        if (_reading)
        {
            Monitor.Exit(_readLock);
            throw new InvalidOperationException("Cannot enter read scope while already reading.");
        }
        
        _reading = true;
        
        lock (_pendingLock)
        {
            if (_pendingDirty)
            {
                (_pending, _read) = (_read, _pending);
                _pendingDirty = false;
            }
        }

        return _read;
    }

    private void ExitReadScope()
    {
        try
        {
            _reading = false;
        }
        finally
        {
            Monitor.Exit(_readLock);
        }
    }
}
