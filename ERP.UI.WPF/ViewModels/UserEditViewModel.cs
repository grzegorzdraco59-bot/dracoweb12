using System.Collections.ObjectModel;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Domain.Entities;
using ERP.Domain.Repositories;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla okna edycji operatora
/// </summary>
public class UserEditViewModel : ViewModelBase
{
    private readonly IUserRepository _repository;
    private readonly ICompanyRepository _companyRepository;
    private readonly UserDto _originalDto;
    private readonly bool _isNew;
    
    private int _defaultCompanyId;
    private string _fullName = string.Empty;
    private int _permissions;
    private Company? _selectedCompany;

    public UserEditViewModel(IUserRepository repository, ICompanyRepository companyRepository, UserDto userDto, bool isNew = false)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
        _originalDto = userDto ?? throw new ArgumentNullException(nameof(userDto));
        _isNew = isNew;
        
        _defaultCompanyId = userDto.DefaultCompanyId;
        _fullName = userDto.FullName ?? string.Empty;
        _permissions = userDto.Permissions;
        
        Companies = new ObservableCollection<Company>();
        
        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => CanSave());
        CancelCommand = new RelayCommand(() => OnCancelled());
        
        // Ładujemy listę firm asynchronicznie
        _ = LoadCompaniesAsync();
    }

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    public int Id => _originalDto.Id;
    public bool IsNew => _isNew;
    
    public string Title => _isNew ? "Dodawanie Operatora" : "Edycja Operatora";

    public ObservableCollection<Company> Companies { get; }

    public Company? SelectedCompany
    {
        get => _selectedCompany;
        set
        {
            _selectedCompany = value;
            if (value != null)
            {
                _defaultCompanyId = value.Id;
                OnPropertyChanged(nameof(DefaultCompanyId));
            }
            OnPropertyChanged();
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        }
    }

    public int DefaultCompanyId
    {
        get => _defaultCompanyId;
        set
        {
            _defaultCompanyId = value;
            // Aktualizujemy SelectedCompany gdy DefaultCompanyId się zmienia
            if (value > 0)
            {
                _selectedCompany = Companies.FirstOrDefault(c => c.Id == value);
                OnPropertyChanged(nameof(SelectedCompany));
            }
            OnPropertyChanged();
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        }
    }

    public string FullName
    {
        get => _fullName;
        set
        {
            _fullName = value;
            OnPropertyChanged();
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        }
    }

    public int Permissions
    {
        get => _permissions;
        set
        {
            _permissions = value;
            OnPropertyChanged();
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        }
    }

    public string PermissionsText
    {
        get => _permissions.ToString();
        set
        {
            if (int.TryParse(value, out int parsedValue))
            {
                _permissions = parsedValue;
                OnPropertyChanged(nameof(Permissions));
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
            OnPropertyChanged();
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private bool CanSave()
    {
        return DefaultCompanyId > 0 && !string.IsNullOrWhiteSpace(FullName);
    }

    private async Task SaveAsync()
    {
        try
        {
            if (_isNew)
            {
                // Tworzymy nowy User
                var newUser = new User(DefaultCompanyId, FullName, Permissions);
                var id = await _repository.AddAsync(newUser);
            }
            else
            {
                // Pobieramy istniejący User
                var existingUser = await _repository.GetByIdAsync(_originalDto.Id);
                if (existingUser == null)
                {
                    System.Windows.MessageBox.Show(
                        $"Operator o ID {_originalDto.Id} nie został znaleziony.",
                        "Błąd",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                    return;
                }

                // Aktualizujemy właściwości
                if (existingUser.FullName != FullName)
                {
                    existingUser.UpdateFullName(FullName);
                }
                
                if (existingUser.Permissions != Permissions)
                {
                    existingUser.UpdatePermissions(Permissions);
                }

                // Aktualizujemy DefaultCompanyId przez refleksję jeśli się zmieniło
                if (existingUser.DefaultCompanyId != DefaultCompanyId)
                {
                    var companyIdProperty = typeof(User).GetProperty("DefaultCompanyId", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (companyIdProperty != null)
                    {
                        companyIdProperty.SetValue(existingUser, DefaultCompanyId);
                        // UpdateTimestamp() jest wywoływane wewnętrznie przez metody domenowe
                        // lub UpdateAsync w repozytorium zaktualizuje UpdatedAt
                    }
                }

                await _repository.UpdateAsync(existingUser);
            }

            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas zapisywania operatora: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void OnCancelled()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private async Task LoadCompaniesAsync()
    {
        try
        {
            Companies.Clear();
            var companies = await _companyRepository.GetAllAsync();
            
            foreach (var company in companies)
            {
                Companies.Add(company);
            }

            // Ustawiamy SelectedCompany na podstawie DefaultCompanyId
            if (_defaultCompanyId > 0)
            {
                _selectedCompany = Companies.FirstOrDefault(c => c.Id == _defaultCompanyId);
                OnPropertyChanged(nameof(SelectedCompany));
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas ładowania listy firm: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
}
