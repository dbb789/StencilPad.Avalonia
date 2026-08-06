namespace StencilPad.Models.Resolvers;

public interface IResourceSet
{
    GeometryResource Get(GeometryResourceId id);
}
