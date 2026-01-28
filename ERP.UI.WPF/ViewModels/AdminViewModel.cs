using System.Collections.ObjectModel;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Services;
using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Services;
using ERP.UI.WPF.Views;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla widoku administratora - zarządzanie operatorami
/// </summary>
public class AdminViewModel : ViewModelBase
{
    private readonly IUserRepository _userRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserCompanyRepository _userCompanyRepository;
    private readonly IUserLoginRepository _userLoginRepository;
    private readonly IOperatorPermissionService _permissionService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IUnitOfWork _unitOfWork;
    private UserDto? _selectedOperator;
    private UserCompanyDto? _selectedUserCompany;
    private UserLoginDto? _selectedUserLogin;
    private OperatorTablePermissionDto? _selectedPermission;

    public AdminViewModel(
        IUserRepository userRepository,
        ICompanyRepository companyRepository,
        IUserCompanyRepository userCompanyRepository,
        IUserLoginRepository userLoginRepository,
        IOperatorPermissionService permissionService,
        IAuthenticationService authenticationService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
        _userCompanyRepository = userCompanyRepository ?? throw new ArgumentNullException(nameof(userCompanyRepository));
        _userLoginRepository = userLoginRepository ?? throw new ArgumentNullException(nameof(userLoginRepository));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        
        Operators = new ObservableCollection<UserDto>();
        UserCompanies = new ObservableCollection<UserCompanyDto>();
        UserLogins = new ObservableCollection<UserLoginDto>();
        Permissions = new ObservableCollection<OperatorTablePermissionDto>();
        
        LoadOperatorsCommand = new RelayCommand(async () => await LoadOperatorsAsync());
        AddOperatorCommand = new RelayCommand(async () => await AddOperatorAsync());
        EditOperatorCommand = new RelayCommand(async () => await EditOperatorAsync(), () => SelectedOperator != null);
        DeleteOperatorCommand = new RelayCommand(async () => await DeleteOperatorAsync(), () => SelectedOperator != null);
        
        LoadUserCompaniesCommand = new RelayCommand(async () => await LoadUserCompaniesAsync());
        AddUserCompanyCommand = new RelayCommand(async () => await AddUserCompanyAsync());
        EditUserCompanyCommand = new RelayCommand(() => EditUserCompany(), () => SelectedUserCompany != null);
        DeleteUserCompanyCommand = new RelayCommand(async () => await DeleteUserCompanyAsync(), () => SelectedUserCompany != null);
        
        LoadUserLoginsCommand = new RelayCommand(async () => await LoadUserLoginsAsync());
        AddUserLoginCommand = new RelayCommand(async () => await AddUserLoginAsync());
        EditUserLoginCommand = new RelayCommand(() => EditUserLogin(), () => SelectedUserLogin != null);
        DeleteUserLoginCommand = new RelayCommand(async () => await DeleteUserLoginAsync(), () => SelectedUserLogin != null);
        
        LoadPermissionsCommand = new RelayCommand(async () => await LoadPermissionsAsync());
        AddPermissionCommand = new RelayCommand(async () => await AddPermissionAsync());
        EditPermissionCommand = new RelayCommand(() => EditPermission(), () => SelectedPermission != null);
        DeletePermissionCommand = new RelayCommand(async () => await DeletePermissionAsync(), () => SelectedPermission != null);
        SetFullAccessToAllTablesCommand = new RelayCommand(async () => await SetFullAccessToAllTablesAsync(), () => SelectedOperator != null);
        
        // Automatyczne ładowanie przy starcie
        _ = LoadOperatorsAsync();
    }

    public ObservableCollection<UserDto> Operators { get; }
    public ObservableCollection<UserCompanyDto> UserCompanies { get; }
    public ObservableCollection<UserLoginDto> UserLogins { get; }
    public ObservableCollection<OperatorTablePermissionDto> Permissions { get; }

