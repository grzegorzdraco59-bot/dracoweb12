using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Services;
using ERP.Infrastructure.Services;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla okna logowania
/// </summary>
public class LoginViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IConnectionStringProvider _connectionStringProvider;
    private string _login = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isLoggingIn; // Guard: blokada double-click i re-entrant
    private bool _isTestingConnection;

    public event EventHandler<(UserDto User, IEnumerable<CompanyDto> Companies)>? LoginSuccessful;
    public event EventHandler? LoginCancelled;

    // Właściwości do przekazania danych do App.xaml.cs
    public UserDto? LoggedInUser { get; private set; }
    public IEnumerable<CompanyDto>? UserCompanies { get; private set; }

    /// <summary>Opcje środowiska bazy danych.</summary>
    public record DatabaseEnvironmentOption(string Value, string DisplayName);

    public IReadOnlyList<DatabaseEnvironmentOption> EnvironmentOptions { get; } =
    [
        new("LOCBD", "Test (locbd)"),
        new("DRACO_OFFICE_WIFI", "Biuro (Wi-Fi)"),
        new("DRACO_REMOTE", "Zdalnie (Internet/VPN)")
    ];

    public string SelectedEnvironment
    {
        get => _connectionStringProvider.GetActiveDatabase();
        set
        {
            if (string.Equals(SelectedEnvironment, value, StringComparison.OrdinalIgnoreCase))
                return;
            try
            {
                _connectionStringProvider.SetActiveDatabase(value);
                OnPropertyChanged(nameof(SelectedEnvironment));
                OnPropertyChanged(nameof(CurrentEnvironmentDisplay));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Błąd zmiany środowiska", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }
    }

    public string CurrentEnvironmentDisplay => _connectionStringProvider.GetEnvironmentDisplayName();

    public ICommand TestConnectionCommand { get; }

    public LoginViewModel(IAuthenticationService authenticationService, IConnectionStringProvider connectionStringProvider)
    {
        try
        {
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _connectionStringProvider = connectionStringProvider ?? throw new ArgumentNullException(nameof(connectionStringProvider));
            LoginCommand = new RelayCommand(async () => await LoginAsync(), () => CanLogin());
            CancelCommand = new RelayCommand(() => Cancel());
            TestConnectionCommand = new RelayCommand(async () => await TestConnectionAsync(), () => !_isTestingConnection);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.ToString(), "LoginViewModel – błąd inicjalizacji", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            throw;
        }
    }

    public string Login
    {
        get => _login;
        set
        {
            SetProperty(ref _login, value);
            ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
            ClearError();
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            SetProperty(ref _password, value);
            ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
            ClearError();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            SetProperty(ref _errorMessage, value);
            HasError = !string.IsNullOrWhiteSpace(value);
        }
    }

    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(ref _hasError, value);
    }

    /// <summary>True podczas logowania – przycisk "Zaloguj" disabled, blokada re-entrant.</summary>
    public bool IsLoggingIn
    {
        get => _isLoggingIn;
        private set
        {
            if (SetProperty(ref _isLoggingIn, value))
                ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand LoginCommand { get; }
    public ICommand CancelCommand { get; }

    private bool CanLogin()
    {
        if (IsLoggingIn)
            return false;
        if (string.IsNullOrWhiteSpace(Login))
            return false;
        // Tryb testowy (login = ID): hasło może być puste
        if (int.TryParse(Login.Trim(), out var id) && id > 0)
            return true;
        // Normalny tryb: wymagane hasło
        return !string.IsNullOrWhiteSpace(Password);
    }

    private async Task LoginAsync()
    {
        if (IsLoggingIn)
            return; // Re-entrant guard
        if (!CanLogin())
            return;

        IsLoggingIn = true;
        try
        {
            ClearError();

            // Autentykacja użytkownika (test: login=ID, hasło dowolne; normalnie: login+hasło)
            var user = await _authenticationService.AuthenticateAsync(Login, Password);

            if (user == null)
            {
                ErrorMessage = "Nieprawidłowy login lub hasło.";
                return;
            }

            // Walidacja: SELECT COUNT(*) FROM operatorfirma WHERE id_operatora = @UserId
            if (!await _authenticationService.HasCompaniesForUserAsync(user.Id))
            {
                ErrorMessage = $"Użytkownik nie ma przypisanych żadnych firm. (UserId: {user.Id})";
                System.Windows.MessageBox.Show(
                    $"Brak firm dla użytkownika ID: {user.Id}\n" +
                    "Sprawdź czy w tabeli 'operatorfirma' są rekordy dla tego użytkownika.",
                    "Brak firm",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Pobieramy listę firm (ta sama logika: id_operatora = @UserId)
            var companies = await _authenticationService.GetUserCompaniesAsync(user.Id);
            var companiesList = companies.ToList();

            LoggedInUser = user;
            UserCompanies = companiesList;
            LoginSuccessful?.Invoke(this, (user, companiesList));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            System.Windows.MessageBox.Show(ex.ToString(), "Błąd logowania", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoggingIn = false;
        }
    }

    private void Cancel()
    {
        LoginCancelled?.Invoke(this, EventArgs.Empty);
    }

    private async Task TestConnectionAsync()
    {
        if (_isTestingConnection)
            return;
        _isTestingConnection = true;
        ((RelayCommand)TestConnectionCommand).RaiseCanExecuteChanged();
        try
        {
            var activeDb = _connectionStringProvider.GetActiveDatabase();
            var (server, port, database) = _connectionStringProvider.GetConnectionInfoForDisplay();
            var info = $"ActiveDatabase = {activeDb}\nServer = {server}\nPort = {port}\nDatabase = {database}";
            var error = await _connectionStringProvider.TestConnectionAsync();
            if (error == null)
                System.Windows.MessageBox.Show($"Połączenie OK\n\n{info}", "Test połączenia", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            else
                System.Windows.MessageBox.Show($"Błąd połączenia:\n\n{info}\n\n{error}", "Test połączenia", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
        finally
        {
            _isTestingConnection = false;
            ((RelayCommand)TestConnectionCommand).RaiseCanExecuteChanged();
        }
    }

    private void ClearError()
    {
        if (HasError)
        {
            ErrorMessage = string.Empty;
        }
    }
}
