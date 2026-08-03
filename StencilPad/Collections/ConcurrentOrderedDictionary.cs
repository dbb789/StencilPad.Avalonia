namespace StencilPad.Collections;

public class ConcurrentOrderedDictionary<TKey, TValue> where TKey : notnull
{
    private readonly OrderedDictionary<TKey, TValue> _data;
    private readonly object _lock;

    private readonly ConcurrentList<TValue> _orderedValues;
    
    public ConcurrentOrderedDictionary()
    {
        _data = new();
        _lock = new();
        _orderedValues = new();
    }
    
    public bool TryGetValue(TKey key, out TValue value)
    {
        lock (_lock)
        {
            return _data.TryGetValue(key, out value!);
        }
    }

    public void Add(TKey key, TValue value)
    {
        lock (_lock)
        {
            _data.Add(key, value);
            _orderedValues.Add(value);
        }
    }

    public void Insert(int index, TKey key, TValue value)
    {
        lock (_lock)
        {
            _data.Insert(index, key, value);
            _orderedValues.Insert(index, value);
        }
    }

    public bool Remove(TKey key)
    {
        lock (_lock)
        {
            int index = _data.IndexOf(key);

            if (index >= 0)
            {
                _data.RemoveAt(index);
                _orderedValues.RemoveAt(index);

                return true;
            }

            return false;
        }
    }

    public bool TryGetRemove(TKey key, out TValue value)
    {
        lock (_lock)
        {
            int index = _data.IndexOf(key);

            if (index >= 0)
            {
                value = _data.GetAt(index).Value;
                _data.RemoveAt(index);
                _orderedValues.RemoveAt(index);
                
                return true;
            }

            value = default!;
            
            return false;
        }
    }

    public IEnumerable<(TKey, TValue)> GetClear()
    {
        lock (_lock)
        {
            var list = new List<(TKey, TValue)>(_data.Count);

            foreach (var kvp in _data)
            {
                list.Add((kvp.Key, kvp.Value));
            }

            _data.Clear();

            return list;
        }
    }
    
    public void Move(int oldIndex, int newIndex)
    {
        lock (_lock)
        {
            var kvp = _data.GetAt(oldIndex);

            _data.RemoveAt(oldIndex);
            _data.Insert(newIndex, kvp.Key, kvp.Value);
            _orderedValues.Move(oldIndex, newIndex);
        }
    }

    public ConcurrentList<TValue>.Enumerator GetEnumerator()
    {
        return _orderedValues.GetEnumerator();
    }
}
