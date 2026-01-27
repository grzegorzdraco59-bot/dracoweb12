using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Services;
using ERP.Domain.Entities;
using ERP.Domain.Repositories;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla okna edycji operatorlogin
/// </summary>
public class UserLoginEditViewModel : ViewModelBase
{
    private readonly IUserLoginRepository _repository;
    private readonly IAuthenticationService _authenticationService;
    private readonly UserLoginDto _originalDto;
    private readonly bool _isNew;
    
    private int _userId;
    private string _login = string.Empty;
    private string _password = string.Empty;

    public UserLoginEditViewModel(
        IUserLoginRepository repository, 
        IAuthenticationService authenticationService,
        UserLoginDto userLoginDto, 
        bool isNew = false)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _originalDto = userLoginDto ?? throw new ArgumentNullException(nameof(userLoginDto));
        _isNew = isNew;
        
        _userId = userLoginDto.UserId;
        _login = userLoginDto.Login ?? string.Empty;
        // Hasło pozostaje puste podczas edycji (użytkownik wprowadza nowe hasło)
        _password = string.Empty;
        
        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => CanSave());
        CancelCommand = new RelayCommand(() => OnCancelled());
    }

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    public int Id => _originalDto.Id;
    public bool IsNew => _isNew;
    
    public string Title => _isNew ? "Dodawanie OperatorLogin" : "Edycja OperatorLogin";

    public int UserId
    {
        get => _userId;
        set
        {
            _userId = value;
            OnPropertyChanged();
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        }
    }

    public string Login
    {
        get => _login;
        set
        {
            _login = value;
            OnPropertyChanged();
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            _password = value;
            OnPropertyChanged();
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private bool CanSave()
    {
        return UserId > 0 && !string.IsNullOrWhiteSpace(Login) && !string.IsNullOrWhiteSpace(Password);
    }

    private async Task SaveAsync()
    {
        try
        {
            // Hashujemy hasło przed zapisem używając BCrypt
            var passwordHash = _authenticationService.HashPassword(Password);

            if (_isNew)
            {
                // Tworzymy nowy UserLogin
                var newUserLogin = new UserLogin(UserId, Login, passwordHash);
                var id = await _repository.AddAsync(newUserLogin);
            }
            else
            {
                // Pobieramy istniejący UserLogin
                if (_originalDto.Id <= 0)
                {
                    System.Windows.MessageBox.Show(
                        "Nieprawidłowe ID rekordu. Nie można edytować rekordu bez ID.",
                        "Błąd",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                    return;
                }

                var existingUserLogin = await _repository.GetByIdAsync(_originalDto.Id);
                if (existingUserLogin == null)
                {
                    System.Windows.MessageBox.Show(
                        $"OperatorLogin o ID {_originalDto.Id} nie został znaleziony.",
                        "Błąd",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                    return;
                }

                // Aktualizujemy właściwości
                if (existingUserLogin.UserId != UserId)
                {
                    // Jeśli zmienił się UserId, musimy usunąć stary i dodać nowy
                    await _repository.DeleteAsync(_originalDto.Id);
                    var newUserLogin = new UserLogin(UserId, Login, passwordHash);
                    await _repository.AddAsync(newUserLogin);
                }
                else
                {
                    // Aktualizujemy login i hasło
                    if (existingUserLogin.Login != Login)
                    {
                        existingUserLogin.UpdateLogin(Login);
                    }
                    // Zawsze aktualizujemy hasło (bo wprowadziliśmy nowe)
                    existingUserLogin.UpdatePasswordHash(passwordHash);
                    await _repository.UpdateAsync(existingUserLogin);
                }
            }

            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas zapisywania operatorlogin: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }


    private void OnCancelled()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }
}
