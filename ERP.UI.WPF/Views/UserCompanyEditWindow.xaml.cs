using System.Windows;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Interaction logic for UserCompanyEditWindow.xaml
/// </summary>
public partial class UserCompanyEditWindow : Window
{
    public UserCompanyEditWindow(UserCompanyEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Zamknij okno po zapisaniu
        viewModel.Saved += (sender, e) => DialogResult = true;
        viewModel.Cancelled += (sender, e) => DialogResult = false;
    }
}
