using System.Windows;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Okno edycji Operatora
/// </summary>
public partial class UserEditWindow : Window
{
    public UserEditWindow(UserEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        viewModel.Saved += (s, e) =>
        {
            DialogResult = true;
            Close();
        };
        
        viewModel.Cancelled += (s, e) =>
        {
            DialogResult = false;
            Close();
        };
    }
}
