using System.Windows;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Interaction logic for CompanySelectionWindow.xaml
/// </summary>
public partial class CompanySelectionWindow : Window
{
    private readonly CompanySelectionViewModel _viewModel;

    public CompanySelectionWindow(CompanySelectionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // Obsługa zdarzeń ViewModel
        _viewModel.CompanySelected += OnCompanySelected;
        _viewModel.SelectionCancelled += OnSelectionCancelled;
    }

    private void OnCompanySelected(object? sender, Application.DTOs.CompanyDto e)
    {
        DialogResult = true;
        Close();
    }

    private void OnSelectionCancelled(object? sender, EventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
