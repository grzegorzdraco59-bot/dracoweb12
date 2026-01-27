using System.Collections.ObjectModel;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.UI.WPF.Services;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla okna wyboru firmy
/// </summary>
public class CompanySelectionViewModel : ViewModelBase
{
    private CompanyDto? _selectedCompany;
    private readonly int _userId;
    private readonly string _userFullName;
    private readonly IUserContext _userContext;

    public event EventHandler<CompanyDto>? CompanySelected;
    public event EventHandler? SelectionCancelled;

    public CompanySelectionViewModel(
        IEnumerable<CompanyDto> companies, 
        int userId, 
        string userFullName,
        IUserContext userContext)
    {
        _userId = userId;
        _userFullName = userFullName;
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        Companies = new ObservableCollection<CompanyDto>(companies);
        
        // Najpierw tworzymy komendy, żeby były dostępne w setterze SelectedCompany
        SelectCommand = new RelayCommand(() => SelectCompany(), () => SelectedCompany != null);
        CancelCommand = new RelayCommand(() => Cancel());
        
        // Teraz możemy bezpiecznie ustawić SelectedCompany
        SelectedCompany = Companies.FirstOrDefault(c => c.IsDefault) ?? Companies.FirstOrDefault();
    }

    public ObservableCollection<CompanyDto> Companies { get; }
    
    public CompanyDto? SelectedCompany
    {
        get => _selectedCompany;
        set
        {
            if (SetProperty(ref _selectedCompany, value))
            {
                // Aktualizujemy stan komendy tylko jeśli SelectCommand jest już zainicjalizowany
                if (SelectCommand is RelayCommand relayCommand)
                {
                    relayCommand.RaiseCanExecuteChanged();
                }
            }
        }
    }

    public string UserFullName => _userFullName;

    public ICommand SelectCommand { get; }
    public ICommand CancelCommand { get; }

    private void SelectCompany()
    {
        if (SelectedCompany != null)
        {
            // Ustawiamy sesję z wybraną firmą
            _userContext.SetSession(
                _userId,
                SelectedCompany.Id,
                SelectedCompany.RoleId,
                _userFullName);
            
            CompanySelected?.Invoke(this, SelectedCompany);
        }
    }

    private void Cancel()
    {
        SelectionCancelled?.Invoke(this, EventArgs.Empty);
    }
}
