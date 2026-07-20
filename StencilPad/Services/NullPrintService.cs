using StencilPad.Models;

namespace StencilPad.Services;

// NOTE: The real printing integration (System.Windows.Controls.PrintDialog /
// System.Printing-based in the WPF app) was deliberately not ported yet -
// Avalonia has no built-in print pipeline. Stubbed to report failure so
// callers can show an appropriate message instead of crashing.
public class NullPrintService : IPrintService
{
    public Task<bool> PrintAsync(string documentName, Sheet sheet)
    {
        return Task.FromResult(false);
    }
}
