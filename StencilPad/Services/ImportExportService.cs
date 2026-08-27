using Avalonia.Platform.Storage;
using SkiaSharp;
using StencilPad.Export;
using StencilPad.Models;
using StencilPad.Models.Operations;
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
    
    private static readonly FilePickerFileType PdfFileType = new("PDF")
    {
        Patterns = ["*.pdf"]
    };

    private static readonly FilePickerFileType ImageFileType = new("Image")
    {
        Patterns = ["*.png", "*.jpg", "*.jpeg"]
    };

    private readonly Avalonia.Controls.Window _owner;
    private readonly IDialogService _dialogService;
    private readonly IOperationService _operationService;
    private readonly PngExporter _pngExporter;
    private readonly SvgExporter _svgExporter;
    private readonly PdfExporter _pdfExporter;

    public ImportExportService(IAvaloniaDialogParent parent,
                               IDialogService dialogService,
                               IOperationService operationService,
                               PngExporter pngExporter,
                               SvgExporter svgExporter,
                               PdfExporter pdfExporter)
    {
        _owner = parent.Window;
        _dialogService = dialogService;
        _operationService = operationService;
        _pngExporter = pngExporter;
        _svgExporter = svgExporter;
        _pdfExporter = pdfExporter;
    }
    
    public async Task ImportImageAsync(Sheet sheet, IViewport viewport)
    {
        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Image",
            AllowMultiple = false,
            FileTypeFilter = [ImageFileType]
        });

        var file = files.FirstOrDefault();
        var path = file?.TryGetLocalPath();

        if (path is null)
        {
            return;
        }

        try
        {
            var imageData = await File.ReadAllBytesAsync(path);
            var bounds = UnitBounds.FromCenterSize(Unit2D.Zero, MeasureImageSize(imageData));
            var imageElement = new ImageElement(bounds.Min, bounds.Max, imageData);

            _operationService.Push(new AddSheetElementOperation(sheet, imageElement));
        }
        catch (Exception e)
        {
            await _dialogService.ShowErrorAsync($"Failed to import image: {e.Message}", "Import Error");
        }
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
    
    public async Task ExportPdfAsync(Sheet sheet)
    {
        var file = await _owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export PDF",
            SuggestedFileName = sheet.Name,
            DefaultExtension = "pdf",
            FileTypeChoices = [PdfFileType]
        });

        var path = file?.TryGetLocalPath();

        if (path is not null)
        {
            _pdfExporter.Export(sheet, path);
        }
    }

    private static Unit2D MeasureImageSize(byte [] imageData)
    {
        var bitmap = SKBitmap.Decode(imageData);

        double widthMm = bitmap.Width * 25.4 / 240.0;
        double heightMm = bitmap.Height * 25.4 / 240.0;

        return new Unit2D(Unit.FromMillimeters(widthMm),
                          Unit.FromMillimeters(heightMm));
    }
}
