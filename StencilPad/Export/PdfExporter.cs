using SkiaSharp;
using StencilPad.Models;
using StencilPad.Models.Resolvers;
using StencilPad.Rendering;

namespace StencilPad.Export;

public class PdfExporter
{
    private readonly IResourceSet _resourceSet;
    private readonly SheetResolver.Factory _sheetResolverFactory;

    public PdfExporter(IResourceSet resourceSet,
                       SheetResolver.Factory sheetResolverFactory)
    {
        _resourceSet = resourceSet;
        _sheetResolverFactory = sheetResolverFactory;
    }

    public void Export(Sheet sheet, string path)
    {
        const double MmPerInch = 25.4;
        const double PointsPerInch = 72.0;
        
        using var document = SKDocument.CreatePdf(path);
        
        var format = sheet.Format;
        
        float width = (float)(format.Size.X.Millimeters * PointsPerInch / MmPerInch);
        float height = (float)(format.Size.Y.Millimeters * PointsPerInch / MmPerInch);
        
        using var canvas = document.BeginPage(width, height);
        {
            float scale = (float)(PointsPerInch / MmPerInch);
            
            var matrix = SKMatrix.CreateTranslation(width / 2, height / 2);
            
            matrix = SKMatrix.Concat(matrix, SKMatrix.CreateScale(scale, -scale));
            
            canvas.Save();
            canvas.SetMatrix(SKMatrix.Concat(canvas.TotalMatrix, matrix));
            
            using var resolver = _sheetResolverFactory.Create(sheet);
            
            foreach (var elementResolver in resolver.Elements)
            {
                using var renderer = new ModelRenderer(_resourceSet);
                
                elementResolver.Attach(renderer);
                renderer.PreRender();
                renderer.Render(canvas, null);
            }
            
            canvas.Restore();
        }
        
        document.EndPage();
        document.Close();
    }
}
