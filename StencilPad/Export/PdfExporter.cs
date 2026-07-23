using StencilPad.Models;
using StencilPad.Models.Resolvers;

namespace StencilPad.Export;

public class PdfExporter
{
    private readonly SheetResolver.Factory _sheetResolverFactory;

    public PdfExporter(SheetResolver.Factory sheetResolverFactory)
    {
        _sheetResolverFactory = sheetResolverFactory;
    }

    public void Export(Sheet sheet, string path)
    {
        
    }
}
