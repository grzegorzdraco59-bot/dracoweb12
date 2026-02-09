using System.Collections.ObjectModel;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Infrastructure.Repositories;
using ERP.UI.WPF.Services;
using ERP.UI.WPF.Views;
using MySqlConnector;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel listy powiązań operator–firma (moduł OperatorFirma).
/// Filtruje po wybranej firmie (id_firmy) i opcjonalnie po operatorze (id_operatora).
/// </summary>
public class OperatorCompanyListViewModel : ViewModelBase
{
    private readonly OperatorCompanyRepository _repository;
    private readonly IUserContext _userContext;
    private int _companyId;
    private int _filterOperatorId;
    private bool _includeInactive;
    private OperatorCompanyDto? _selectedItem;

    public OperatorCompanyListViewModel(OperatorCompanyRepository repository, IUserContext userContext)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        Items = new ObservableCollection<OperatorCompanyDto>();
        _companyId = userContext.CompanyId ?? 0;

        LoadCommand = new RelayCommand(async () => await LoadAsync());
        AddCommand = new RelayCommand(async () => await AddAsync());
        EditCommand = new RelayCommand(async () => await EditAsync(), () => SelectedItem != null);
        DeactivateCommand = new RelayCommand(async () => await DeactivateAsync(), () => SelectedItem != null && SelectedItem.IsActive);

        // Auto-ładuj gdy firma jest wybrana
        if (_companyId > 0)
            System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => await LoadAsync());
    }

    /// <summary>ID firmy do filtrowania (z kontekstu lub ręcznie).</summary>
    public int CompanyId
    {
        get => _companyId;
        set => SetProperty(ref _companyId, value);
    }

    /// <summary>ID operatora do filtrowania (0 = wszystkie).</summary>
    public int FilterOperatorId
    {
        get => _filterOperatorId;
        set => SetProperty(ref _filterOperatorId, value);
    }

    /// <summary>Pokaż też nieaktywne powiązania (IsActive=0). Po zmianie lista jest przeładowywana.</summary>
    public bool IncludeInactive
    {
        get => _includeInactive;
        set
        {
            if (!SetProperty(ref _includeInactive, value)) return;
            if (_companyId > 0)
                _ = LoadAsync();
        }
    }

    public ObservableCollection<OperatorCompanyDto> Items { get; }

    public ICommand LoadCommand { get; }

    public OperatorCompanyDto? SelectedItem
    {
        get => _selectedItem;
        set
        {
            _selectedItem = value;
            OnPropertyChanged();
            if (EditCommand is RelayCommand editCmd) editCmd.RaiseCanExecuteChanged();
            if (DeactivateCommand is RelayCommand deactCmd) deactCmd.RaiseCanExecuteChanged();
        }
    }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeactivateCommand { get; }

    /// <summary>
    /// Ładuje listę powiązań dla wybranej firmy (id_firmy) i opcjonalnie operatora (id_operatora).
    /// </summary>
    public async Task LoadAsync()
    {
        var companyId = _userContext.CompanyId ?? CompanyId;
        if (companyId <= 0)
        {
            System.Windows.MessageBox.Show(
                "Brak wybranej firmy. Wybierz firmę przed załadowaniem listy OperatorFirma.",
                "Brak firmy",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }
        _companyId = companyId;
        Items.Clear();
        try
        {
            var operatorId = FilterOperatorId > 0 ? FilterOperatorId : (int?)null;
            var list = await _repository.GetByCompanyIdAsync(companyId, operatorId, IncludeInactive);
            foreach (var dto in list)
                Items.Add(dto);

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var first = Items.FirstOrDefault();
                if (first != null)
                    SelectedItem = first;
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd ładowania listy: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task AddAsync()
    {
        var companyId = _userContext.CompanyId ?? CompanyId;
        var dto = new OperatorCompanyDto { Id = 0, UserId = 0, CompanyId = companyId > 0 ? companyId : 0, RoleId = null };
        var editVm = new OperatorCompanyEditViewModel(_repository, dto, isNew: true);
        var window = new OperatorCompanyEditWindow(editVm) { Owner = System.Windows.Application.Current.MainWindow };
        if (window.ShowDialog() == true)
            await LoadAsync();
    }

    private async Task EditAsync()
    {
        if (SelectedItem == null) return;
        var existing = await _repository.GetByIdAsync(SelectedItem.Id);
        if (existing == null)
        {
            System.Windows.MessageBox.Show("Rekord nie istnieje.", "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            await LoadAsync();
            return;
        }
        var editVm = new OperatorCompanyEditViewModel(_repository, existing, isNew: false);
        var window = new OperatorCompanyEditWindow(editVm) { Owner = System.Windows.Application.Current.MainWindow };
        if (window.ShowDialog() == true)
            await LoadAsync();
    }

    private async Task DeactivateAsync()
    {
        if (SelectedItem == null || !SelectedItem.IsActive) return;
        var result = System.Windows.MessageBox.Show(
            $"Czy na pewno dezaktywować powiązanie (ID {SelectedItem.Id})?",
            "Potwierdzenie",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (result != System.Windows.MessageBoxResult.Yes) return;
        try
        {
            await _repository.DeactivateAsync(SelectedItem.Id);
            await LoadAsync();
        }
        catch (MySqlException sqlEx)
        {
            var message = sqlEx.Number switch
            {
                1062 => "Powiązanie operator–firma już istnieje.",
                1451 => "Nie można wykonać operacji z powodu powiązań w bazie danych.",
                _ => $"Błąd bazy danych: {sqlEx.Message}"
            };
            System.Diagnostics.Trace.WriteLine($"OperatorCompany Deactivate: {sqlEx}");
            System.Windows.MessageBox.Show(message, "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"OperatorCompany Deactivate: {ex}");
            System.Windows.MessageBox.Show(
                "Wystąpił błąd podczas dezaktywacji. Szczegóły w logu.",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
}
