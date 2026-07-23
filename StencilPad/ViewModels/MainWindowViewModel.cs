using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using StencilPad.Models;

namespace StencilPad.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<SheetTabViewModel> Tabs { get; } = new();

    private Project? _project = null;
    public Project? Project
    {
        get => _project;
        set => SetProperty(ref _project, value);
    }

    private SheetTabViewModel? _selectedTab;
    public SheetTabViewModel? SelectedTab
    {
        get => _selectedTab;
        set => SetProperty(ref _selectedTab, value);
    }

    private string _title = "StencilPad";
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public IRelayCommand NewProjectCommand { get; set; } = null!;
    public IRelayCommand GridSettingsCommand { get; set; } = null!;
    public IRelayCommand UnitScaleCommand { get; set; } = null!;
    public IRelayCommand AddSheetCommand { get; set; } = null!;
    public IRelayCommand RenameSheetCommand { get; set; } = null!;
    public IRelayCommand DeleteSheetCommand { get; set; } = null!;
    public IRelayCommand PrintCommand { get; set; } = null!;
    public IRelayCommand ExitCommand { get; set; } = null!;
    public IRelayCommand OpenProjectCommand { get; set; } = null!;
    public IRelayCommand SaveProjectCommand { get; set; } = null!;
    public IRelayCommand SaveProjectAsCommand { get; set; } = null!;
    public IRelayCommand UndoCommand { get; set; } = null!;
    public IRelayCommand RedoCommand { get; set; } = null!;
    public IRelayCommand ImportImageCommand { get; set; } = null!;
    public IRelayCommand ExportSvgCommand { get; set; } = null!;
    public IRelayCommand ExportPngCommand { get; set; } = null!;
    public Action<int, int>? SheetTabReordered = null;

    public MainWindowViewModel()
    { }

}
