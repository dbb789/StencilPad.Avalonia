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
                using var pdfView = new PdfView();

                pdfView.Document = document;

                using var firstPage = document.GetPage(0);

                if (firstPage is null)
                {
                    throw new InvalidOperationException("PDF document has no pages.");
                }

                pdfView.AutoScales = false;
                pdfView.ScaleFactor = 1.0f;
                pdfView.PageShadowsEnabled = false;
                pdfView.BackgroundColor = NSColor.White;

                var pageBounds = firstPage.GetBoundsForBox(PdfDisplayBox.Media);
                pdfView.Frame = new CGRect(CGPoint.Empty, pageBounds.Size);

                using var printInfo = NSPrintInfo.SharedPrintInfo;

                using var printOperation = NSPrintOperation.FromView(pdfView, printInfo);

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
