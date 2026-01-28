using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Services;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla okna logowania
/// </summary>
public class LoginViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;
    private string _login = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasError;

    public event EventHandler<(UserDto User, IEnumerable<CompanyDto> Companies)>? LoginSuccessful;
    public event EventHandler? LoginCancelled;

    // Właściwości do przekazania danych do App.xaml.cs
    public UserDto? LoggedInUser { get; private set; }
    public IEnumerable<CompanyDto>? UserCompanies { get; private set; }

    public LoginViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        LoginCommand = new RelayCommand(async () => await LoginAsync(), () => CanLogin());
        CancelCommand = new RelayCommand(() => Cancel());
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

    public ICommand LoginCommand { get; }
    public ICommand CancelCommand { get; }

    private bool CanLogin()
    {
        return !string.IsNullOrWhiteSpace(Login) && !string.IsNullOrWhiteSpace(Password);
    }

    private async Task LoginAsync()
    {
        if (!CanLogin())
            return;

        try
        {
            ClearError();

            // Autentykacja użytkownika
            var user = await _authenticationService.AuthenticateAsync(Login, Password);
            
            if (user == null)
            {
                ErrorMessage = "Nieprawidłowy login lub hasło.";
                return;
            }

            // Pobieramy listę firm użytkownika
            var companies = await _authenticationService.GetUserCompaniesAsync(user.Id);
            var companiesList = companies.ToList();

            if (!companiesList.Any())
            {
                ErrorMessage = $"Użytkownik nie ma przypisanych żadnych firm. (UserId: {user.Id})";
                System.Windows.MessageBox.Show(
                    $"Brak firm dla użytkownika ID: {user.Id}\n" +
                    $"Sprawdź czy w tabeli 'operatorfirma' są rekordy dla tego użytkownika.",
                    "Brak firm",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Zapisujemy dane do właściwości
            LoggedInUser = user;
            UserCompanies = companiesList;

            // Wywołujemy zdarzenie sukcesu z danymi użytkownika i firm
            // Okno wyboru firmy zostanie otwarte w App.xaml.cs
            LoginSuccessful?.Invoke(this, (user, companiesList));
        }
        catch (Exception ex)
        {
            var errorDetails = $"Błąd podczas logowania: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorDetails += $"\n\nWewnętrzny błąd: {ex.InnerException.Message}";
            }
            errorDetails += $"\n\nStack trace: {ex.StackTrace}";
            ErrorMessage = errorDetails;
            
            // Diagnostyka: wyjątek podczas logowania – MessageBox, żeby nie było „ciszy po zamknięciu okna”
            System.Windows.MessageBox.Show(errorDetails, "Błąd logowania (wyjątek)", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void Cancel()
    {
        LoginCancelled?.Invoke(this, EventArgs.Empty);
    }

    private void ClearError()
    {
        if (HasError)
        {
            ErrorMessage = string.Empty;
        }
    }
}