    public UserDto? SelectedOperator
    {
        get => _selectedOperator;
        set
        {
            _selectedOperator = value;
            OnPropertyChanged();
            if (EditOperatorCommand is RelayCommand editCmd)
                editCmd.RaiseCanExecuteChanged();
            if (DeleteOperatorCommand is RelayCommand deleteCmd)
                deleteCmd.RaiseCanExecuteChanged();
            if (SetFullAccessToAllTablesCommand is RelayCommand fullAccessCmd)
                fullAccessCmd.RaiseCanExecuteChanged();
            
            // Automatyczne ładowanie UserCompanies, UserLogins i Permissions dla wybranego operatora
            if (value != null)
            {
                _ = LoadUserCompaniesForOperatorAsync(value.Id);
                _ = LoadUserLoginsForOperatorAsync(value.Id);
                _ = LoadPermissionsForOperatorAsync(value.Id);
            }
            else
            {
                UserCompanies.Clear();
                UserLogins.Clear();
                Permissions.Clear();
            }
        }
    }

    public UserCompanyDto? SelectedUserCompany
    {
        get => _selectedUserCompany;
        set
        {
            _selectedUserCompany = value;
            OnPropertyChanged();
            if (EditUserCompanyCommand is RelayCommand editCmd)
                editCmd.RaiseCanExecuteChanged();
            if (DeleteUserCompanyCommand is RelayCommand deleteCmd)
                deleteCmd.RaiseCanExecuteChanged();
        }
    }

    public UserLoginDto? SelectedUserLogin
    {
        get => _selectedUserLogin;
        set
        {
            _selectedUserLogin = value;
            OnPropertyChanged();
            if (EditUserLoginCommand is RelayCommand editCmd)
                editCmd.RaiseCanExecuteChanged();
            if (DeleteUserLoginCommand is RelayCommand deleteCmd)
                deleteCmd.RaiseCanExecuteChanged();
        }
    }

    public ICommand LoadOperatorsCommand { get; }
    public ICommand AddOperatorCommand { get; }
    public ICommand EditOperatorCommand { get; }
    public ICommand DeleteOperatorCommand { get; }
    
    public ICommand LoadUserCompaniesCommand { get; }
    public ICommand AddUserCompanyCommand { get; }
    public ICommand EditUserCompanyCommand { get; }
    public ICommand DeleteUserCompanyCommand { get; }
    
    public ICommand LoadUserLoginsCommand { get; }
    public ICommand AddUserLoginCommand { get; }
    public ICommand EditUserLoginCommand { get; }
    public ICommand DeleteUserLoginCommand { get; }
    
    public ICommand LoadPermissionsCommand { get; }
    public ICommand AddPermissionCommand { get; }
    public ICommand EditPermissionCommand { get; }
    public ICommand DeletePermissionCommand { get; }
    public ICommand SetFullAccessToAllTablesCommand { get; }

    public OperatorTablePermissionDto? SelectedPermission
    {
        get => _selectedPermission;
        set
        {
            _selectedPermission = value;
            OnPropertyChanged();
            if (EditPermissionCommand is RelayCommand editCmd)
                editCmd.RaiseCanExecuteChanged();
            if (DeletePermissionCommand is RelayCommand deleteCmd)
                deleteCmd.RaiseCanExecuteChanged();
        }
    }

