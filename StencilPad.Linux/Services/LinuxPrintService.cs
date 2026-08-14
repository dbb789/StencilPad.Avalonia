using StencilPad.Export;
using StencilPad.Models;
using StencilPad.Services;
using Tmds.DBus;

namespace StencilPad.Linux.Services;

// Prints via the xdg-desktop-portal Print portal, which shows the desktop
// environment's own native print dialog (GTK's on GNOME, KDE's on Plasma,
// etc.) over D-Bus - Avalonia has no print pipeline of its own and there is
// no GTK dependency in this app to call into directly.
//
// The portal is a two-phase, asynchronous request/response protocol:
//   1. PreparePrint shows the settings/page-setup dialog and hands back a
//      "token" identifying the user's choices.
//   2. Print submits the actual document (passed as a Unix file descriptor)
//      against that token, without prompting again.
// Every portal method returns an opaque "Request" object path; the real
// result of the call arrives later via a Response signal on that object,
// wrapped here by RequestAsync.
public class LinuxPrintService : IPrintService
{
    private readonly PdfExporter _pdfExporter;

    public LinuxPrintService(PdfExporter pdfExporter)
    {
        _pdfExporter = pdfExporter;
    }

    public async Task<bool> PrintAsync(string documentName, Sheet sheet)
    {
        using var connection = new Connection(Address.Session!);
        await connection.ConnectAsync();

        var portal = connection.CreateProxy<IPrintPortal>(
            "org.freedesktop.portal.Desktop", "/org/freedesktop/portal/desktop");

        var (prepareResponse, prepareResults) = await RequestAsync(connection,
            portal.PreparePrintAsync("", documentName,
                new Dictionary<string, object>(),
                new Dictionary<string, object>(),
                new Dictionary<string, object>()));

        if (prepareResponse != 0)
        {
            // User cancelled the settings dialog.
            return false;
        }

        var token = (uint)prepareResults["token"];

        // NOTE: prepareResults also contains "settings" and "page-setup"
        // dictionaries describing the chosen printer/paper options. A more
        // complete implementation would honor those (paper size, orientation,
        // colour, etc.) when generating the PDF below; for now the PDF is
        // rendered using the Sheet's own fixed format via the same
        // PdfExporter used by File > Export > PDF.
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");

        try
        {
            _pdfExporter.Export(sheet, tempPath);

            await using var fileStream = File.OpenRead(tempPath);

            var (printResponse, _) = await RequestAsync(connection,
                portal.PrintAsync("", documentName,
                    new CloseSafeHandle(fileStream.SafeFileHandle.DangerousGetHandle(), ownsHandle: false),
                    new Dictionary<string, object> { ["token"] = token }));

            return printResponse == 0;
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    private static async Task<(uint Response, IDictionary<string, object> Results)> RequestAsync(
        Connection connection, Task<ObjectPath> call)
    {
        var tcs = new TaskCompletionSource<(uint, IDictionary<string, object>)>();

        var requestPath = await call;
        var request = connection.CreateProxy<IRequest>("org.freedesktop.portal.Desktop", requestPath);

        using var subscription = await request.WatchResponseAsync(result => tcs.TrySetResult(result));

        return await tcs.Task;
    }
}
