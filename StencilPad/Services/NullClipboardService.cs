using StencilPad.Models;

namespace StencilPad.Services;

// NOTE: The real clipboard integration (System.Windows.Clipboard-based in the
// WPF app) was deliberately not ported yet - it needs Avalonia's
// TopLevel.Clipboard API which requires threading a window/TopLevel reference
// into this service. Stubbed as a no-op so the app can start; copy/cut/paste
// commands are wired but currently do nothing.
public class NullClipboardService : IClipboardService
{
    public void Copy(Sheet sheet)
    {
    }

    public void Cut(Sheet sheet)
    {
    }

    public void Paste(Sheet sheet)
    {
    }
}
