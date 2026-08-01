namespace StencilPad.Collections;

public class AtomicBool
{
    public bool Value
    {
        get
        {
            return Volatile.Read(ref _value);
        }
        set
        {
            Volatile.Write(ref _value, value);
        }
    }
    
    private bool _value;

    public AtomicBool(bool initialValue = false)
    {
        _value = initialValue;
    }

    public bool Swap(bool newValue)
    {
        return Interlocked.Exchange(ref _value, newValue);
    }
}
