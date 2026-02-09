using System.Windows;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Interaction logic for SupplierEditWindow.xaml
/// </summary>
public partial class SupplierEditWindow : Window
{
    public SupplierEditWindow(SupplierEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.Saved += (sender, e) => DialogResult = true;
        viewModel.Cancelled += (sender, e) => DialogResult = false;
    }
}
