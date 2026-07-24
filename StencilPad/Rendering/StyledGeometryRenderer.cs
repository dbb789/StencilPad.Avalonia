using StencilPad.Common;
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
        public SKPath? FillPath;
        public SKPath? OutlinePath;
        public SKPath? OverlayPath;

        public void Dispose()
        {
            FillPath?.Dispose();
            FillPath = null;
            
            OutlinePath?.Dispose();
            OutlinePath = null;
            
            OverlayPath?.Dispose();
            OverlayPath = null;
        }
    }
    
    private class RenderedPaint : IDisposable
    {
        public SKPaint? FillPaint;
        public SKPaint? StrokePaint;

        public void Dispose()
        {
            FillPaint?.Dispose();
            FillPaint = null;
            
            StrokePaint?.Dispose();
            StrokePaint = null;
        }
    }

    private readonly IResourceSet _resourceSet;
    private readonly Dictionary<int, Entry> _entryMap;
    private ClampedGeometryWalker? _clampedGeometryWalker;
    private SKPathGeometryWalker? _pathGeometryWalker;

    private SharedDisposable<RenderedGeometry> _renderedGeometry;
    private SharedDisposable<RenderedPaint> _renderedPaint;

    public event Action? RendererDirty;
    
    public StyledGeometryRenderer(IResourceSet resourceSet)
    {
        _resourceSet = resourceSet;
        _entryMap = new();
        _renderedGeometry = new(new RenderedGeometry());
        _renderedPaint = new(new RenderedPaint());
    }

    public void Dispose()
    {
        _entryMap.Clear();
        
        _renderedGeometry.Dispose();
        _renderedPaint.Dispose();
    }
    
    public void Render(SKCanvas canvas)
    {
        using var geometryHandle = _renderedGeometry.Get();
        using var paintHandle = _renderedPaint.Get();
        
        var geometry = geometryHandle.Value;
        var paint = paintHandle.Value;
        
        DrawPath(canvas, geometry.FillPath, paint.FillPaint);
        DrawPath(canvas, geometry.FillPath, paint.StrokePaint);
        DrawPath(canvas, geometry.OutlinePath, paint.StrokePaint);
        DrawPath(canvas, geometry.OverlayPath, paint.FillPaint);
        DrawPath(canvas, geometry.OverlayPath, paint.StrokePaint);
    }
    
    public void SetStyle(GeometryStyle style)
    {
        _renderedPaint.SetValue(new RenderedPaint
        {
            FillPaint = CreateFillPaint(style),
            StrokePaint = CreateStrokePaint(style),
        });

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
        var renderedGeometry = new RenderedGeometry();

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
                renderedGeometry.OutlinePath ??= new();
                renderedGeometry.OutlinePath.AddPath(path);
            }

            foreach (var (resource, transform) in entry.GeometrySet.Overlays)
            {
                renderedGeometry.OverlayPath ??= new();
                renderedGeometry.OverlayPath.AddPath(resource.Path, transform.CreateMatrix());
            }
            
            path.Dispose();
        }

        if (fillBuilder is not null)
        {
            renderedGeometry.FillPath = new();
            fillBuilder.Resolve(renderedGeometry.FillPath);
            fillBuilder.Dispose();
        }

        _renderedGeometry.SetValue(renderedGeometry);
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
