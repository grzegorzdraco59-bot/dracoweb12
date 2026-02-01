using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Repositories;
using IUserContext = ERP.UI.WPF.Services.IUserContext;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla widoku Faktury: lista faktur + pozycje wybranej faktury.
/// Wzorzec jak OffersViewModel: SearchText, FilteredInvoices (CollectionViewSource), toolbar, akcje w wierszu.
/// </summary>
public class InvoicesViewModel : ViewModelBase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IInvoicePositionRepository _invoicePositionRepository;
    private readonly IUserContext _userContext;
    private InvoiceDto? _selectedInvoice;
    private string _searchText = string.Empty;
    private readonly CollectionViewSource _invoicesViewSource;

    public InvoicesViewModel(
        IInvoiceRepository invoiceRepository,
        IInvoicePositionRepository invoicePositionRepository,
        IUserContext userContext)
    {
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _invoicePositionRepository = invoicePositionRepository ?? throw new ArgumentNullException(nameof(invoicePositionRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));

        Invoices = new ObservableCollection<InvoiceDto>();
        Positions = new ObservableCollection<InvoicePositionDto>();

        _invoicesViewSource = new CollectionViewSource { Source = Invoices };
        _invoicesViewSource.View.Filter = FilterInvoices;

        LoadInvoicesCommand = new RelayCommand(async () => await LoadInvoicesAsync());
        AddInvoiceCommand = new RelayCommand(AddInvoice);
        EditInvoiceCommand = new RelayCommand(EditSelectedInvoice, () => SelectedInvoice != null);
        DeleteInvoiceCommand = new RelayCommand(DeleteSelectedInvoice, () => SelectedInvoice != null);

        EditInvoiceCommandParam = new RelayCommandWithParameter(EditInvoiceByParam);
        ViewInvoiceCommandParam = new RelayCommandWithParameter(ViewInvoiceByParam);
        DeleteInvoiceCommandParam = new RelayCommandWithParameter(DeleteInvoiceByParam);

        _ = LoadInvoicesAsync();
    }

    public ObservableCollection<InvoiceDto> Invoices { get; }
    public ObservableCollection<InvoicePositionDto> Positions { get; }

    public ICollectionView FilteredInvoices => _invoicesViewSource.View;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value ?? string.Empty;
                OnPropertyChanged();
                FilteredInvoices.Refresh();
            }
        }
    }

    public InvoiceDto? SelectedInvoice
    {
        get => _selectedInvoice;
        set
        {
            if (_selectedInvoice != value)
            {
                _selectedInvoice = value;
                OnPropertyChanged();
                if (EditInvoiceCommand is RelayCommand editCmd)
                    editCmd.RaiseCanExecuteChanged();
                if (DeleteInvoiceCommand is RelayCommand delCmd)
                    delCmd.RaiseCanExecuteChanged();
                if (value != null)
                    _ = LoadPositionsAsync(value.Id);
                else
                    Positions.Clear();
            }
        }
    }

    public ICommand LoadInvoicesCommand { get; }
    public ICommand AddInvoiceCommand { get; }
    public ICommand EditInvoiceCommand { get; }
    public ICommand DeleteInvoiceCommand { get; }
    public ICommand EditInvoiceCommandParam { get; }
    public ICommand ViewInvoiceCommandParam { get; }
    public ICommand DeleteInvoiceCommandParam { get; }

    private bool FilterInvoices(object obj)
    {
        if (obj is not InvoiceDto inv)
            return false;
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;
        var searchLower = SearchText.ToLowerInvariant();
        return (inv.Id.ToString().Contains(searchLower)) ||
               (inv.FormattedDataFaktury?.ToLowerInvariant().Contains(searchLower) ?? false) ||
               (inv.SkrotNazwaFaktury?.ToLowerInvariant().Contains(searchLower) ?? false) ||
               (inv.NrFakturyText?.ToLowerInvariant().Contains(searchLower) ?? false) ||
               (inv.NrFaktury?.ToString().Contains(searchLower) ?? false) ||
               (inv.OdbiorcaNazwa?.ToLowerInvariant().Contains(searchLower) ?? false) ||
               (inv.Waluta?.ToLowerInvariant().Contains(searchLower) ?? false) ||
               (inv.KwotaBrutto?.ToString().Contains(searchLower) ?? false) ||
               (inv.Operator?.ToLowerInvariant().Contains(searchLower) ?? false);
    }

    private async Task LoadInvoicesAsync()
    {
        if (!_userContext.CompanyId.HasValue)
        {
            System.Windows.MessageBox.Show("Brak wybranej firmy.", "Faktury",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }
        try
        {
            var items = await _invoiceRepository.GetByCompanyIdAsync(_userContext.CompanyId.Value);
            Invoices.Clear();
            foreach (var item in items)
                Invoices.Add(item);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd ładowania faktur: {ex.Message}", "Faktury",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task LoadPositionsAsync(long invoiceId)
    {
        if (invoiceId <= 0)
        {
            Positions.Clear();
            return;
        }
        try
        {
            var items = await _invoicePositionRepository.GetByInvoiceIdAsync(invoiceId);
            Positions.Clear();
            foreach (var item in items)
                Positions.Add(item);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd ładowania pozycji: {ex.Message}", "Faktury",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void AddInvoice()
    {
        System.Windows.MessageBox.Show("Dodaj fakturę – funkcjonalność w przygotowaniu.", "Faktury",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private void EditSelectedInvoice()
    {
        if (SelectedInvoice != null)
            EditInvoiceByParam(SelectedInvoice);
    }

    private void EditInvoiceByParam(object? parameter)
    {
        if (parameter is InvoiceDto inv)
            System.Windows.MessageBox.Show($"Edytuj fakturę ID: {inv.Id} – funkcjonalność w przygotowaniu.", "Faktury",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private void ViewInvoiceByParam(object? parameter)
    {
        if (parameter is InvoiceDto inv)
            System.Windows.MessageBox.Show($"Podgląd faktury ID: {inv.Id} – funkcjonalność w przygotowaniu.", "Faktury",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private void DeleteSelectedInvoice()
    {
        if (SelectedInvoice != null)
            DeleteInvoiceByParam(SelectedInvoice);
    }

    private void DeleteInvoiceByParam(object? parameter)
    {
        if (parameter is not InvoiceDto inv)
            return;
        var result = System.Windows.MessageBox.Show(
            $"Czy na pewno chcesz usunąć fakturę ID: {inv.Id}?",
            "Potwierdzenie usunięcia",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (result == System.Windows.MessageBoxResult.Yes)
            System.Windows.MessageBox.Show("Usuń fakturę – funkcjonalność w przygotowaniu.", "Faktury",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
}
