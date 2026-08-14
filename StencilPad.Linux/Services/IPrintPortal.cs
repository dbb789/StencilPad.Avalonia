using Tmds.DBus;

namespace StencilPad.Linux.Services;

// D-Bus proxy interfaces for the xdg-desktop-portal Print portal
// (org.freedesktop.portal.Print). See:
// https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Print.html
[DBusInterface("org.freedesktop.portal.Print")]
internal interface IPrintPortal : IDBusObject
{
    Task<ObjectPath> PreparePrintAsync(string parentWindow, string title,
        IDictionary<string, object> settings,
        IDictionary<string, object> pageSetup,
        IDictionary<string, object> options);

    Task<ObjectPath> PrintAsync(string parentWindow, string title,
        CloseSafeHandle fd,
        IDictionary<string, object> options);
}

// org.freedesktop.portal.Request - every portal method call returns an
// object path implementing this interface; the actual result of the call
// arrives asynchronously via the Response signal on that object.
[DBusInterface("org.freedesktop.portal.Request")]
internal interface IRequest : IDBusObject
{
    Task<IDisposable> WatchResponseAsync(
        Action<(uint response, IDictionary<string, object> results)> handler);
}
