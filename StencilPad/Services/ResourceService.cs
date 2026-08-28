using Microsoft.Extensions.Logging;
using Avalonia.Media;
using SkiaSharp;
using StencilPad.Models;
using StencilPad.Rendering;
using StencilPad.Schemas;
using StencilPad.Spatial;

namespace StencilPad.Services;

public class ResourceService : IResourceService
{
    private readonly ILogger<ResourceService> _logger;
    private readonly Dictionary<GeometryResourceId, GeometryResource> _geometryMap;
    private readonly Dictionary<GeometryResourceType, List<GeometryResourceId>> _byType;

    public ResourceService(ILogger<ResourceService> logger)
    {
        _logger = logger;
        _geometryMap = [];
        _byType = [];

        try
        {
            LoadGeometry();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to load geometry resources.");
        }
    }

    public IEnumerable<GeometryResourceId> GetGeometryResourceIds(GeometryResourceType type)
    {
        if (_byType.TryGetValue(type, out var list))
        {
            return list;
        }

        return Enumerable.Empty<GeometryResourceId>();
    }

    public IEnumerable<LineStyle> GetLineStyles()
    {
        return LineStyleResourceLibrary.ResourceList;
    }

    private void LoadGeometry()
    {
        foreach (var entry in GeometryResourceLibrary.Load())
        {
            Load(entry.Id, entry.Filename, entry.Size);

            List<GeometryResourceId> list;
            
            if (!_byType.TryGetValue(entry.Type, out list!))
            {
                list = [];
                _byType[entry.Type] = list;
            }

            list.Add(entry.Id);
        }        
    }

    private void Load(GeometryResourceId id, string filename, Unit2D? size)
    {
        SKPath? path = null;
        Shape? shape = null;
        Unit2D geometrySize = Unit2D.Zero;
        
        try
        {
            (path, shape, geometrySize) = LoadGeometry(filename);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to load geometry resource '{id}' from file '{filename}'.");
            return;
        }
        
        if (path is not null)
        {
            _geometryMap[id] = new GeometryResource(path, shape ?? new Shape(), size ?? geometrySize);
        }
        else
        {
            _logger.LogError("Failed to load geometry resource '{Id}'.", id);
        }
    }
    
    public GeometryResource Get(GeometryResourceId id)
    {
        if (id == GeometryResourceId.None)
        {
            return GeometryResource.Empty;
        }
        
        if (_geometryMap.TryGetValue(id, out var geometry))
        {
            return geometry;
        }

        return GeometryResource.Empty;
    }

    // NOTE: Throws a variety of exceptions on failure.
    private (SKPath, Shape?, Unit2D) LoadGeometry(string filename)
    {
        var schema = SchemaUtil.LoadProject(filename);

        Project project = new();

        ProjectSchema.Unpack(schema, project);

        var sheet = project.Sheets.First();

        var geometry = new StreamGeometry();

        Shape? shape = null;
        UnitBounds? bounds = null;

        var path = new SKPath();

        shape = sheet.Elements.FirstOrDefault(e => e is Shape) as Shape;
        
        if (shape is not null)
        {
            var walker = new SKPathGeometryWalker();
            var builder = new SKPath.OpBuilder();

            foreach (var polygon in shape.PolygonSet)
            {
                var subPath = new SKPath();
                
                polygon.Transform(shape.Transform);
                bounds = UnitBounds.Union(bounds, polygon.CalculateBounds());

                walker.Path = subPath;
                polygon.Resolver.Walk(walker);

                builder.Add(subPath, SKPathOp.Xor);
            }

            builder.Resolve(path);
            
            shape.Transform = UnitTransform.Identity;
        }

        return (path, shape, bounds?.Size ?? Unit2D.Zero);
    }
}
