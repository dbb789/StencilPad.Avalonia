using PdfKit;
using StencilPad.Export;
using StencilPad.Models;
using StencilPad.Services;

namespace StencilPad.OSX.Services;

public class OSXPrintService : IPrintService
{
    private readonly PdfExporter _pdfExporter;

    public OSXPrintService(PdfExporter pdfExporter)
    {
        _pdfExporter = pdfExporter;
    }

    public Task<bool> PrintAsync(string documentName, Sheet sheet)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");

        NSApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            try
            {
                _pdfExporter.Export(sheet, tempPath);

                using var document = new PdfDocument(NSUrl.FromFilename(tempPath));

                using var printInfo = NSPrintInfo.SharedPrintInfo;
                using var printOperation = document.GetPrintOperation(printInfo, PdfPrintScalingMode.None, true);

                if (printOperation is null)
                {
                    throw new InvalidOperationException("Failed to create print operation.");
                }
                
                printOperation.ShowsPrintPanel = true;
                printOperation.ShowsProgressPanel = true;

                printOperation.RunOperation();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error printing document: {e.Message}");
            }
            finally
            {
                File.Delete(tempPath);
            }
        });

        return Task.FromResult(true);
    }
}
