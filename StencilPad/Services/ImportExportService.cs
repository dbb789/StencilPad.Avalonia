using Avalonia.Platform.Storage;
using StencilPad.Export;
using StencilPad.Models;
using StencilPad.Spatial;
using StencilPad.UI;

namespace StencilPad.Services;

public class ImportExportService : IImportExportService
{
    private static readonly FilePickerFileType SvgFileType = new("SVG")
    {
        Patterns = ["*.svg"]
    };
    
    private static readonly FilePickerFileType PngFileType = new("PNG")
    {
        Patterns = ["*.png"]
    };

    private readonly Avalonia.Controls.Window _owner;
    private readonly IDialogService _dialogService;
    private readonly IOperationService _operationService;
    private readonly PngExporter _pngExporter;
    private readonly SvgExporter _svgExporter;

    public ImportExportService(IAvaloniaDialogParent parent,
                               IDialogService dialogService,
                               IOperationService operationService,
                               PngExporter pngExporter,
                               SvgExporter svgExporter)
    {
        _owner = parent.Window;
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
    
    public async Task ExportSvgAsync(Sheet sheet)
    {
        var file = await _owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export SVG",
            SuggestedFileName = sheet.Name,
            DefaultExtension = "svg",
            FileTypeChoices = [SvgFileType]
        });

        var path = file?.TryGetLocalPath();

        if (path is not null)
        {
            _svgExporter.Export(sheet, path);
        }
    }

    public async Task ExportPngAsync(Sheet sheet)
    {
        var file = await _owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export PNG",
            SuggestedFileName = sheet.Name,
            DefaultExtension = "png",
            FileTypeChoices = [PngFileType]
        });

        var path = file?.TryGetLocalPath();

        if (path is not null)
        {
            _pngExporter.Export(sheet, path);
        }
    }

    private static Unit2D MeasureImageSize(byte[] imageData, double maxMm = 150.0)
    {
        throw new NotImplementedException(
            "Image size measurement needs porting to Avalonia.Media.Imaging.Bitmap.");
    }
}
