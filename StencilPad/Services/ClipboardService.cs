using System.Text.Json;
using Microsoft.Extensions.Logging;
using Avalonia.Input;
using Avalonia.Input.Platform;
using StencilPad.Models;
using StencilPad.Models.Operations;
using StencilPad.Schemas;
using StencilPad.Spatial;
using StencilPad.UI;

namespace StencilPad.Services;

public class ClipboardService : IClipboardService
{
    private static readonly DataFormat<string> ClipboardDataFormat = DataFormat.CreateStringApplicationFormat("stencilpad.data");
    private static readonly Unit2D PasteMajorOffset = Unit2D.FromMillimeters(-5, -5);
    private static readonly Unit2D PasteMinorOffset = Unit2D.FromMillimeters(5, -5);

    private readonly ILogger<ClipboardService> _logger;
    private readonly IClipboard? _clipboard;
    private readonly IOperationService _operationService;
    private int _pasteCounter;

    public ClipboardService(ILogger<ClipboardService> logger,
                            IAvaloniaDialogParent dialogParent,
                            IOperationService operationService)
    {
        _logger = logger;
        _clipboard = dialogParent.Window.Clipboard;
        _operationService = operationService;
    }

    public async Task Copy(Sheet sheet)
    {
        if (_clipboard is null)
        {
            _logger.LogWarning("Clipboard is not available.");
            return;
        }
        
        _pasteCounter = 0;

        await PackToClipboard(sheet, sheet.Selection);
    }

    public async Task Cut(Sheet sheet)
    {
        if (_clipboard is null)
        {
            _logger.LogWarning("Clipboard is not available.");
            return;
        }
        
        await Copy(sheet);

        var operations = sheet.Selection
            .Select(e => new RemoveSheetElementOperation(sheet, e));

        _operationService.Push(new BulkCommandOperation(operations));
    }
    
    public async Task Paste(Sheet sheet)
    {
        var elements = await UnpackFromClipboard();

        if (!elements.Any())
        {
            return;
        }

        ++_pasteCounter;
        
        var pasteOffset = PasteMajorOffset * (_pasteCounter / 10);
        
        pasteOffset += PasteMinorOffset * (_pasteCounter % 10);

        foreach (var element in elements)
        {
            element.Transform = element.Transform with
                { Position = element.Transform.Position + pasteOffset };
        }

        var operations = elements
            .Select(e => new AddSheetElementOperation(sheet, e));

        _operationService.Push(new BulkCommandOperation(operations));

        sheet.Selection.Clear();
        
        foreach (var element in elements)
        {
            sheet.Selection.Add(element);
        }
    }

    private async Task PackToClipboard(Sheet sheet, IEnumerable<ISheetElement> elements)
    {
        if (_clipboard is null)
        {
            return;
        }
        
        // Pack in render order to preserve z-index when pasting.
        var schemas = elements.OrderBy(e => sheet.Elements.IndexOf(e))
            .Select(SheetElementSchema.Pack)
            .Where(s => s is not null)
            .ToArray();

        var data = new DataTransfer();
        var item = new DataTransferItem();
        
        item.Set(ClipboardDataFormat, JsonSerializer.Serialize(schemas, SchemaJsonOptions.Default));

        data.Add(item);
        
        await _clipboard.SetDataAsync(data);
    }
    
    private async Task<List<ISheetElement>?> UnpackFromClipboard()
    {
        if (_clipboard is null)
        {
            _logger.LogWarning("Clipboard is not available.");

            return null;
        }
        
        var data = await _clipboard.TryGetValueAsync(ClipboardDataFormat);

        if (string.IsNullOrWhiteSpace(data))
        {
            return null;
        }

        SheetElementSchema[]? schemas;

        try
        {
            schemas = JsonSerializer.Deserialize<SheetElementSchema[]>(data, SchemaJsonOptions.Default);
        }
        catch (JsonException je)
        {
            _logger.LogError("Failed to deserialize clipboard content: {Exception}", je);

            return null;
        }

        if (schemas is null)
        {
            return null;
        }

        return schemas.Select(s => s.Unpack()).ToList();
    }
}
