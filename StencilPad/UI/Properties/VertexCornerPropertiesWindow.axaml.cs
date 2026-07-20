using Avalonia.Controls;
using Avalonia.Interactivity;
using StencilPad.Common;
using StencilPad.Models;
using StencilPad.Services;
using StencilPad.ViewModels.Properties;

namespace StencilPad.UI.Properties;

public partial class VertexCornerPropertiesWindow : Window
{
    public VertexCornerPropertiesViewModel ViewModel { get; }

    public VertexCornerPropertiesWindow(Sheet sheet,
                                        ISettings settings,
                                        IOperationService operationService)
    {
        InitializeComponent();

        ViewModel = new VertexCornerPropertiesViewModel(sheet, settings, operationService);
        DataContext = ViewModel;

        CornerTypeComboBox.ItemsSource = VertexCornerPropertiesViewModel.CornerTypes;
        CornerTypeComboBox.SelectedItem = VertexCornerPropertiesViewModel.CornerTypes
            .FirstOrDefault(x => x.Value == ViewModel.CornerType);
    }

    private void CornerTypeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (CornerTypeComboBox.SelectedItem is CornerTypeItem item)
        {
            ViewModel.CornerType = item.Value;
        }
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
