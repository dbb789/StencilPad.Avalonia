using System.IO;
using System.Printing;
using System.Windows.Controls;
using System.Windows.Xps.Packaging;
using SkiaSharp;
using StencilPad.Models;
using StencilPad.Models.Resolvers;
using StencilPad.Rendering;
using StencilPad.Services;

namespace StencilPad.Windows.Services;

public class WindowsPrintService : IPrintService
{
    private readonly IResourceSet _resourceSet;
    private readonly SheetResolver.Factory _sheetResolverFactory;

    public WindowsPrintService(IResourceSet resourceSet,
                               SheetResolver.Factory sheetResolverFactory)
    {
        _resourceSet = resourceSet;
        _sheetResolverFactory = sheetResolverFactory;
    }
    
    public Task<bool> PrintAsync(string documentName, Sheet sheet)
    {
        var printDialog = new PrintDialog();

        if (printDialog.ShowDialog() == true)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xps");
            {
                const double MmPerInch = 25.4;
                const double PointsPerInch = 72.0;
                
                using var document = SKDocument.CreateXps(tempPath);

                var format = sheet.Format;

                var sheetIsLandscape = format.Orientation == SheetOrientation.Landscape;
                var pageIsLandscape = printDialog.PrintableAreaWidth > printDialog.PrintableAreaHeight;
                var rotate = sheetIsLandscape != pageIsLandscape;
                
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
                
            using var xpsDoc = new XpsDocument(tempPath, FileAccess.Read);
         
            var fixedDocSeq = xpsDoc.GetFixedDocumentSequence();

            if (fixedDocSeq is null)
            {
                return Task.FromResult(false);
            }

            var writer = PrintQueue.CreateXpsDocumentWriter(printDialog.PrintQueue);

            writer.Write(fixedDocSeq, printDialog.PrintTicket);
        }
    
        return Task.FromResult(true);
    }
}
