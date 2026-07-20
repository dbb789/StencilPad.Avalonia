namespace StencilPad.Spatial;

public class EmptyGeometryResolver :  IGeometryResolver
{
    public static readonly EmptyGeometryResolver Instance = new();
    
    private EmptyGeometryResolver()
    {
        // ...
    }
    
    public void Walk(IGeometryWalker walker)
    {
        if (walker.Begin(0, false))
        {
            walker.End();
        }
    }
    
    public void WalkReverse(IGeometryWalker walker)
    {
        if (walker.Begin(0, false))
        {
            walker.End();
        }
    }
    
    public void WalkEdge(IGeometryWalker walker, int edgeIndex)
    {
        // ...
    }
}
