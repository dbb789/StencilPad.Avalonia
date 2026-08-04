using StencilPad.Collections;

namespace StencilPad.Rendering;

public class TripleBuffer<T> : IDisposable where T : class, IDisposable, new()
{
    public ref struct WriteContext : IDisposable
    {
        public T Buffer => _buffer;
        public bool IsValid => _parent is not null;
        
        private TripleBuffer<T>? _parent;
        private T _buffer;

        public WriteContext(TripleBuffer<T>? parent, T buffer)
        {
            _parent = parent;
            _buffer = buffer;
        }

        public void Dispose()
        {
            _parent?.ExitWriteScope();
            _parent = null;
        }
    }
    
    public ref struct ReadContext : IDisposable
    {
        public T Buffer => _buffer;
        public bool IsValid => _parent is not null;

        private TripleBuffer<T>? _parent;
        private T _buffer;

        public ReadContext(TripleBuffer<T>? parent, T buffer)
        {
            _parent = parent;
            _buffer = buffer;
        }
        
        public void Dispose()
        {
            _parent?.ExitReadScope();
            _parent = null;
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
    
    private AtomicBool _disposed;

    public TripleBuffer()
    {
        _write = new T();
        _pending = new T();
        _read = new T();

        _writeLock = new object();
        _pendingLock = new object();
        _readLock = new object();

        _disposed = new(false);
    }

    public void Dispose()
    {
        lock (_writeLock)
        {
            lock (_readLock)
            {
                lock (_pendingLock)
                {
                    if (_disposed.Swap(true))
                    {
                        return;
                    }
                    
                    _write.Dispose();
                    _pending.Dispose();
                    _read.Dispose();
                }
            }
        }
    }
    
    public WriteContext TryWrite()
    {
        var buffer = TryEnterWriteScope();

        if (buffer is null)
        {
            return new WriteContext(null, default!);
        }

        return new WriteContext(this, buffer);
    }

    public ReadContext TryRead()
    {
        var buffer = TryEnterReadScope();

        if (buffer is null)
        {
            return new ReadContext(null, default!);
        }

        return new ReadContext(this, buffer);
    }
    
    private T? TryEnterWriteScope()
    {
        Monitor.Enter(_writeLock);

        if (_disposed.Value)
        {
            Monitor.Exit(_writeLock);
            return null;
        }
        
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

    private T? TryEnterReadScope()
    {
        Monitor.Enter(_readLock);
        
        if (_disposed.Value)
        {
            Monitor.Exit(_readLock);
            return null;
        }

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
