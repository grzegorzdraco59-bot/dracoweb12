using System.Collections.ObjectModel;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Infrastructure.Repositories;
using ERP.UI.WPF.Views;
using MySqlConnector;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel listy powiązań operator–firma (nowy moduł OperatorCompany).
/// Soft delete (IsActive); lista domyślnie tylko aktywne; Dezaktywuj zamiast Usuń.
/// </summary>
public class OperatorCompanyListViewModel : ViewModelBase
{
    private readonly OperatorCompanyRepository _repository;
    private int _userId;
    private int _currentUserId;
    private bool _includeInactive;
    private OperatorCompanyDto? _selectedItem;

    public OperatorCompanyListViewModel(OperatorCompanyRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        Items = new ObservableCollection<OperatorCompanyDto>();

        LoadCommand = new RelayCommand(async () => await LoadAsync(CurrentUserId));
        AddCommand = new RelayCommand(async () => await AddAsync());
        EditCommand = new RelayCommand(async () => await EditAsync(), () => SelectedItem != null);
        DeactivateCommand = new RelayCommand(async () => await DeactivateAsync(), () => SelectedItem != null && SelectedItem.IsActive);
    }

    /// <summary>ID operatora do załadowania listy powiązań. Ustaw i wywołaj LoadCommand (Ładuj).</summary>
    public int CurrentUserId
    {
        get => _currentUserId;
        set => SetProperty(ref _currentUserId, value);
    }

    /// <summary>Pokaż też nieaktywne powiązania (IsActive=0). Po zmianie lista jest przeładowywana.</summary>
    public bool IncludeInactive
    {
        get => _includeInactive;
        set
        {
            if (!SetProperty(ref _includeInactive, value)) return;
            if (_userId != 0)
                _ = LoadAsync(_userId);
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
    /// Ładuje listę powiązań dla danego operatora (domyślnie tylko IsActive=1). Po zapisie / dezaktywacji wywołaj ponownie.
    /// </summary>
    public async Task LoadAsync(int userId)
    {
        _userId = userId;
        Items.Clear();
        try
        {
            var list = await _repository.GetByUserIdAsync(userId, IncludeInactive);
            foreach (var dto in list)
                Items.Add(dto);
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
        var dto = new OperatorCompanyDto { Id = 0, UserId = _userId, CompanyId = 0, RoleId = null };
        var editVm = new OperatorCompanyEditViewModel(_repository, dto, isNew: true);
        var window = new OperatorCompanyEditWindow(editVm) { Owner = System.Windows.Application.Current.MainWindow };
        if (window.ShowDialog() == true)
            await LoadAsync(_userId);
    }

    private async Task EditAsync()
    {
        if (SelectedItem == null) return;
        var existing = await _repository.GetByIdAsync(SelectedItem.Id);
        if (existing == null)
        {
            System.Windows.MessageBox.Show("Rekord nie istnieje.", "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            await LoadAsync(_userId);
            return;
        }
        var editVm = new OperatorCompanyEditViewModel(_repository, existing, isNew: false);
        var window = new OperatorCompanyEditWindow(editVm) { Owner = System.Windows.Application.Current.MainWindow };
        if (window.ShowDialog() == true)
            await LoadAsync(_userId);
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
            await LoadAsync(_userId);
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
