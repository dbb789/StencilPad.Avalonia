using System.Collections.Specialized;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using StencilPad.Common;
using StencilPad.Models;
using StencilPad.Models.Operations;
using StencilPad.Services;
using StencilPad.ViewModels;
using StencilPad.Spatial;
using StencilPad.Collections;

namespace StencilPad.Controllers;

public class MainWindowController
{
    public bool SaveState => _undoStack.SaveState;
    
    private readonly Project _project;
    private readonly IOperationService _operationService;
    private readonly IDialogService _dialogService;
    private readonly IPrintService _printService;
    private readonly IFileService _fileService;
    private readonly IImportExportService _importExportService;
    private readonly MainWindowViewModel _viewModel;
    private readonly SheetTabController.Factory _tabControllerFactory;
    private readonly UndoStack _undoStack;

    private List<(SheetTabController Controller, SheetTabViewModel ViewModel)> _sheetTabs = new();
    private string? _currentFilePath;

    public MainWindowController(Project project,
                                MainWindowViewModel viewModel,
                                IOperationService operationService,
                                IDialogService dialogService,
                                IPrintService printService,
                                IFileService fileService,
                                IImportExportService importExportService,
                                SheetTabController.Factory tabControllerFactory)
    {

        _project = project;
        _viewModel = viewModel;
        _operationService = operationService;
        _dialogService = dialogService;
        _printService = printService;
        _fileService = fileService;
        _importExportService = importExportService;
        _tabControllerFactory = tabControllerFactory;
        _undoStack = new();
        
        _viewModel.NewProjectCommand = new AsyncRelayCommand(NewProject);
        _viewModel.GridSettingsCommand = new AsyncRelayCommand(GridSettings);
        _viewModel.UnitScaleCommand = new AsyncRelayCommand(UnitScale);
        _viewModel.AddSheetCommand = new RelayCommand(AddNewSheet);
        _viewModel.RenameSheetCommand = new AsyncRelayCommand(RenameActiveSheet);
        _viewModel.DeleteSheetCommand = new AsyncRelayCommand(DeleteActiveSheet);
        _viewModel.PrintCommand = new RelayCommand(PrintSelectedTabAsync);
        _viewModel.ExitCommand = new RelayCommand(() =>
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lifetime)
            {
                lifetime.Shutdown();
            }
        });
        
        _viewModel.OpenProjectCommand = new AsyncRelayCommand(OpenProject);
        _viewModel.SaveProjectCommand = new AsyncRelayCommand(SaveProject);
        _viewModel.SaveProjectAsCommand = new AsyncRelayCommand(SaveProjectAs);
        _viewModel.UndoCommand = new RelayCommand(Undo);
        _viewModel.RedoCommand = new RelayCommand(Redo);
        _viewModel.ImportImageCommand = new RelayCommand(ImportImageAsync);
        _viewModel.ExportSvgCommand = new AsyncRelayCommand(ExportSvg);
        _viewModel.ExportPngCommand = new AsyncRelayCommand(ExportPng);
        _viewModel.ExportPdfCommand = new AsyncRelayCommand(ExportPdf);
        _viewModel.SheetTabReordered = ReorderSheet;
        
        _undoStack.SaveStateChanged += UpdateTitle;
        _operationService.OperationPushed += PushOperation;

        _project.Sheets.ListChanged += SheetsChanged;
    }

    public void Initialize()
    {
        ClearProject();
    }

    private async Task NewProject()
    {
        if (!SaveState && !await _dialogService.ShowConfirmationAsync(
            "You have unsaved changes. Are you sure you want to create a new project?",
            "Unsaved Changes",
            false))
        {
            return;
        }

        ClearProject();
    }
    
    private void ClearProject()
    {
        _undoStack.Clear();
        _operationService.DiscardEditContext();
        
        _project.Clear();
        SetCurrentFilePath(null);

        var sheet = new Sheet { Name = $"Sheet {_project.Sheets.Count() + 1}" };

        _project.Sheets.Add(sheet.Id, sheet);
    }

    private async Task OpenProject()
    {
        if (!SaveState && !await _dialogService.ShowConfirmationAsync(
            "You have unsaved changes. Are you sure you want to open a new file?",
            "Unsaved Changes",
            false))
        {
            return;
        }

        try
        {
            var path = await _fileService.OpenAsync(_project);

            if (path is not null)
            {
                _undoStack.Clear();
                _operationService.DiscardEditContext();
                SetCurrentFilePath(path);
            }
        }
        catch (FileServiceException e)
        {
            await _dialogService.ShowErrorAsync(e.Message, "Cannot Open File");
        }
    }

    public async Task OpenProject(string filename)
    {
        try
        {
            filename = System.IO.Path.GetFullPath(filename);
            
            await _fileService.OpenAsync(filename, _project);

            _undoStack.Clear();
            _operationService.DiscardEditContext();
            SetCurrentFilePath(filename);
        }
        catch (FileServiceException e)
        {
            await _dialogService.ShowErrorAsync(e.Message, "Cannot Open File");
        }
    }

    private async Task SaveProject()
    {
        if (_currentFilePath is null)
        {
            await SaveProjectAs();
            return;
        }

        try
        {
            await _fileService.SaveAsync(_project, _currentFilePath);
            _undoStack.MarkSavePoint();
        }
        catch (FileServiceException e)
        {
            await _dialogService.ShowErrorAsync(e.Message, "Cannot Save File");
        }
    }

    private async Task SaveProjectAs()
    {
        try
        {
            var path = await _fileService.SaveAsAsync(_project, _currentFilePath);

            if (path is not null)
            {
                SetCurrentFilePath(path);
                _undoStack.MarkSavePoint();
            }
        }
        catch (FileServiceException e)
        {
            await _dialogService.ShowErrorAsync(e.Message, "Cannot Save File");
        }
    }

    private async Task UnitScale()
    {
        var result = await _dialogService.ShowUnitScaleDialogAsync(_project.UnitRatio);

        if (result is null)
        {
            return;
        }

        _project.UnitRatio = result.Value;
    }

    private async Task GridSettings()
    {
        Unit gridSpacing;
        int gridSubdivisions;
        
        if (_project.UnitSystem == UnitSystem.Metric)
        {
            gridSpacing = _project.GridSpacingMetric;
            gridSubdivisions = _project.GridSubdivisionsMetric;
        }
        else
        {
            gridSpacing = _project.GridSpacingImperial;
            gridSubdivisions = _project.GridSubdivisionsImperial;
        }

        var result = await _dialogService.ShowGridSettingsDialogAsync(gridSpacing,
                                                                       gridSubdivisions,
                                                                       _project.UnitSettings);

        if (result is null)
        {
            return;
        }

        if (_project.UnitSystem == UnitSystem.Metric)
        {
            _project.GridSpacingMetric = result.Value.Spacing;
            _project.GridSubdivisionsMetric = result.Value.Subdivisions;
        }
        else
        {
            _project.GridSpacingImperial = result.Value.Spacing;
            _project.GridSubdivisionsImperial = result.Value.Subdivisions;
        }
    }

    private void SheetsChanged(ObservableListChangedArgs<Sheet> e)
    {
        switch (e.Action)
        {
        case ObservableListChangedAction.Add:
            AddSheet(e.Item, e.NewIndex);
            break;

        case ObservableListChangedAction.Remove:
            RemoveSheet(e.Item);
            break;

        case ObservableListChangedAction.Move:
            // TODO: Handle sheet reordering in the UI.
            RemoveSheet(e.Item);
            AddSheet(e.Item, e.NewIndex);
            break;
        }
    }
    
    private void AddSheet(Sheet sheet, int index = -1)
    {
        if (index < 0)
        {
            index = _sheetTabs.Count;
        }
        
        var tabViewModel = new SheetTabViewModel(sheet);
        var tabController = _tabControllerFactory.Create(tabViewModel);

        _sheetTabs.Insert(index, (tabController, tabViewModel));
        _viewModel.Tabs.Insert(index, tabViewModel);
        _viewModel.SelectedTab = tabViewModel;
    }

    private void RemoveSheet(Sheet sheet)
    {
        var tabToRemove = _sheetTabs.FirstOrDefault(t => t.ViewModel.Sheet == sheet);

        if (tabToRemove.ViewModel is null)
        {
            return;
        }
        
        _viewModel.Tabs.Remove(tabToRemove.ViewModel);
        _sheetTabs.Remove(tabToRemove);
        
        if (_viewModel.SelectedTab == tabToRemove.ViewModel)
        {
            _viewModel.SelectedTab = _viewModel.Tabs.FirstOrDefault();
        }

        tabToRemove.Controller.Dispose();
        tabToRemove.ViewModel.Dispose();
    }

    private void ReorderSheet(int fromIndex, int toIndex)
    {
        _operationService.Push(new ReorderSheetOperation(fromIndex, toIndex));
    }

    private void AddNewSheet()
    {
        var sheet = new Sheet { Name = $"Sheet {_project.Sheets.Count() + 1}" };

        _operationService.Push(new AddSheetOperation(sheet));
    }

    private async Task RenameActiveSheet()
    {
        var selectedSheet = _viewModel.SelectedTab?.Sheet;

        if (selectedSheet is null)
        {
            return;
        }
        
        var newName = await _dialogService.ShowRenameDialogAsync(selectedSheet.Name);

        if (newName != null)
        {
            _operationService.Push(new RenameSheetOperation(selectedSheet, newName));
        }
    }

    private async Task DeleteActiveSheet()
    {
        var selectedSheet = _viewModel.SelectedTab?.Sheet;

        if (selectedSheet is null)
        {
            return;
        }
        
        if (_project.Sheets.Count() <= 1)
        {
            await _dialogService.ShowWarningAsync("A project must contain at least one sheet.",
                                                   "Cannot Delete");
            return;
        }

        _operationService.Push(new RemoveSheetOperation(selectedSheet));
    }

    private async void PrintSelectedTabAsync()
    {
        var sheet = SelectedSheet();

        if (sheet is null)
        {
            return;
        }

        var success = await _printService.PrintAsync(sheet.Name, sheet);

        if (!success)
        {
            await _dialogService.ShowWarningAsync("Print job failed or was cancelled.", "Print Failed");
        }
    }

    private void PushOperation(IOperation operation, bool shouldExecute)
    {
        _undoStack.Push(operation);

        if (shouldExecute)
        {
            operation.Execute(_project, out var targetSheet);
        }
    }

    private void Undo()
    {
        if (_operationService.HasEditContext)
        {
            Debug.WriteLine("Trying to undo while an edit context is active");
            return;
        }
        
        _undoStack.Undo(_project, out var targetSheet);

        SelectSheet(targetSheet);
    }

    private void Redo()
    {
        if (_operationService.HasEditContext)
        {
            Debug.WriteLine("Trying to redo while an edit context is active");
            return;
        }

        _undoStack.Redo(_project, out var targetSheet);
        
        SelectSheet(targetSheet);
    }

    private void SelectSheet(Sheet? sheet)
    {
        if (sheet is null)
        {
            return;
        }
        
        var tab = _sheetTabs.FirstOrDefault(t => t.ViewModel.Sheet == sheet);

        if (tab.ViewModel is not null)
        {
            _viewModel.SelectedTab = tab.ViewModel;
        }
    }
    
    private async void ImportImageAsync()
    {
        var sheet = SelectedSheet();

        if (sheet is null)
        {
            return;
        }

        var viewport = _viewModel.SelectedTab?.Viewport;

        if (viewport is null)
        {
            return;
        }

        await _importExportService.ImportImageAsync(sheet, viewport);
    }

    private async Task ExportSvg()
    {
        var sheet = SelectedSheet();

        if (sheet is null)
        {
            return;
        }

        await _importExportService.ExportSvgAsync(sheet);
    }

    private async Task ExportPng()
    {
        var sheet = SelectedSheet();

        if (sheet is null)
        {
            return;
        }
        
        await _importExportService.ExportPngAsync(sheet);
    }
    
    private async Task ExportPdf()
    {
        var sheet = SelectedSheet();

        if (sheet is null)
        {
            return;
        }
        
        await _importExportService.ExportPdfAsync(sheet);
    }

    private Sheet? SelectedSheet()
    {
        return _viewModel.SelectedTab?.Sheet;
    }

    private void SetCurrentFilePath(string? path)
    {
        _currentFilePath = path;
        UpdateTitle();
    }

    private void UpdateTitle()
    {
        var title = _currentFilePath is not null
            ? $"{System.IO.Path.GetFileName(_currentFilePath)} - StencilPad"
            : "StencilPad";

        if (!_undoStack.SaveState)
        {
            title += " *";
        }

        _viewModel.Title = title;
    }
}
