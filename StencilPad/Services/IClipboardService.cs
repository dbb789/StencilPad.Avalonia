using StencilPad.Models;

namespace StencilPad.Services;

public interface IClipboardService
{
    Task Copy(Sheet sheet);
    Task Cut(Sheet sheet);
    Task Paste(Sheet sheet);
}
