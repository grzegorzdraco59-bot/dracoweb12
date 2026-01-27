using System.Windows;
using System.Windows.Controls;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Okno edycji OperatorLogin
/// </summary>
public partial class UserLoginEditWindow : Window
{
    public UserLoginEditWindow(UserLoginEditViewModel viewModel)
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

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserLoginEditViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.Password = passwordBox.Password;
        }
    }
}
