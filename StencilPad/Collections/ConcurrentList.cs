using System.Collections.Immutable;
namespace StencilPad.Collections;

public class ConcurrentList<T>
{
    private ImmutableList<T> _list;

    public ConcurrentList()
    {
        _list = ImmutableList<T>.Empty;
    }

    public void Add(T item)
    {
        ImmutableInterlocked.Update(ref _list, list => list.Add(item));
    }

    public void Remove(T item)
    {
        ImmutableInterlocked.Update(ref _list, list => list.Remove(item));
    }

    public void Clear()
    {
        ImmutableInterlocked.Update(ref _list, list => list.Clear());
    }
    
    public List<T> ToList()
    {
        return _list.ToList();
    }
    
    public ImmutableList<T>.Enumerator GetEnumerator()
    {
        return _list.GetEnumerator();
    }
}
