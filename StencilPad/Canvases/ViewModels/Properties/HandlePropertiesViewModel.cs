using StencilPad.Canvases.Common;
using StencilPad.Common;
using StencilPad.Models;
using StencilPad.Services;
using StencilPad.Spatial;
using StencilPad.ViewModels;

namespace StencilPad.Canvases.ViewModels.Properties;

public class HandlePropertiesViewModel : ViewModelBase, IDisposable
{
    public string Title => "Handle Properties";

    public UnitSettings UnitSettings => _settings.UnitSettings;

    private bool _hasHandles;
    public bool HasHandles
    {
        get => _hasHandles;
        private set
        {
            _hasHandles = value;
            OnPropertyChanged();
        }
    }

    private Unit? _x;
    public Unit? X
    {
        get => _x;
        set
        {
            if (_x == value)
            {
                return;
            }
            
            _x = value;

            if (_x is not null)
            {
                Unit xValue = _x.Value;
                
                SetSelectedHandlePositions(entry => new Unit2D(xValue, entry.Position.Y));
            }

            OnPropertyChanged();
        }
    }

    private Unit? _y;
    public Unit? Y
    {
        get => _y;
        set
        {
            if (_y == value)
            {
                return;
            }

            _y = value;

            if (_y is not null)
            {
                Unit yValue = _y.Value;

                SetSelectedHandlePositions(entry => new Unit2D(entry.Position.X, yValue));
            }

            OnPropertyChanged();
        }
    }

    private readonly Sheet _sheet;
    private readonly IHandleMap _handleMap;
    private readonly IOperationService _operationService;
    private readonly ISettings _settings;

    public HandlePropertiesViewModel(Sheet sheet,
                                     IHandleMap handleMap,
                                     IOperationService operationService,
                                     ISettings settings)
    {
        _sheet = sheet;
        _handleMap = handleMap;
        _operationService = operationService;
        _settings = settings;

        _handleMap.HandleSelectionChanged += OnHandlesChanged;
        _handleMap.HandleMoved += OnHandleMoved;
        _handleMap.HandleRemoved += OnHandleRemoved;

        UpdatePosition();
    }

    public void Dispose()
    {
        _handleMap.HandleSelectionChanged -= OnHandlesChanged;
        _handleMap.HandleMoved -= OnHandleMoved;
        _handleMap.HandleRemoved -= OnHandleRemoved;
    }

    private void OnHandlesChanged()
    {
        UpdatePosition();
    }

    private void OnHandleMoved(ISheetElement element, Handle handle, Unit2D position)
    {
        UpdatePosition();
    }

    private void OnHandleRemoved(ISheetElement element, Handle handle)
    {
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        var selectedHandles = _handleMap.SelectedHandles;

        HasHandles = selectedHandles.Count > 0;

        _x = All(selectedHandles, entry => entry.Position.X);
        OnPropertyChanged(nameof(X));

        _y = All(selectedHandles, entry => entry.Position.Y);
        OnPropertyChanged(nameof(Y));
    }

    private void SetSelectedHandlePositions(Func<IHandleMapEntry, Unit2D> newPosition)
    {
        var selectedHandles = _handleMap.SelectedHandles;

        if (selectedHandles.Count == 0)
        {
            return;
        }

        var elements = selectedHandles.Select(e => e.Element).Distinct();

        using var context = _operationService.TryCreateEditContext(_sheet, elements);

        foreach (var entry in selectedHandles)
        {
            entry.SetPosition(newPosition(entry));
        }
    }

    private static Unit? All(IEnumerable<IHandleMapEntry> entries, Func<IHandleMapEntry, Unit> selector)
    {
        Unit? first = null;

        foreach (var entry in entries)
        {
            var value = selector(entry);

            if (first is null)
            {
                first = value;
            }
            else if (!first.Value.ApproximatelyEquals(value))
            {
                return null;
            }
        }

        return first;
    }
}
