#if SP_OSX

using AppKit;
using Foundation;
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
        // AppKit UI (NSPrintOperation/NSPrintPanel) must run on the main
        // thread; Avalonia's macOS backend already pumps the NSApplication
        // run loop from its UI-thread dispatcher, so this assumes PrintAsync
        // is invoked from there (as it is for the other platform services).
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");

        try
        {
            _pdfExporter.Export(sheet, tempPath);

            using var document = new PDFDocument(NSUrl.FromFilename(tempPath));

            // An offscreen PDFView is the simplest way to obtain a correctly
            // configured NSPrintOperation for a PDFDocument - it derives
            // page size/orientation directly from the PDF's own MediaBox,
            // so no manual paper-size/rotation logic is needed here (unlike
            // WindowsPrintService, which has to reconcile sheet orientation
            // against the printer's printable area by hand).
            using var pdfView = new PDFView();
            pdfView.Document = document;

            using var printInfo = NSPrintInfo.SharedPrintInfo;
            printInfo.JobTitle = documentName;

            using var printOperation = pdfView.GetPrintOperation(printInfo, autoRotate: true, pageScaling: PDFPrintScalingMode.PageScaleNone);

            printOperation.ShowsPrintPanel = true;
            printOperation.ShowsProgressPanel = true;

            var succeeded = printOperation.RunOperation();

            return Task.FromResult(succeeded);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}

#endif
