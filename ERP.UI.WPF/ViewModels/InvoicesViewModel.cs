using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.Application.Services;
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
    private readonly IInvoiceCopyService _invoiceCopyService;
    private readonly IUserContext _userContext;
    private InvoiceDto? _selectedInvoice;
    private InvoicePositionDto? _selectedInvoiceItem;
    private string _searchText = string.Empty;
    private readonly CollectionViewSource _invoicesViewSource;

    public InvoicesViewModel(
        IInvoiceRepository invoiceRepository,
        IInvoicePositionRepository invoicePositionRepository,
        IInvoiceCopyService invoiceCopyService,
        IUserContext userContext)
    {
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _invoicePositionRepository = invoicePositionRepository ?? throw new ArgumentNullException(nameof(invoicePositionRepository));
        _invoiceCopyService = invoiceCopyService ?? throw new ArgumentNullException(nameof(invoiceCopyService));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));

        Invoices = new ObservableCollection<InvoiceDto>();
        Positions = new ObservableCollection<InvoicePositionDto>();

        _invoicesViewSource = new CollectionViewSource { Source = Invoices };
        _invoicesViewSource.View.Filter = FilterInvoices;

        LoadInvoicesCommand = new RelayCommand(async () => await LoadInvoicesAsync());
        AddInvoiceCommand = new RelayCommand(AddInvoice);
        EditInvoiceCommand = new RelayCommand(async () => await EditSelectedInvoiceAsync(), () => SelectedInvoice != null);
        DeleteInvoiceCommand = new RelayCommand(DeleteSelectedInvoice, () => SelectedInvoice != null);
        AddItemCommand = new RelayCommand(AddInvoiceItem, () => SelectedInvoice != null);
        EditItemCommand = new RelayCommand(EditInvoiceItem, () => SelectedInvoiceItem != null);
        DeleteItemCommand = new RelayCommand(DeleteInvoiceItem, () => SelectedInvoiceItem != null);

        SendInvoiceMailCommand = new RelayCommand(SendInvoiceMail, () => SelectedInvoice != null);
        PrintInvoicePdfCommand = new RelayCommand(PrintInvoicePdf, () => SelectedInvoice != null);
        CopyToFvzCommand = new RelayCommand(async () => await CopyToFvzAsync(), () => SelectedInvoice != null && SelectedInvoice.Id > 0);
        CopyToFvCommand = new RelayCommand(async () => await CopyToFvAsync(), () => SelectedInvoice != null && SelectedInvoice.Id > 0);

        EditInvoiceCommandParam = new RelayCommandWithParameter(EditInvoiceByParam);
        ViewInvoiceCommandParam = new RelayCommandWithParameter(ViewInvoiceByParam);
        DeleteInvoiceCommandParam = new RelayCommandWithParameter(DeleteInvoiceByParam);

        _ = LoadInvoicesAsync();
    }

    public ObservableCollection<InvoiceDto> Invoices { get; }
    public ObservableCollection<InvoicePositionDto> Positions { get; }
    public ObservableCollection<InvoicePositionDto> InvoiceItems => Positions;

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
                if (AddItemCommand is RelayCommand addItemCmd)
                    addItemCmd.RaiseCanExecuteChanged();
                if (SendInvoiceMailCommand is RelayCommand sendMailCmd)
                    sendMailCmd.RaiseCanExecuteChanged();
                if (PrintInvoicePdfCommand is RelayCommand printPdfCmd)
                    printPdfCmd.RaiseCanExecuteChanged();
                if (CopyToFvzCommand is RelayCommand copyFvzCmd)
                    copyFvzCmd.RaiseCanExecuteChanged();
                if (CopyToFvCommand is RelayCommand copyFvCmd)
                    copyFvCmd.RaiseCanExecuteChanged();
                SelectedInvoiceItem = null;
                if (value != null)
                    _ = LoadPositionsAsync(value.Id);
                else
                    Positions.Clear();
            }
        }
    }

    public InvoicePositionDto? SelectedInvoiceItem
    {
        get => _selectedInvoiceItem;
        set
        {
            if (_selectedInvoiceItem != value)
            {
                _selectedInvoiceItem = value;
                OnPropertyChanged();
                if (EditItemCommand is RelayCommand editItemCmd)
                    editItemCmd.RaiseCanExecuteChanged();
                if (DeleteItemCommand is RelayCommand deleteItemCmd)
                    deleteItemCmd.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand LoadInvoicesCommand { get; }
    public ICommand AddInvoiceCommand { get; }
    public ICommand EditInvoiceCommand { get; }
    public ICommand DeleteInvoiceCommand { get; }
    public ICommand SendInvoiceMailCommand { get; }
    public ICommand PrintInvoicePdfCommand { get; }
    public ICommand CopyToFvzCommand { get; }
    public ICommand CopyToFvCommand { get; }
    public ICommand EditInvoiceCommandParam { get; }
    public ICommand ViewInvoiceCommandParam { get; }
    public ICommand DeleteInvoiceCommandParam { get; }
    public ICommand AddItemCommand { get; }
    public ICommand EditItemCommand { get; }
    public ICommand DeleteItemCommand { get; }

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

            // Auto-zaznaczenie pierwszego rekordu po załadowaniu (MVVM)
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var first = FilteredInvoices.OfType<InvoiceDto>().FirstOrDefault();
                if (first != null)
                    SelectedInvoice = first;
            });
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
            var items = (await _invoicePositionRepository.GetByInvoiceIdAsync(invoiceId)).ToList();
            // Aktualizacja ObservableCollection musi być na wątku UI
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Positions.Clear();
                foreach (var item in items)
                    Positions.Add(item);
            });
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

    private void AddInvoiceItem()
    {
        try
        {
            if (SelectedInvoice == null)
            {
                System.Windows.MessageBox.Show("Wybierz fakturę, aby dodać pozycję.", "Pozycje faktury",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            System.Windows.MessageBox.Show("Dodaj pozycję faktury – funkcjonalność w przygotowaniu.", "Pozycje faktury",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd dodawania pozycji: {ex.Message}", "Pozycje faktury",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void EditInvoiceItem()
    {
        try
        {
            if (SelectedInvoiceItem == null)
            {
                System.Windows.MessageBox.Show("Wybierz pozycję do edycji.", "Pozycje faktury",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            System.Windows.MessageBox.Show($"Edytuj pozycję ID: {SelectedInvoiceItem.Id} – funkcjonalność w przygotowaniu.", "Pozycje faktury",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd edycji pozycji: {ex.Message}", "Pozycje faktury",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void DeleteInvoiceItem()
    {
        try
        {
            if (SelectedInvoiceItem == null)
            {
                System.Windows.MessageBox.Show("Wybierz pozycję do usunięcia.", "Pozycje faktury",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            var result = System.Windows.MessageBox.Show(
                $"Czy na pewno chcesz usunąć pozycję ID: {SelectedInvoiceItem.Id}?",
                "Potwierdzenie usunięcia",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);
            if (result != System.Windows.MessageBoxResult.Yes)
                return;
            System.Windows.MessageBox.Show("Usuń pozycję faktury – funkcjonalność w przygotowaniu.", "Pozycje faktury",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd usuwania pozycji: {ex.Message}", "Pozycje faktury",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task EditSelectedInvoiceAsync()
    {
        if (SelectedInvoice != null)
            await OpenInvoiceEditAsync(SelectedInvoice);
    }

    private void EditInvoiceByParam(object? parameter)
    {
        if (parameter is InvoiceDto inv)
            _ = OpenInvoiceEditAsync(inv);
    }

    private async Task OpenInvoiceEditAsync(InvoiceDto inv)
    {
        if (!_userContext.CompanyId.HasValue)
            return;
        try
        {
            var full = await _invoiceRepository.GetByIdAsync(inv.Id, _userContext.CompanyId.Value);
            if (full == null) return;
            var vm = new InvoiceEditViewModel(_invoiceRepository, full);
            var win = new ERP.UI.WPF.Views.InvoiceEditWindow(vm)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            if (win.ShowDialog() == true)
                await LoadInvoicesAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd edycji faktury: {ex.Message}", "Faktury",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
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

    private void SendInvoiceMail()
    {
        System.Windows.MessageBox.Show("Faktura mail – funkcjonalność w przygotowaniu.", "Faktury",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private void PrintInvoicePdf()
    {
        System.Windows.MessageBox.Show("Drukuj PDF faktury – funkcjonalność w przygotowaniu.", "Faktury",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private async Task CopyToFvzAsync()
    {
        if (SelectedInvoice == null || !_userContext.CompanyId.HasValue) return;
        try
        {
            var newId = await _invoiceCopyService.CopyInvoiceToFvzAsync(SelectedInvoice.Id, _userContext.CompanyId.Value);
            System.Windows.MessageBox.Show($"Skopiowano do FVZ. ID nowej faktury: {newId}", "Kopiuj do FVZ",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            await LoadInvoicesAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd: {ex.Message}", "Kopiuj do FVZ",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task CopyToFvAsync()
    {
        if (SelectedInvoice == null || !_userContext.CompanyId.HasValue) return;
        try
        {
            var newId = await _invoiceCopyService.CopyInvoiceToFvAsync(SelectedInvoice.Id, _userContext.CompanyId.Value);
            System.Windows.MessageBox.Show($"Skopiowano do FV. ID nowej faktury: {newId}", "Kopiuj do FV",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            await LoadInvoicesAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd: {ex.Message}", "Kopiuj do FV",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
