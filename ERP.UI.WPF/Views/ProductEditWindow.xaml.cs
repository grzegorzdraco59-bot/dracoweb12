using System.Windows;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Interaction logic for ProductEditWindow.xaml
/// </summary>
public partial class ProductEditWindow : Window
{
    public ProductEditWindow(ProductEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        viewModel.Saved += (s, e) => { DialogResult = true; Close(); };
        viewModel.Cancelled += (s, e) => { DialogResult = false; Close(); };
    }
}
