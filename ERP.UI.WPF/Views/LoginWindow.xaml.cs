using System.Windows;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Interaction logic for LoginWindow.xaml
/// </summary>
public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // Obsługa zdarzeń ViewModel
        _viewModel.LoginSuccessful += OnLoginSuccessful;
        _viewModel.LoginCancelled += OnLoginCancelled;

        // Podpięcie zdarzenia zmiany hasła
        PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;

        // Ustawienie fokusa na pole loginu
        Loaded += (s, e) => LoginTextBox.Focus();
    }

    private void OnLoginSuccessful(object? sender, (Application.DTOs.UserDto User, IEnumerable<Application.DTOs.CompanyDto> Companies) e)
    {
        DialogResult = true;
        Close();
    }

    private void OnLoginCancelled(object? sender, EventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && sender is System.Windows.Controls.PasswordBox passwordBox)
        {
            _viewModel.Password = passwordBox.Password;
        }
    }
}
