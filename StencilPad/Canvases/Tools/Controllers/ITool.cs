using CommunityToolkit.Mvvm.Input;

namespace StencilPad.Canvases.Tools.Controllers;

public interface ITool : IDisposable
{
    IRelayCommand SelectAllCommand { get; }
    IRelayCommand ClearSelectionCommand { get; }
    IRelayCommand CopyCommand { get; }
    IRelayCommand CutCommand { get; }
    IRelayCommand PasteCommand { get; }
    IRelayCommand DeleteCommand { get; }

    void ToolBegin();
    void ToolEnd();
}
