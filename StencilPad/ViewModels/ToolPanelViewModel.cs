using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;

namespace StencilPad.ViewModels;

public class ToolPanelViewModel : ViewModelBase
{
    public ObservableCollection<ToolViewModel> Tools { get; }

    public IRelayCommand SelectToolCommand { get; set; } = null!;

    private ToolViewModel? _selectedTool;
    public ToolViewModel? SelectedTool
    {
        get => _selectedTool;
        set => SetProperty(ref _selectedTool, value);
    }
    
    public ToolPanelViewModel()
    {
        Tools = [];

        SelectToolCommand = new RelayCommand<ToolViewModel>(SelectTool);
    }

    private void SelectTool(ToolViewModel? tool)
    {
        SelectedTool = tool;
    }
}
