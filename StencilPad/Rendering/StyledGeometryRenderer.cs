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

    private readonly IResourceSet _resourceSet;
    private readonly Dictionary<int, Entry> _entryMap;
    private ClampedGeometryWalker? _clampedGeometryWalker;
    private SKPathGeometryWalker? _pathGeometryWalker;
    
    private SKPath? _fillPath;
    private SKPath? _outlinePath;
    private SKPath? _overlayPath;
    private SKPaint? _fillPaint;
    private SKPaint? _strokePaint;

    public event Action? RendererDirty;
    
    public StyledGeometryRenderer(IResourceSet resourceSet)
    {
        _resourceSet = resourceSet;
        _entryMap = new();
    }

    public void Dispose()
    {
        _entryMap.Clear();
        
        _fillPath?.Dispose();
        _outlinePath?.Dispose();
        _overlayPath?.Dispose();
        _fillPaint?.Dispose();
        _strokePaint?.Dispose();
    }
    
    public void Render(SKCanvas canvas)
    {
        DrawPath(canvas, _fillPath, _fillPaint);
        DrawPath(canvas, _fillPath, _strokePaint);
        DrawPath(canvas, _outlinePath, _strokePaint);
        DrawPath(canvas, _overlayPath, _fillPaint);
        DrawPath(canvas, _overlayPath, _strokePaint);
    }
    
    public void SetStyle(GeometryStyle style)
    {
        _strokePaint = CreateStrokePaint(style);
        _fillPaint = CreateFillPaint(style);

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
        _fillPath?.Dispose();
        _outlinePath?.Dispose();
        _overlayPath?.Dispose();
        
        using var fillBuilder = new SKPath.OpBuilder();

        _outlinePath = new SKPath();
        _overlayPath = new SKPath();
        
        foreach (var (_, entry) in _entryMap)
        {
            var (path, closed) = CreatePath(entry.GeometrySet);

            if (closed)
            {
                fillBuilder.Add(path, SKPathOp.Xor);
            }
            else
            {
                _outlinePath.AddPath(path);
            }

            foreach (var (resource, transform) in entry.GeometrySet.Overlays)
            {
                _overlayPath.AddPath(resource.Path, transform.CreateMatrix());
            }
            
            path.Dispose();
        }

        _fillPath = new SKPath();
        fillBuilder.Resolve(_fillPath);
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

    private SKPaint? CreateStrokePaint(GeometryStyle style)
    {
        if (style.LineColor.A == 0 || style.LineWidth.Millimeters <= 0)
        {
            return null;
        }
        
        var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = new SKColor(style.LineColor.R,
                                style.LineColor.G,
                                style.LineColor.B,
                                style.LineColor.A),
            StrokeWidth = (float)style.LineWidth.Millimeters,
            IsAntialias = true
        };

        return paint;
    }

    private SKPaint? CreateFillPaint(GeometryStyle style)
    {
        if (style.FillColor.A == 0)
        {
            return null;
        }

        var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = new SKColor(style.FillColor.R,
                                style.FillColor.G,
                                style.FillColor.B,
                                style.FillColor.A),
            IsAntialias = true
        };

        return paint;
    }

    private void DrawPath(SKCanvas canvas, SKPath? path, SKPaint? paint)
    {
        if (paint is null || path is null)
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
