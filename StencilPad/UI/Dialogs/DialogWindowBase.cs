using Avalonia.Controls;

namespace StencilPad.UI.Dialogs;

// NOTE: Avalonia's Window has no WPF-style bool? DialogResult property that
// OK/Cancel handlers can set before closing. This tiny base class gives the
// small modal dialogs in this folder an equivalent Result flag, read by
// WpfDialogService after ShowDialog() completes.
public abstract class DialogWindowBase : Window
{
    public bool Result { get; protected set; }
}
