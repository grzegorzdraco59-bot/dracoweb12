using System.IO;
using System.Windows;
using ERP.UI.WPF;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Interaction logic for LoginWindow.xaml
/// </summary>
public partial class LoginWindow : Window
{
    private readonly LoginViewModel? _viewModel;

    /// <summary>Konstruktor używany przy StartupUri – ViewModel z App (DI).</summary>
    public LoginWindow()
    {
        try
        {
            InitializeComponent();
            _viewModel = (System.Windows.Application.Current as App)?.GetLoginViewModel();
            if (_viewModel == null)
            {
                MessageBox.Show("Błąd: Nie można utworzyć ViewModelu logowania.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }
            DataContext = _viewModel;
            _viewModel.LoginSuccessful += OnLoginSuccessful;
            _viewModel.LoginCancelled += OnLoginCancelled;
            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
            Loaded += LoginWindow_Loaded;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "LoginWindow – błąd inicjalizacji", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    /// <summary>Konstruktor używany gdy okno jest tworzone z kodu (np. ShowDialog).</summary>
    public LoginWindow(LoginViewModel viewModel)
    {
        try
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            _viewModel.LoginSuccessful += OnLoginSuccessful;
            _viewModel.LoginCancelled += OnLoginCancelled;
            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
            Loaded += LoginWindow_Loaded;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "LoginWindow – błąd inicjalizacji", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            LoginTextBox.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "LoginWindow Loaded", MessageBoxButton.OK, MessageBoxImage.Error);
            try { File.AppendAllText("logs/startup.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\r\n{ex}\r\n\r\n"); } catch { }
        }
    }

    /// <summary>
    /// Flow jest obsługiwany przez App.OnLoginSuccessfulFromStartup.
    /// LoginWindow nie otwiera Main – tylko zamyka się po tym, gdy App ustawi MainWindow na Main lub CompanySelection.
    /// </summary>
    private void OnLoginSuccessful(object? sender, (Application.DTOs.UserDto User, IEnumerable<Application.DTOs.CompanyDto> Companies) e)
    {
        // Nic nie robimy – App.xaml.cs obsługuje cały flow (Main / SelectCompany)
    }

    private void OnLoginCancelled(object? sender, EventArgs e)
    {
        // Okno otwarte przez Show() – nie używamy DialogResult. Zamknięcie wywoła w App Shutdown() (gdy !_loginCompleted).
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
