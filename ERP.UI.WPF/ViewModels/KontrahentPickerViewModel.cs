using System.Collections.ObjectModel;
using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.UI.WPF.Services;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla KontrahentPicker – ładuje kontrahentów z kontrahenci_v.
/// </summary>
public class KontrahentPickerViewModel : ViewModelBase
{
    private readonly IKontrahenciQueryRepository _repo;
    private readonly IUserContext? _userContext;
    private int? _companyId;
    private string _searchText = string.Empty;
    private KontrahentLookupDto? _selectedKontrahent;

    public KontrahentPickerViewModel(IKontrahenciQueryRepository repo, IUserContext? userContext)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _userContext = userContext;
        Kontrahenci = new ObservableCollection<KontrahentLookupDto>();
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
    }

    public ObservableCollection<KontrahentLookupDto> Kontrahenci { get; }

    public int? CompanyId
    {
        get => _companyId;
        set
        {
            if (_companyId != value)
            {
                _companyId = value;
                OnPropertyChanged();
                _ = LoadAsync();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value ?? string.Empty;
                OnPropertyChanged();
                _ = LoadAsync();
            }
        }
    }

    public KontrahentLookupDto? SelectedKontrahent
    {
        get => _selectedKontrahent;
        set
        {
            if (_selectedKontrahent != value)
            {
                _selectedKontrahent = value;
                OnPropertyChanged();
                SelectedKontrahentChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? SelectedKontrahentChanged;

    public RelayCommand RefreshCommand { get; }

    public async Task LoadAsync()
    {
        var effectiveCompanyId = (_companyId.HasValue && _companyId.Value > 0)
            ? _companyId.Value
            : (_userContext?.CompanyId ?? 0);
        if (effectiveCompanyId <= 0)
        {
            Kontrahenci.Clear();
            return;
        }
        try
        {
            var items = await _repo.SearchAsync(
                effectiveCompanyId,
                string.IsNullOrWhiteSpace(_searchText) ? null : _searchText);

            Kontrahenci.Clear();
            foreach (var item in items)
                Kontrahenci.Add(item);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd ładowania kontrahentów: {ex.Message}",
                "Kontrahenci",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    public void SetSelectionById(int? id)
    {
        if (id == null)
        {
            SelectedKontrahent = null;
            return;
        }
        var found = Kontrahenci.FirstOrDefault(k => k.Id == id.Value);
        if (found != null)
            SelectedKontrahent = found;
    }
}
