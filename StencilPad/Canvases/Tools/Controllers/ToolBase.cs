using CommunityToolkit.Mvvm.Input;

namespace StencilPad.Canvases.Tools.Controllers;

public abstract class ToolBase : ITool
{
    private static readonly RelayCommand DisabledCommand = new (() => { }, () => false);
    
    public virtual IRelayCommand SelectAllCommand => DisabledCommand;
    public virtual IRelayCommand ClearSelectionCommand => DisabledCommand;
    public virtual IRelayCommand CopyCommand => DisabledCommand;
    public virtual IRelayCommand CutCommand => DisabledCommand;
    public virtual IRelayCommand PasteCommand => DisabledCommand;
    public virtual IRelayCommand DeleteCommand => DisabledCommand;

    public virtual void Dispose()
    {
        // ...
    }
    
    public abstract void ToolBegin();
    public abstract void ToolEnd();
}
