using Avalonia.Media;
using StencilPad.Common;
using StencilPad.Models;
using StencilPad.Services;
using StencilPad.Spatial;

namespace StencilPad.ViewModels.Properties;

public class ShapePropertiesViewModel : ElementPropertiesViewModel<Shape>
{
    public string Title => "Shape Properties";

    private Color _fillColor;
    public Color FillColor
    {
        get => _fillColor;
        set
        {
            _fillColor = value;
            SetElementProperty(s => s.FillColor = value);
            OnPropertyChanged();
        }
    }

    private Color _lineColor;
    public Color LineColor
    {
        get => _lineColor;
        set
        {
            _lineColor = value;
            SetElementProperty(s => s.LineColor = value);
            OnPropertyChanged();
        }
    }

    private Unit _lineWidth;
    public Unit LineWidth
    {
        get => _lineWidth;
        set
        {
            _lineWidth = value;
            SetElementProperty(s => s.LineWidth = value);
            OnPropertyChanged();
        }
    }

    private int _startCapIndex;
    public int StartCapIndex
    {
        get => _startCapIndex;
        set
        {
            _startCapIndex = value;
            SetElementProperty(s => s.StartCap = _capIds[value]);
            OnPropertyChanged();
        }
    }

    private int _endCapIndex;
    public int EndCapIndex
    {
        get => _endCapIndex;
        set
        {
            _endCapIndex = value;
            SetElementProperty(s => s.EndCap = _capIds[value]);
            OnPropertyChanged();
        }
    }

    private LineStyle _lineStyle;
    public LineStyle LineStyle
    {
        get => _lineStyle;
        set
        {
            _lineStyle = value;
            SetElementProperty(s => s.LineStyle = _lineStyle);
            OnPropertyChanged();
        }
    }
    
    public IReadOnlyList<GeometryResourceId> CapIds => _capIds;
    public IReadOnlyList<LineStyle> LineStyles => _lineStyles;

    private List<GeometryResourceId> _capIds;
    private List<LineStyle> _lineStyles;
    private IDisposable? _dragContext;
    
    public ShapePropertiesViewModel(Sheet sheet,
                                    ISettings settings,
                                    IResourceService resourceService,
                                    IOperationService operationService)
        : base(sheet, operationService, settings)
    {
        _capIds = [ GeometryResourceId.None ];
        _capIds.AddRange(resourceService.GetGeometryResourceIds(GeometryResourceType.Cap));

        _lineStyles = [];
        _lineStyles.AddRange(resourceService.GetLineStyles());

        OnElementsChanged();
    }

    public void DragBegin()
    {
        _dragContext = OperationService.CreateEditContext(Sheet, Elements);
    }

    public void DragEnd()
    {
        _dragContext?.Dispose();
    }
    
    protected override void OnElementsChanged()
    {
        _fillColor = Mode(shape => shape.FillColor);
        OnPropertyChanged(nameof(FillColor));

        _lineColor = Mode(shape => shape.LineColor);
        OnPropertyChanged(nameof(LineColor));

        _lineWidth = Mode(shape => shape.LineWidth);
        OnPropertyChanged(nameof(LineWidth));

        _startCapIndex = Mode(shape => _capIds.IndexOf(shape.StartCap));
        OnPropertyChanged(nameof(StartCapIndex));

        _endCapIndex = Mode(shape => _capIds.IndexOf(shape.EndCap));
        OnPropertyChanged(nameof(EndCapIndex));

        _lineStyle = Mode(shape => shape.LineStyle);
        OnPropertyChanged(nameof(LineStyle));
    }
}
