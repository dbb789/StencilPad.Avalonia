using System.IO;
using StencilPad.Models;
using StencilPad.Models.Operations;
using StencilPad.Spatial;
using StencilPad.Export;

namespace StencilPad.Services;

// NOTE: File-open/save dialogs and image loading here were WPF-only
// (Microsoft.Win32 dialogs, BitmapImage) and have been stubbed out rather than
// silently ported wrong. Avalonia needs the async TopLevel.StorageProvider API
// for file pickers (which requires a window/TopLevel reference not currently
// threaded through this service) and Avalonia.Media.Imaging.Bitmap for image
// metrics. This needs a proper redesign, not a mechanical swap.
public class ImportExportService : IImportExportService
{
    private readonly IDialogService _dialogService;
    private readonly IOperationService _operationService;
    private readonly PngExporter _pngExporter;
    private readonly SvgExporter _svgExporter;

    public ImportExportService(IDialogService dialogService,
                               IOperationService operationService,
                               PngExporter pngExporter,
                               SvgExporter svgExporter)
    {
        _dialogService = dialogService;
        _operationService = operationService;
        _pngExporter = pngExporter;
        _svgExporter = svgExporter;
    }
    
    public Task ImportImageAsync(Sheet sheet, IViewport viewport)
    {
        // TODO: Port to Avalonia's TopLevel.StorageProvider.OpenFilePickerAsync
        // and Avalonia.Media.Imaging.Bitmap for measuring image size.
        return _dialogService.ShowErrorAsync("Image import is not yet implemented on this platform.", "Not Implemented");
    }
    
    public Task ExportSvgAsync(Sheet sheet)
    {
        // TODO: Port to Avalonia's TopLevel.StorageProvider.SaveFilePickerAsync.
        return _dialogService.ShowErrorAsync("SVG export is not yet implemented on this platform.", "Not Implemented");
    }

    public Task ExportPngAsync(Sheet sheet)
    {
        // TODO: Port to Avalonia's TopLevel.StorageProvider.SaveFilePickerAsync.
        return _dialogService.ShowErrorAsync("PNG export is not yet implemented on this platform.", "Not Implemented");
    }

    private static Unit2D MeasureImageSize(byte[] imageData, double maxMm = 150.0)
    {
        throw new NotImplementedException(
            "Image size measurement needs porting to Avalonia.Media.Imaging.Bitmap.");
    }
}
