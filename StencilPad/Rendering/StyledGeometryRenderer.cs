using StencilPad.Models.Resolvers;
using StencilPad.Spatial;
using SkiaSharp;

namespace StencilPad.Rendering;

public class StyledGeometryRenderer : IStyledGeometryWalker, IWalkerRenderer
{
    private class Entry
    {
        public GeometrySet GeometrySet;
    }
    
    private class RenderedGeometry : IDisposable
    {
        public SKPath FillPath = new();
        public SKPath OutlinePath = new();
        public SKPath OverlayPath = new();

        private bool _disposed;
        
        public void Reset()
        {
            FillPath.Reset();
            OutlinePath.Reset();
            OverlayPath.Reset();
        }

        public void Dispose()
        {
            FillPath.Dispose();            
            OutlinePath.Dispose();            
            OverlayPath.Dispose();
        }
    }
    
    private class RenderedPaint : IDisposable
    {
        public SKPaint FillPaint = new();
        public SKPaint StrokePaint = new();

        public void Reset()
        {
            FillPaint.Reset();
            StrokePaint.Reset();
        }
        
        public void Dispose()
        {
            FillPaint.Dispose();
            StrokePaint.Dispose();
        }
    }

    private readonly IResourceSet _resourceSet;
    private readonly Dictionary<int, Entry> _entryMap;
    private ClampedGeometryWalker? _clampedGeometryWalker;
    private SKPathGeometryWalker? _pathGeometryWalker;

    private TripleBuffer<RenderedGeometry> _renderedGeometry;
    private TripleBuffer<RenderedPaint> _renderedPaint;

    public event Action? RendererDirty;
    
    public StyledGeometryRenderer(IResourceSet resourceSet)
    {
        _resourceSet = resourceSet;
        _entryMap = new();
        _renderedGeometry = new();
        _renderedPaint = new();
    }

    public void Dispose()
    {
        _entryMap.Clear();
        
        _renderedGeometry.Dispose();
        _renderedPaint.Dispose();
    }
    
    public void Render(SKCanvas canvas, GRContext? context)
    {
        using var geometryHandle = _renderedGeometry.Read();
        using var paintHandle = _renderedPaint.Read();
        
        var geometry = geometryHandle.Buffer;
        var paint = paintHandle.Buffer;
        
        DrawPath(canvas, geometry.FillPath, paint.FillPaint);
        DrawPath(canvas, geometry.FillPath, paint.StrokePaint);
        DrawPath(canvas, geometry.OutlinePath, paint.StrokePaint);
        DrawPath(canvas, geometry.OverlayPath, paint.FillPaint);
        DrawPath(canvas, geometry.OverlayPath, paint.StrokePaint);
    }
    
    public void SetStyle(GeometryStyle style)
    {
        using var paintHandle = _renderedPaint.Write();

        CreateFillPaint(style, paintHandle.Buffer.FillPaint);
        CreateStrokePaint(style, paintHandle.Buffer.StrokePaint);

        InvokeRendererDirty();
    }
    
    public void Create(int id, GeometrySet geometry)
    {
        _entryMap[id] = new Entry
        {
            GeometrySet = geometry,
        };

        RebuildGeometry();
        InvokeRendererDirty();
    }

    public void Update(int id, GeometrySet geometry)
    {
        if (!_entryMap.TryGetValue(id, out var entry))
        {
            return;
        }
        
        entry.GeometrySet = geometry;

        RebuildGeometry();
        InvokeRendererDirty();
    }

    public void Destroy(int id)
    {
        _entryMap.Remove(id);

        RebuildGeometry();
        InvokeRendererDirty();
    }

    private void RebuildGeometry()
    {
        using var geometryHandle = _renderedGeometry.Write();
        var geometry = geometryHandle.Buffer;

        geometry.Reset();
        
        SKPath.OpBuilder? fillBuilder = null;

        foreach (var (_, entry) in _entryMap)
        {
            var (path, closed) = CreatePath(entry.GeometrySet);

            if (closed)
            {
                fillBuilder ??= new();
                fillBuilder.Add(path, SKPathOp.Xor);
            }
            else
            {
                geometry.OutlinePath.AddPath(path);
            }

            foreach (var (resource, transform) in entry.GeometrySet.Overlays)
            {
                geometry.OverlayPath.AddPath(resource.Path, transform.CreateMatrix());
            }
            
            path.Dispose();
        }

        if (fillBuilder is not null)
        {
            fillBuilder.Resolve(geometry.FillPath);
            fillBuilder.Dispose();
        }
    }

    private (SKPath, bool) CreatePath(GeometrySet geometrySet)
    {
        var path = new SKPath();

        _pathGeometryWalker ??= new();
        _pathGeometryWalker.Path = path;
        
        if (geometrySet.StartPoint is not null ||
            geometrySet.EndPoint is not null)
        {
            _clampedGeometryWalker ??= new ClampedGeometryWalker(_pathGeometryWalker);
            _clampedGeometryWalker.SetStartEnd(geometrySet.StartPoint,
                                               geometrySet.EndPoint);
            
            geometrySet.Resolver.Walk(_clampedGeometryWalker);
        }
        else
        {
            geometrySet.Resolver.Walk(_pathGeometryWalker);
        }

        return (path, _pathGeometryWalker.Closed);
    }

    private void CreateStrokePaint(GeometryStyle style, SKPaint paint)
    {
        paint.Style = SKPaintStyle.Stroke;
        paint.Color = new SKColor(style.LineColor.R,
                                  style.LineColor.G,
                                  style.LineColor.B,
                                  style.LineColor.A);
        paint.StrokeWidth = (float)style.LineWidth.Millimeters;
        paint.IsAntialias = true;
        paint.IsDither = true;
    }

    private void CreateFillPaint(GeometryStyle style, SKPaint paint)
    {
        paint.Style = SKPaintStyle.Fill;
        paint.Color = new SKColor(style.FillColor.R,
                                  style.FillColor.G,
                                  style.FillColor.B,
                                  style.FillColor.A);
        paint.IsAntialias = true;
        paint.IsDither = true;
    }

    private void DrawPath(SKCanvas canvas, SKPath path, SKPaint paint)
    {
        if (paint.Color.Alpha == 0 || path.IsEmpty)
        {
            return;
        }

        canvas.DrawPath(path, paint);
    }
    
    private void InvokeRendererDirty()
    {
        RendererDirty?.Invoke();
    }
}
