namespace StencilPad.Common;

public class SharedDisposable<T> where T : class, IDisposable
{
    public interface IHandle : IDisposable
    {
        T Value { get; }
    }
    
    private class Handle : IHandle
    {
        public T Value => _parent.GetValue();
        
        private readonly SharedDisposable<T> _parent;
        
        public Handle(SharedDisposable<T> parent)
        {
            _parent = parent;
        }
        
        public void Dispose()
        {
            _parent.Release(this);
        }
    }

    private T? _pendingValue;
    private T _value;
    private int _handleCount;
    private readonly Stack<Handle> _handles;
    private readonly object _lock;
    
    public SharedDisposable(T value)
    {
        _value = value;
        _handleCount = 0;
        _handles = new();
        _lock = new();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _value?.Dispose();
            _value = default!;
            _pendingValue?.Dispose();
            _pendingValue = default;
            _handleCount = 0;
        }
    }
    
    public void SetValue(T value)
    {
        lock (_lock)
        {
            if (_handleCount == 0)
            {
                _value?.Dispose();
                _value = value;
            }
            else
            {
                _pendingValue = value;
            }
        }
    }

    public IHandle Get()
    {
        lock (_lock)
        {
            ++_handleCount;

            if (_handles.Count > 0)
            {
                return _handles.Pop();
            }
            
            return new Handle(this);
        }
    }

    private void Release(Handle handle)
    {
        lock (_lock)
        {
            --_handleCount;
            _handles.Push(handle);
            
            if (_handleCount == 0 && _pendingValue is not null)
            {
                _value?.Dispose();
                _value = _pendingValue;
                _pendingValue = null;
            }
            else if (_handleCount < 0)
            {
                throw new InvalidOperationException("Handle count cannot be negative.");
            }
        }
    }

    private T GetValue()
    {
        lock (_lock)
        {
            return _value;
        }
    }
}
