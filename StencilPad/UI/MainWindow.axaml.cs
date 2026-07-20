using StencilPad.ViewModels;

namespace StencilPad.UI;

public partial class MainWindow : Avalonia.Controls.Window, IWpfDialogParent
{
    public Avalonia.Controls.Window Window => this;
    
    public MainWindow()
    {
        InitializeComponent();
    }

    // NOTE: WPF's drag-and-drop based tab reordering (TabItemDrag/TabItemDrop,
    // wired via PreviewMouseMove/Drop EventSetters in the TabItem style) relied
    // on WPF's DragDrop.DoDragDrop API. Reimplementing drag-to-reorder tabs in
    // Avalonia is a real UI feature, not a mechanical port, and reordering tabs
    // by dragging is a minor convenience rather than core functionality - so
    // it's stubbed out for now. MainWindowViewModel.SheetTabReordered and the
    // underlying ReorderSheetOperation are still wired up and functional; they
    // just have no UI gesture driving them yet in this Avalonia port.
}