    private async Task LoadOperatorsAsync()
    {
        try
        {
            Operators.Clear();
            var users = await _userRepository.GetAllAsync();
            
            foreach (var user in users)
            {
                Operators.Add(new UserDto
                {
                    Id = user.Id,
                    DefaultCompanyId = user.DefaultCompanyId,
                    FullName = user.FullName,
                    Permissions = user.Permissions
                });
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas ładowania operatorów: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task AddOperatorAsync()
    {
        try
        {
            // Tworzymy nowy pusty operator
            var newOperator = new UserDto
            {
                Id = 0, // Nowy operator
                DefaultCompanyId = 0, // Będzie trzeba ustawić w oknie edycji
                FullName = string.Empty,
                Permissions = 0
            };

            // Otwieramy okno edycji operatora w trybie dodawania
            var editViewModel = new UserEditViewModel(_userRepository, _companyRepository, newOperator, isNew: true);
            var editWindow = new UserEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                // Odświeżamy listę, aby pokazać nowo dodanego operatora
                await LoadOperatorsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas dodawania operatora: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task LoadUserCompaniesAsync()
    {
        // Metoda pozostaje dla przycisku Odśwież, ale nie jest używana automatycznie
        if (SelectedOperator != null)
        {
            await LoadUserCompaniesForOperatorAsync(SelectedOperator.Id);
        }
    }

    private async Task LoadUserCompaniesForOperatorAsync(int userId)
    {
        try
        {
            UserCompanies.Clear();
            var userCompanies = await _userCompanyRepository.GetByUserIdAsync(userId);
            
            foreach (var uc in userCompanies)
            {
                UserCompanies.Add(new UserCompanyDto
                {
                    Id = uc.Id,
                    UserId = uc.UserId,
                    CompanyId = uc.CompanyId,
                    RoleId = uc.RoleId
                });
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas ładowania operatorfirma: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task LoadUserLoginsAsync()
    {
        // Metoda pozostaje dla przycisku Odśwież, ale nie jest używana automatycznie
        if (SelectedOperator != null)
        {
            await LoadUserLoginsForOperatorAsync(SelectedOperator.Id);
        }
    }

    private async Task LoadUserLoginsForOperatorAsync(int userId)
    {
        try
        {
            UserLogins.Clear();
            var userLogin = await _userLoginRepository.GetByUserIdAsync(userId);
            
            if (userLogin != null)
            {
                UserLogins.Add(new UserLoginDto
                {
                    Id = userLogin.Id,
                    UserId = userLogin.UserId,
                    Login = userLogin.Login,
                    PasswordHash = userLogin.PasswordHash
                });
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas ładowania operatorlogin: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task EditOperatorAsync()
    {
        if (SelectedOperator == null) return;

        try
        {
            // Pobieramy pełne dane operatora z repozytorium
            var user = await _userRepository.GetByIdAsync(SelectedOperator.Id);
            
            if (user == null)
            {
                System.Windows.MessageBox.Show(
                    $"Operator o ID {SelectedOperator.Id} nie został znaleziony.",
                    "Błąd",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return;
            }

            // Tworzymy DTO z pełnymi danymi
            var userDto = new UserDto
            {
                Id = user.Id,
                DefaultCompanyId = user.DefaultCompanyId,
                FullName = user.FullName,
                Permissions = user.Permissions
            };

            // Otwieramy okno edycji operatora
            var editViewModel = new UserEditViewModel(_userRepository, _companyRepository, userDto, isNew: false);
            var editWindow = new UserEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                await LoadOperatorsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas otwierania okna edycji operatora: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task DeleteOperatorAsync()
    {
        if (SelectedOperator == null) return;

        try
        {
            var result = System.Windows.MessageBox.Show(
                $"Czy na pewno chcesz usunąć operatora '{SelectedOperator.FullName}' (ID: {SelectedOperator.Id})?\n\n" +
                "Uwaga: Spowoduje to również usunięcie powiązanych rekordów z operatorfirma i operatorlogin.",
                "Potwierdzenie usunięcia",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                // TODO: Implementuj usuwanie operatora gdy będzie potrzebne
                // Na razie pokazujemy MessageBox
                System.Windows.MessageBox.Show(
                    "Funkcjonalność usuwania operatora - w przygotowaniu",
                    "Info",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas usuwania operatora: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task AddUserCompanyAsync()
    {
        try
        {
            if (SelectedOperator == null)
            {
                System.Windows.MessageBox.Show(
                    "Najpierw wybierz operatora, dla którego chcesz dodać powiązanie z firmą.",
                    "Brak wybranego operatora",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Tworzymy nowy UserCompany - używamy wybranego operatora
            var newUserCompany = new UserCompanyDto
            {
                UserId = SelectedOperator.Id,
                CompanyId = SelectedOperator.DefaultCompanyId,
                RoleId = null
            };

            var editViewModel = new UserCompanyEditViewModel(_userCompanyRepository, newUserCompany, isNew: true);
            var editWindow = new UserCompanyEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                await LoadUserCompaniesAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas dodawania operatorfirma: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void EditUserCompany()
    {
        if (SelectedUserCompany == null) return;

        try
        {
            var editViewModel = new UserCompanyEditViewModel(_userCompanyRepository, SelectedUserCompany, isNew: false);
            var editWindow = new UserCompanyEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                _ = LoadUserCompaniesAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas otwierania okna edycji operatorfirma: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task DeleteUserCompanyAsync()
    {
        if (SelectedUserCompany == null) return;

        try
        {
            var result = System.Windows.MessageBox.Show(
                $"Czy na pewno chcesz usunąć powiązanie operatora {SelectedUserCompany.UserId} z firmą {SelectedUserCompany.CompanyId}?",
                "Potwierdzenie usunięcia",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var userCompany = await _userCompanyRepository.GetByIdAsync(SelectedUserCompany.Id);
                if (userCompany != null)
                {
                    await _userCompanyRepository.DeleteByIdAsync(SelectedUserCompany.Id);
                    await LoadUserCompaniesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas usuwania operatorfirma: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task AddUserLoginAsync()
    {
        try
        {
            if (SelectedOperator == null)
            {
                System.Windows.MessageBox.Show(
                    "Najpierw wybierz operatora, dla którego chcesz dodać dane logowania.",
                    "Brak wybranego operatora",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Sprawdzamy, czy operator już ma dane logowania
            var existingLogin = await _userLoginRepository.GetByUserIdAsync(SelectedOperator.Id);
            if (existingLogin != null)
            {
                System.Windows.MessageBox.Show(
                    $"Operator {SelectedOperator.FullName} już ma dane logowania. Użyj edycji, aby je zmienić.",
                    "Dane logowania już istnieją",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Tworzymy nowy UserLogin - używamy wybranego operatora
            var newUserLogin = new UserLoginDto
            {
                UserId = SelectedOperator.Id,
                Login = string.Empty,
                PasswordHash = string.Empty
            };

            var editViewModel = new UserLoginEditViewModel(_userLoginRepository, _authenticationService, _unitOfWork, newUserLogin, isNew: true);
            var editWindow = new UserLoginEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                await LoadUserLoginsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas dodawania operatorlogin: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void EditUserLogin()
    {
        if (SelectedUserLogin == null) return;

        try
        {
            var editViewModel = new UserLoginEditViewModel(_userLoginRepository, _authenticationService, _unitOfWork, SelectedUserLogin, isNew: false);
            var editWindow = new UserLoginEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                _ = LoadUserLoginsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas otwierania okna edycji operatorlogin: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task DeleteUserLoginAsync()
    {
        if (SelectedUserLogin == null) return;

        try
        {
            var result = System.Windows.MessageBox.Show(
                $"Czy na pewno chcesz usunąć dane logowania dla operatora {SelectedUserLogin.UserId} (login: {SelectedUserLogin.Login})?",
                "Potwierdzenie usunięcia",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                await _userLoginRepository.DeleteAsync(SelectedUserLogin.Id);
                await LoadUserLoginsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas usuwania operatorlogin: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task LoadPermissionsAsync()
    {
        if (SelectedOperator != null)
        {
            await LoadPermissionsForOperatorAsync(SelectedOperator.Id);
        }
    }

    private async Task LoadPermissionsForOperatorAsync(int operatorId)
    {
        try
        {
            Permissions.Clear();
            var permissions = await _permissionService.GetByOperatorIdAsync(operatorId);
            
            foreach (var permission in permissions)
            {
                Permissions.Add(permission);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas ładowania uprawnień: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task AddPermissionAsync()
    {
        try
        {
            if (SelectedOperator == null)
            {
                System.Windows.MessageBox.Show(
                    "Najpierw wybierz operatora, dla którego chcesz dodać uprawnienia.",
                    "Brak wybranego operatora",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var newPermission = new OperatorTablePermissionDto
            {
                OperatorId = SelectedOperator.Id,
                OperatorName = SelectedOperator.FullName,
                TableName = string.Empty,
                CanSelect = false,
                CanInsert = false,
                CanUpdate = false,
                CanDelete = false
            };

            var editViewModel = new OperatorTablePermissionEditViewModel(_permissionService, newPermission, isNew: true);
            var editWindow = new OperatorTablePermissionEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                await LoadPermissionsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas dodawania uprawnienia: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void EditPermission()
    {
        if (SelectedPermission == null) return;

        try
        {
            var editViewModel = new OperatorTablePermissionEditViewModel(_permissionService, SelectedPermission, isNew: false);
            var editWindow = new OperatorTablePermissionEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                _ = LoadPermissionsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas otwierania okna edycji uprawnienia: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task DeletePermissionAsync()
    {
        if (SelectedPermission == null) return;

        try
        {
            var result = System.Windows.MessageBox.Show(
                $"Czy na pewno chcesz usunąć uprawnienie operatora '{SelectedPermission.OperatorName}' do tabeli '{SelectedPermission.TableName}'?",
                "Potwierdzenie usunięcia",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                await _permissionService.DeletePermissionAsync(SelectedPermission.Id);
                await LoadPermissionsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas usuwania uprawnienia: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task SetFullAccessToAllTablesAsync()
    {
        if (SelectedOperator == null) return;

        try
        {
            var result = System.Windows.MessageBox.Show(
                $"Czy na pewno chcesz ustawić pełny dostęp (SELECT, INSERT, UPDATE, DELETE) dla operatora '{SelectedOperator.FullName}' do wszystkich tabel w bazie danych?\n\n" +
                "Ta operacja ustawi pełny dostęp do wszystkich dostępnych tabel. Istniejące uprawnienia zostaną zaktualizowane.",
                "Potwierdzenie ustawienia pełnego dostępu",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            // Pobierz listę wszystkich dostępnych tabel
            var tables = await _permissionService.GetAvailableTablesAsync();
            var tablesList = tables.ToList();

            if (!tablesList.Any())
            {
                System.Windows.MessageBox.Show(
                    "Nie znaleziono żadnych tabel w bazie danych.",
                    "Brak tabel",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }

            // Ustaw pełny dostęp do każdej tabeli
            int successCount = 0;
            int errorCount = 0;
            var errors = new List<string>();

            foreach (var tableName in tablesList)
            {
                try
                {
                    await _permissionService.SetPermissionAsync(
                        SelectedOperator.Id,
                        tableName,
                        canSelect: true,
                        canInsert: true,
                        canUpdate: true,
                        canDelete: true);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    errors.Add($"{tableName}: {ex.Message}");
                }
            }

            // Odśwież listę uprawnień
            await LoadPermissionsAsync();

            // Pokaż podsumowanie
            var message = $"Ustawiono pełny dostęp do {successCount} tabel.";
            if (errorCount > 0)
            {
                message += $"\n\nBłędy podczas ustawiania uprawnień do {errorCount} tabel:\n" + string.Join("\n", errors.Take(5));
                if (errors.Count > 5)
                {
                    message += $"\n... i {errors.Count - 5} więcej błędów.";
                }
            }

            System.Windows.MessageBox.Show(
                message,
                errorCount > 0 ? "Ustawianie uprawnień zakończone z błędami" : "Sukces",
                System.Windows.MessageBoxButton.OK,
                errorCount > 0 ? System.Windows.MessageBoxImage.Warning : System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas ustawiania pełnego dostępu do wszystkich tabel: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
}
