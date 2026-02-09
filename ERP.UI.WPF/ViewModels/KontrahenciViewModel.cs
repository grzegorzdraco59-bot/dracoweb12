using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.UI.WPF.Services;
using ERP.UI.WPF.Views;
using IUserContext = ERP.UI.WPF.Services.IUserContext;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// Lista kontrahentów (kontrahenci_v) – tylko odczyt.
/// </summary>
public class KontrahenciViewModel : ViewModelBase
{
    private readonly IKontrahenciQueryRepository _repo;
    private readonly IUserContext _userContext;
    private readonly IKontrahenciCommandRepository _commandRepo;
    private readonly CollectionViewSource _kontrahenciViewSource;
    private string _searchText = string.Empty;
    private KontrahentLookupDto? _selectedKontrahent;
    private bool _isSelectionMode;

    public KontrahenciViewModel(
        IKontrahenciQueryRepository repo,
        IUserContext userContext,
        IKontrahenciCommandRepository commandRepo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _commandRepo = commandRepo ?? throw new ArgumentNullException(nameof(commandRepo));
        Kontrahenci = new ObservableCollection<KontrahentLookupDto>();
        _kontrahenciViewSource = new CollectionViewSource { Source = Kontrahenci };
        _kontrahenciViewSource.View.Filter = FilterKontrahenci;
        LoadCommand = new RelayCommand(async () => await LoadAsync());
        AddCommand = new RelayCommand(async () => await AddAsync());
        EditCommand = new RelayCommand(async () => await EditAsync(), () => SelectedKontrahent != null);
        DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedKontrahent != null);
        SelectCommand = new RelayCommand(ConfirmSelection, () => SelectedKontrahent != null);
        CloseCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));

        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Błąd podczas ładowania kontrahentów: {ex.Message}\n\n{ex.StackTrace}",
                    "Błąd",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        });
    }

    public ObservableCollection<KontrahentLookupDto> Kontrahenci { get; }

    public ICollectionView FilteredKontrahenci => _kontrahenciViewSource.View;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value ?? string.Empty;
                OnPropertyChanged();
                FilteredKontrahenci.Refresh();
            }
        }
    }

    public ICommand LoadCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SelectCommand { get; }
    public ICommand CloseCommand { get; }
    public event EventHandler? CloseRequested;
    public event EventHandler? SelectionConfirmed;

    public bool IsSelectionMode
    {
        get => _isSelectionMode;
        set
        {
            if (_isSelectionMode != value)
            {
                _isSelectionMode = value;
                OnPropertyChanged();
            }
        }
    }

    public KontrahentLookupDto? SelectedKontrahent
    {
        get => _selectedKontrahent;
        set
        {
            if (!ReferenceEquals(_selectedKontrahent, value))
            {
                _selectedKontrahent = value;
                OnPropertyChanged();
                if (EditCommand is RelayCommand editCmd)
                    editCmd.RaiseCanExecuteChanged();
                if (DeleteCommand is RelayCommand deleteCmd)
                    deleteCmd.RaiseCanExecuteChanged();
                if (SelectCommand is RelayCommand selectCmd)
                    selectCmd.RaiseCanExecuteChanged();
            }
        }
    }

    private bool FilterKontrahenci(object obj)
    {
        if (obj is not KontrahentLookupDto kontrahent)
            return false;

        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        var searchLower = SearchText.ToLowerInvariant();
        return (kontrahent.Nazwa?.ToLowerInvariant().Contains(searchLower) ?? false) ||
               (kontrahent.Email?.ToLowerInvariant().Contains(searchLower) ?? false);
    }

    private async Task LoadAsync()
    {
        if (!_userContext.CompanyId.HasValue)
        {
            MessageBox.Show(
                "Brak wybranej firmy. Wybierz firmę przed załadowaniem kontrahentów.",
                "Brak firmy",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var items = await _repo.GetAllForCompanyAsync(_userContext.CompanyId.Value);
        Kontrahenci.Clear();
        foreach (var item in items)
            Kontrahenci.Add(item);
        FilteredKontrahenci.Refresh();
    }

    private async Task AddAsync()
    {
        if (!_userContext.CompanyId.HasValue)
        {
            MessageBox.Show(
                "Brak wybranej firmy. Wybierz firmę przed dodaniem kontrahenta.",
                "Brak firmy",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var companyId = _userContext.CompanyId.Value;

        try
        {
            var newId = await _commandRepo.AddAsync(
                companyId,
                "O",
                "NOWY",
                null,
                null,
                null,
                "PLN");

            var kontrahent = await _commandRepo.GetByIdAsync(companyId, newId);
            if (kontrahent == null)
                return;

            var editVm = new KontrahentEditViewModel(_commandRepo, kontrahent);
            var editWindow = new KontrahentEditWindow(editVm)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                await LoadAsync();
                SelectedKontrahent = Kontrahenci.FirstOrDefault(k => k.KontrahentId == newId);
            }
            else
            {
                await LoadAsync();
                SelectedKontrahent = Kontrahenci.FirstOrDefault(k => k.KontrahentId == newId);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Błąd podczas dodawania kontrahenta: {ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ConfirmSelection()
    {
        if (SelectedKontrahent == null)
        {
            MessageBox.Show(
                "Wybierz kontrahenta z listy.",
                "Kontrahenci",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }
        SelectionConfirmed?.Invoke(this, EventArgs.Empty);
    }

    private async Task EditAsync()
    {
        if (SelectedKontrahent == null)
            return;

        if (!_userContext.CompanyId.HasValue)
        {
            MessageBox.Show(
                "Brak wybranej firmy. Wybierz firmę przed edycją kontrahenta.",
                "Brak firmy",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!SelectedKontrahent.KontrahentId.HasValue || SelectedKontrahent.KontrahentId.Value <= 0)
        {
            MessageBox.Show(
                "Brak kontrahent_id dla wybranego rekordu.",
                "Brak danych",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var companyId = _userContext.CompanyId.Value;
        var kontrahentId = SelectedKontrahent.KontrahentId.Value;
        try
        {
            var kontrahent = await _commandRepo.GetByIdAsync(companyId, kontrahentId);
            if (kontrahent == null)
            {
                MessageBox.Show(
                    $"Nie znaleziono kontrahenta id={kontrahentId}.",
                    "Brak danych",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var editVm = new KontrahentEditViewModel(_commandRepo, kontrahent);
            var editWindow = new KontrahentEditWindow(editVm)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                await LoadAsync();
                SelectedKontrahent = Kontrahenci.FirstOrDefault(k => k.KontrahentId == kontrahentId);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Błąd podczas edycji kontrahenta: {ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedKontrahent == null)
            return;

        if (!_userContext.CompanyId.HasValue)
        {
            MessageBox.Show(
                "Brak wybranej firmy. Wybierz firmę przed usunięciem kontrahenta.",
                "Brak firmy",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Usunąć kontrahenta: {SelectedKontrahent.Nazwa}?",
            "Potwierdzenie usunięcia",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
            return;

        if (!SelectedKontrahent.KontrahentId.HasValue || SelectedKontrahent.KontrahentId.Value <= 0)
        {
            MessageBox.Show(
                "Brak kontrahent_id dla wybranego rekordu.",
                "Brak danych",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var companyId = _userContext.CompanyId.Value;
        var kontrahentId = SelectedKontrahent.KontrahentId.Value;

        try
        {
            var used = await _commandRepo.IsUsedInDocumentsAsync(companyId, kontrahentId);
            if (used)
            {
                MessageBox.Show(
                    "Nie można usunąć – używany w dokumentach.",
                    "Blokada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            await _commandRepo.DeleteAsync(companyId, kontrahentId);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Błąd podczas usuwania kontrahenta: {ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
