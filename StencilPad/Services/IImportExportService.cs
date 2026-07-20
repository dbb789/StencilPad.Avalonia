using StencilPad.Models;
using StencilPad.Spatial;

namespace StencilPad.Services;

public interface IImportExportService
{
    Task ImportImageAsync(Sheet sheet, IViewport viewport);
    Task ExportSvgAsync(Sheet sheet);
    Task ExportPngAsync(Sheet sheet);
}
