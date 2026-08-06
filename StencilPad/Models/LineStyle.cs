using System.Runtime.CompilerServices;
using StencilPad.Spatial;

using Avalonia.Collections;

namespace StencilPad.Models;

public record struct LineStyle
{
    public static readonly LineStyle Solid = new();

    [InlineArray(4)]
    public struct Element
    {
        private Unit Value;
    }

    public bool IsSolid => (_count == 0);

    private int _count;
    private Element _elements;

    public LineStyle(params Unit[] elements)
    {
        if (elements.Length > 4)
        {
            throw new ArgumentException("LineStyle can have a maximum of 4 elements.");
        }
        
        _count = elements.Length;
        _elements = new Element();

        for (int i = 0; i < _count; ++i)
        {
            _elements[i] = elements[i];
        }
    }

    public Unit [] ToArray()
    {
        var result = new Unit[_count];
        
        for (int i = 0; i < _count; ++i)
        {
            result[i] = _elements[i];
        }
        
        return result;
    }
    
    public AvaloniaList<double> AvaloniaDashPattern
    {
        get
        {
            var result = new AvaloniaList<double>();

            for (int i = 0; i < _count; ++i)
            {
                result.Add(_elements[i].Millimeters);
            }
            
            return result;
        }
    }

    public float [] ToDashPattern()
    {
        var result = new float[_count];
        
        for (int i = 0; i < _count; ++i)
        {
            result[i] = (float)_elements[i].Millimeters;
        }
        
        return result;
    }
    
    public bool Equals(LineStyle other)
    {
        if (_count != other._count)
        {
            return false;
        }

        for (int i = 0; i < _count; ++i)
        {
            if (_elements[i] != other._elements[i])
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        int hash = 0;

        for (int i = 0; i < _count; ++i)
        {
            hash = HashCode.Combine(hash, _elements[i]);
        }

        return hash;
    }
}
