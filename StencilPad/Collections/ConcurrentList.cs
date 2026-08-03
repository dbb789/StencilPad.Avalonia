using System.Collections.Immutable;
namespace StencilPad.Collections;

public class ConcurrentList<T>
{
    public struct Enumerator
    {
        public T Current => _enumerator.Current;
        
        private ImmutableList<T>.Enumerator _enumerator;
        
        public Enumerator(ImmutableList<T>.Enumerator enumerator)
        {
            _enumerator = enumerator;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
            _enumerator.Reset();
        }

        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }

    private ImmutableList<T> _list;

    public ConcurrentList()
    {
        _list = ImmutableList<T>.Empty;
    }

    public void Add(T item)
    {
        ImmutableInterlocked.Update(ref _list, list => list.Add(item));
    }

    public void Insert(int index, T item)
    {
        ImmutableInterlocked.Update(ref _list, list => list.Insert(index, item));
    }
    
    public void Remove(T item)
    {
        ImmutableInterlocked.Update(ref _list, list => list.Remove(item));
    }

    public void RemoveAt(int index)
    {
        ImmutableInterlocked.Update(ref _list, list => list.RemoveAt(index));
    }

    public void Move(int oldIndex, int newIndex)
    {
        ImmutableInterlocked.Update(ref _list, (list, indices) =>
            list.RemoveAt(indices.oldIndex).Insert(indices.newIndex, list[indices.oldIndex]),
            (oldIndex, newIndex));
    }
    
    public void Clear()
    {
        ImmutableInterlocked.Update(ref _list, list => list.Clear());
    }
    
    public List<T> ToList()
    {
        return _list.ToList();
    }
    
    public Enumerator GetEnumerator()
    {
        return new Enumerator(_list.GetEnumerator());
    }
}
