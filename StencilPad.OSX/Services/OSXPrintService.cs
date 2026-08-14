using PdfKit;
using StencilPad.Export;
using StencilPad.Models;
using StencilPad.Services;

namespace StencilPad.OSX.Services;

// Uses the native AppKit print pipeline (NSPrintOperation/NSPrintPanel via
// PDFKit) rather than shelling out to CUPS directly - this gives the same
// native "Print" sheet (printer picker, copies, page range, paper size,
// etc.) that macOS apps normally show, at the cost of only being buildable
// for net10.0-macos.
//
// The document itself is still produced by the shared PdfExporter (the
// same renderer used by File > Export > PDF and by LinuxPrintService), so
// there's no separate Skia/rendering code to maintain here - PDFKit just
// needs a PDFDocument to hand to NSPrintOperation, which already knows how
// to size/rotate pages and talks to CUPS under the hood on our behalf.
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

        try
        {
            _pdfExporter.Export(sheet, tempPath);

            using var document = new PdfDocument(NSUrl.FromFilename(tempPath));
            using var pdfView = new PdfView();
            
            pdfView.Document = document;

            using var printInfo = NSPrintInfo.SharedPrintInfo;

            pdfView.Print(printInfo, true, PdfPrintScalingMode.None);

            return Task.FromResult(true);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}
