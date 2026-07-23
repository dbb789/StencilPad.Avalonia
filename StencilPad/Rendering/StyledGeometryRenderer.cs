using Avalonia.Media;
using StencilPad.Models.Resolvers;
using StencilPad.Spatial;
using SkiaSharp;

namespace StencilPad.Rendering;

public class StyledGeometryRenderer : IStyledGeometryWalker, IWalkerRenderer
{
    private class Entry
    {
        public GeometrySet GeometrySet;

        ////////////////////
        
        public SKPath? Path;
        public List<(SKPath, SKMatrix)> Overlays { get; } = [];
        
        public bool GeometryDirty;
    }

    private readonly IResourceSet _resourceSet;
    private readonly Dictionary<int, Entry> _entryMap;
    private ClampedGeometryWalker? _clampedGeometryWalker;
    private SKPathGeometryWalker? _pathGeometryWalker;
    
    private GeometryGroup? _baseGeometry;
    private bool _geometryDirty;
    private SKPaint? _fillPaint;
    private SKPaint? _strokePaint;

    public event Action? RendererDirty;
    
    public StyledGeometryRenderer(IResourceSet resourceSet)
    {
        _resourceSet = resourceSet;
        _entryMap = new();
        _geometryDirty = true;
    }

    public void Dispose()
    {
        // ...
    }
    
    public void Render(SKCanvas canvas)
    {
        var path = GetCombinedPath();

        if (_fillPaint is not null)
        {
            canvas.DrawPath(path, _fillPaint);
        }

        if (_strokePaint is not null)
        {
            canvas.DrawPath(path, _strokePaint);
        }

        foreach (var (_, entry) in _entryMap)
        {
            foreach (var (overlayPath, overlayTransform) in entry.Overlays)
            {
                canvas.Save();
                canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, overlayTransform));

                if (_fillPaint is not null)
                {
                    canvas.DrawPath(overlayPath, _fillPaint);
                }
                
                if (_strokePaint is not null)
                {
                    canvas.DrawPath(overlayPath, _strokePaint);
                }
               
                canvas.Restore();
            }
        }
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
            Path = null,
            GeometryDirty = true,
        };

        _geometryDirty = true;
        
        InvokeRendererDirty();
    }

    public void Update(int id, GeometrySet geometry)
    {
        if (!_entryMap.TryGetValue(id, out var entry))
        {
            return;
        }
        
        entry.GeometrySet = geometry;
        entry.GeometryDirty = true;

        _geometryDirty = true;

        InvokeRendererDirty();
    }

    public void Destroy(int id)
    {
        _entryMap.Remove(id);
        _geometryDirty = true;

        InvokeRendererDirty();
    }

    private SKPath GetCombinedPath()
    {
        var builder = new SKPath.OpBuilder();

        foreach (var (_, entry) in _entryMap)
        {
            builder.Add(GetPath(entry), SKPathOp.Xor);
        }
        
        var path = new SKPath();

        builder.Resolve(path);

        return path;
    }

    private SKPath GetPath(Entry entry)
    {
        if (!entry.GeometryDirty && entry.Path is not null)
        {
            return entry.Path;
        }

        entry.GeometryDirty = false;
        
        var path = new SKPath();

        _pathGeometryWalker ??= new();
        _pathGeometryWalker.Path = path;
        
        if (entry.GeometrySet.StartPoint is not null ||
            entry.GeometrySet.EndPoint is not null)
        {
            _clampedGeometryWalker ??= new ClampedGeometryWalker(_pathGeometryWalker);
            _clampedGeometryWalker.SetStartEnd(entry.GeometrySet.StartPoint,
                                               entry.GeometrySet.EndPoint);
            
            entry.GeometrySet.Resolver.Walk(_clampedGeometryWalker);
        }
        else
        {
            entry.GeometrySet.Resolver.Walk(_pathGeometryWalker);
        }

        entry.Path = path;
        entry.Overlays.Clear();
        
        foreach (var (resource, overlayTransform) in entry.GeometrySet.Overlays)
        {
            entry.Overlays.Add((resource.Path, overlayTransform.CreateMatrix()));
        }

        return path;
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
    
    private void InvokeRendererDirty()
    {
        RendererDirty?.Invoke();
    }
}
