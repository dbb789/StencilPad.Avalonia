using SkiaSharp;
using StencilPad.Models;
using StencilPad.Spatial;
using StencilPad.Rendering;

namespace StencilPad.Canvases.Tools.Overlays;

public class PolygonToolOverlayRenderer : IToolOverlayRenderer
{
    public static readonly IToolOverlayRendererFactory Factory = new FactoryImpl();
    
    private class FactoryImpl : IToolOverlayRendererFactory
    {
        public IToolOverlayRenderer? CreateOverlay(ISheetElement element)
        {
            if (element is IPolygonSheetElement polygonSheetElement)
            {
                return new PolygonToolOverlayRenderer(polygonSheetElement);
            }

            return null;
        }
    }

    private class RenderedGeometry : IDisposable
    {
        public SKPath EdgeOverlayPath = new();
        public SKPath ControlStemPath = new();
        public SKMatrix Matrix = SKMatrix.Identity;

        public void Reset()
        {
            EdgeOverlayPath.Reset();
            ControlStemPath.Reset();
        }

        public void Dispose()
        {
            EdgeOverlayPath.Dispose();
            ControlStemPath.Dispose();
        }
    }

    private static readonly SKPaint EdgeOverlayPaint = new SKPaint
    {
        Color = new SKColor(0, 0, 255, 128),
        StrokeWidth = 2,
        Style = SKPaintStyle.Stroke,
        IsAntialias = true,
        IsDither = true
    };

    private static readonly SKPaint ControlStemPaint = new SKPaint
    {
        Color = new SKColor(0, 200, 0, 128),
        StrokeWidth = 2,
        Style = SKPaintStyle.Stroke,
        IsAntialias = true,
        IsDither = true
    };

    private readonly IPolygonSheetElement _element;
    private readonly SKPathGeometryWalker _walker;

    private RenderBuffer<RenderedGeometry> _renderedGeometry;
    private bool _geometryDirty;
    
    public event Action? RendererDirty;

    private PolygonToolOverlayRenderer(IPolygonSheetElement element)
    {
        _element = element;
        _element.PolygonSet.PolygonAdded += PolygonAdded;
        _element.PolygonSet.PolygonRemoved += PolygonRemoved;
        _element.PolygonSet.HandleSource.HandleSelectionChanged += SelectionChanged;
        _element.TransformChanged += TransformChanged;
        
        _walker = new();
        _renderedGeometry = new();
        
        foreach (var polygon in _element.PolygonSet)
        {
            polygon.GeometryChanged += MarkGeometryDirty;
        }

        _geometryDirty = true;

        RebuildGeometry();
        _geometryDirty = false;
    }

    public void Dispose()
    {
        foreach (var polygon in _element.PolygonSet)
        {
            polygon.GeometryChanged -= MarkGeometryDirty;
        }
        
        _element.PolygonSet.PolygonAdded -= PolygonAdded;
        _element.PolygonSet.PolygonRemoved -= PolygonRemoved;
        _element.PolygonSet.HandleSource.HandleSelectionChanged -= SelectionChanged;
        _element.TransformChanged -= TransformChanged;

        _renderedGeometry.Dispose();
    }

    private void PolygonAdded(EditablePolygon polygon)
    {
        polygon.GeometryChanged += MarkGeometryDirty;
        MarkGeometryDirty();
    }

    private void PolygonRemoved(EditablePolygon polygon)
    {
        polygon.GeometryChanged -= MarkGeometryDirty;
        MarkGeometryDirty();
    }

    private void SelectionChanged(IHandleSource source, Handle handle, bool selected)
    {
        MarkGeometryDirty();
    }
    
    private void MarkGeometryDirty(IPolygon polygon)
    {
        MarkGeometryDirty();
    }

    private void MarkGeometryDirty()
    {
        _geometryDirty = true;

        InvokeRendererDirty();
    }

    private void TransformChanged(ISheetElement element)
    {
        MarkGeometryDirty();
    }

    public void PreRender()
    {
        if (_geometryDirty)
        {
            _geometryDirty = false;
            RebuildGeometry();
        }
    }

    public void Render(SKCanvas canvas, GRContext? context, SKMatrix transformMatrix)
    {
        using var geometryHandle = _renderedGeometry.TryRead();

        if (!geometryHandle.IsValid)
        {
            return;
        }

        var geometry = geometryHandle.Buffer;
        var matrix = SKMatrix.Concat(transformMatrix, geometry.Matrix);
        
        if (!geometry.EdgeOverlayPath.IsEmpty)
        {
            // FIXME: Allocation here.
            using var edgeOverlayPath = new SKPath(geometry.EdgeOverlayPath);

            edgeOverlayPath.Transform(matrix);
        
            canvas.DrawPath(edgeOverlayPath, EdgeOverlayPaint);
        }

        if (!geometry.ControlStemPath.IsEmpty)
        {
            using var controlStemPath = new SKPath(geometry.ControlStemPath);

            controlStemPath.Transform(matrix);
            
            canvas.DrawPath(controlStemPath, ControlStemPaint);
        }
    }

    private void RebuildGeometry()
    {
        using var geometryHandle = _renderedGeometry.TryWrite();

        if (!geometryHandle.IsValid)
        {
            return;
        }

        var geometry = geometryHandle.Buffer;

        geometry.Reset();
        geometry.Matrix = _element.Transform.CreateMatrix();

        var polygonList = _element.PolygonSet;

        _walker.Path = geometry.EdgeOverlayPath;

        foreach (var polygon in polygonList)
        {
            foreach (var edgeIndex in polygon.GetSelectedEdges())
            {
                polygon.Resolver.WalkEdge(_walker, edgeIndex);
            }
        }

        foreach (var polygon in polygonList)
        {
            for (int i = 0; i < polygon.Edges.Count; i++)
            {
                var edge = polygon.Edges[i];

                if (edge.Type == EdgeType.Bezier)
                {
                    var vertexBegin = polygon.Vertices[i].Position;
                    var controlBegin = vertexBegin + edge.ControlBeginOffset;

                    geometry.ControlStemPath.MoveTo(Point(vertexBegin));
                    geometry.ControlStemPath.LineTo(Point(controlBegin));

                    var vertexEnd = polygon.Vertices.At(i + 1).Position;
                    var controlEnd = vertexEnd + edge.ControlEndOffset;

                    geometry.ControlStemPath.MoveTo(Point(vertexEnd));
                    geometry.ControlStemPath.LineTo(Point(controlEnd));
                }
            }
        }
    }

    private static SKPoint Point(Unit2D point)
    {
        return new SKPoint((float)point.X.Millimeters, (float)point.Y.Millimeters);
    }
    
    private void InvokeRendererDirty()
    {
        RendererDirty?.Invoke();
    }
}
