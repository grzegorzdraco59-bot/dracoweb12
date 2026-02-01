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
        Loaded += (s, e) => LoginTextBox.Focus();
    }

    /// <summary>Konstruktor używany gdy okno jest tworzone z kodu (np. ShowDialog).</summary>
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.LoginSuccessful += OnLoginSuccessful;
        _viewModel.LoginCancelled += OnLoginCancelled;
        PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
        Loaded += (s, e) => LoginTextBox.Focus();
    }

    private void OnLoginSuccessful(object? sender, (Application.DTOs.UserDto User, IEnumerable<Application.DTOs.CompanyDto> Companies) e)
    {
        try
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var main = new MainWindow();
                var app = System.Windows.Application.Current as App;
                if (app != null)
                {
                    main.DataContext = app.GetMainViewModel();
                }
                System.Windows.Application.Current.MainWindow = main;
                main.Show();
                Close();
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Błąd przy otwieraniu MainWindow", MessageBoxButton.OK, MessageBoxImage.Error);
            try
            {
                var dir = Path.GetDirectoryName("logs/startup.log");
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                File.AppendAllText("logs/startup.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\r\n{ex}\r\n\r\n");
            }
            catch { /* ignoruj błędy zapisu logu */ }
        }
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
