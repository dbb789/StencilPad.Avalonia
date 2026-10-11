using System.ComponentModel;
using System.Runtime.CompilerServices;
using StencilPad.Collections;
using StencilPad.Spatial;

namespace StencilPad.Models;

public class Project : INotifyPropertyChanged
{
    public ObservableKeyedList<Guid, Sheet> Sheets => _sheets;
    public IEnumerable<ISheetElement> DefaultElements => _defaultElements.Values;

    private ObservableKeyedList<Guid, Sheet> _sheets;
    private Dictionary<Type, ISheetElement> _defaultElements;

    public event PropertyChangedEventHandler? PropertyChanged;

    private UnitSystem _unitSystem = UnitSystem.Metric;
    public UnitSystem UnitSystem
    {
        get => _unitSystem;
        set
        {
            if (_unitSystem != value)
            {
                _unitSystem = value;
                OnPropertyChanged();
            }
        }
    }

    private Fraction _unitRatio = Fraction.One;
    public Fraction UnitRatio
    {
        get => _unitRatio;
        set
        {
            if (_unitRatio != value)
            {
                _unitRatio = value;
                OnPropertyChanged();
            }
        }
    }

    public UnitSettings UnitSettings => new(UnitSystem, UnitRatio);
    
    private Unit _gridSpacingMetric = Unit.FromMillimeters(10);
    public Unit GridSpacingMetric
    {
        get => _gridSpacingMetric;
        set
        {
            if (_gridSpacingMetric != value)
            {
                _gridSpacingMetric = value;
                OnPropertyChanged();
            }
        }
    }

    private int _gridSubdivisionsMetric = 5;
    public int GridSubdivisionsMetric
    {
        get => _gridSubdivisionsMetric;
        set
        {
            if (_gridSubdivisionsMetric != value)
            {
                _gridSubdivisionsMetric = value;
                OnPropertyChanged();
            }
        }
    }

    private Unit _gridSpacingImperial = Unit.FromInches(1);
    public Unit GridSpacingImperial
    {
        get => _gridSpacingImperial;
        set
        {
            if (_gridSpacingImperial != value)
            {
                _gridSpacingImperial = value;
                OnPropertyChanged();
            }
        }
    }

    private int _gridSubdivisionsImperial = 8;
    public int GridSubdivisionsImperial
    {
        get => _gridSubdivisionsImperial;
        set
        {
            if (_gridSubdivisionsImperial != value)
            {
                _gridSubdivisionsImperial = value;
                OnPropertyChanged();
            }
        }
    }
    
    public Project()
    {
        _sheets = [];
        _defaultElements = [];
    }

    public void Clear()
    {
        _sheets.Clear();
        _defaultElements.Clear();
    }
    
    public void GetElementStyle<T>(T target) where T : class, ISheetElement, new()
    {
        if (!_defaultElements.TryGetValue(typeof(T), out var stored))
        {
            stored = new T();
            _defaultElements[typeof(T)] = stored;
        }
        
        target.AssignStyleFromElement(stored);
    }

    public void SetElementStyle<T>(T source) where T : class, ISheetElement, new()
    {
        if (!_defaultElements.TryGetValue(typeof(T), out var stored))
        {
            stored = new T();
            _defaultElements[typeof(T)] = stored;
        }

        stored.AssignStyleFromElement(source);
    }

    public void SetElementStyle(ISheetElement source)
    {
        var type = source.GetType();
        
        if (!_defaultElements.TryGetValue(type, out var stored))
        {
            stored = CreateDefault(source);
            
            _defaultElements[type] = stored;
        }

        stored.AssignStyleFromElement(source);
    }

    // Reflection-free factory (keeps the type AOT/trim safe) mirroring the
    // concrete ISheetElement types handled by SheetElementSchema.Pack.
    private static ISheetElement CreateDefault(ISheetElement source)
    {
        return source switch
        {
            GroupElement => new GroupElement(),
            Shape => new Shape(),
            MarkerPath => new MarkerPath(),
            Ruler => new Ruler(),
            TextElement => new TextElement(),
            ImageElement => new ImageElement(),
            _ => throw new InvalidOperationException(
                $"Could not create an instance of type {source.GetType().FullName}")
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
