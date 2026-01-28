using System.Collections.ObjectModel;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Infrastructure.Repositories;
using ERP.UI.WPF.Views;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel listy powiązań operator–firma (nowy moduł OperatorCompany).
/// Zawsze po zapisie / usunięciu wykonuje pełny reload.
/// </summary>
public class OperatorCompanyListViewModel : ViewModelBase
{
    private readonly OperatorCompanyRepository _repository;
    private int _userId;
    private int _currentUserId;
    private OperatorCompanyDto? _selectedItem;

    public OperatorCompanyListViewModel(OperatorCompanyRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        Items = new ObservableCollection<OperatorCompanyDto>();

        LoadCommand = new RelayCommand(async () => await LoadAsync(CurrentUserId));
        AddCommand = new RelayCommand(async () => await AddAsync());
        EditCommand = new RelayCommand(async () => await EditAsync(), () => SelectedItem != null);
        DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedItem != null);
    }

    /// <summary>ID operatora do załadowania listy powiązań. Ustaw i wywołaj LoadCommand (Ładuj).</summary>
    public int CurrentUserId
    {
        get => _currentUserId;
        set => SetProperty(ref _currentUserId, value);
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
            if (DeleteCommand is RelayCommand delCmd) delCmd.RaiseCanExecuteChanged();
        }
    }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    /// <summary>
    /// Ładuje listę powiązań dla danego operatora. Po zapisie / usunięciu zawsze wywołaj ponownie.
    /// </summary>
    public async Task LoadAsync(int userId)
    {
        _userId = userId;
        Items.Clear();
        try
        {
            var list = await _repository.GetByUserIdAsync(userId);
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

    private async Task DeleteAsync()
    {
        if (SelectedItem == null) return;
        var result = System.Windows.MessageBox.Show(
            $"Czy na pewno usunąć powiązanie (ID {SelectedItem.Id})?",
            "Potwierdzenie",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (result != System.Windows.MessageBoxResult.Yes) return;
        try
        {
            await _repository.DeleteAsync(SelectedItem.Id);
            await LoadAsync(_userId);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd usuwania: {ex.Message}", "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
