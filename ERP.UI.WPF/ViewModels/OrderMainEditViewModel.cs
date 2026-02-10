using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Helpers;
using ERP.Application.Services;
using ERP.Domain.Enums;
using ERP.UI.WPF.Views;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla okna edycji nagłówka zamówienia (zamowienia) – zgodnie z Clarionem
/// </summary>
public class OrderMainEditViewModel : ViewModelBase
{
    private readonly IOrderMainService _orderMainService;
    private readonly OrderMainDto _order;

    public OrderMainEditViewModel(IOrderMainService orderMainService, OrderMainDto? order)
    {
        _orderMainService = orderMainService ?? throw new ArgumentNullException(nameof(orderMainService));
        IsNew = order == null || order.Id == 0;
        _order = IsNew
            ? (order ?? new OrderMainDto())
            : CopyFrom(order!);
        if (IsNew)
        {
            InitializeNewOrderDefaults();
        }
        WindowTitle = IsNew ? "Nowe zamówienie" : "Edycja zamówienia";

        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => CanSave());
        CancelCommand = new RelayCommand(() => OnCancelled());
        PobierzNbpCommand = new RelayCommand(() => PobierzNbp()); // placeholder – integracja NBP później
        PickKontrahentCommand = new RelayCommand(PickKontrahent);
    }

    private void InitializeNewOrderDefaults()
    {
        var today = DateTime.Today;
        var todayClarion = ClarionDateConverter.DateToClarionInt(today);
        _order.OrderDate = today;
        _order.DataZamowienia = today;
        _order.OrderDateInt = todayClarion;
        _order.SkopiowanoNiedostarczone = false;
        _order.SkopiowanoDoMagazynu = false;
    }

    private static OrderMainDto CopyFrom(OrderMainDto src)
    {
        return new OrderMainDto
        {
            Id = src.Id,
            CompanyId = src.CompanyId,
            OrderNumber = src.OrderNumber ?? src.Nr,
            Nr = src.Nr,
            OrderDateInt = src.OrderDateInt,
            OrderDate = src.OrderDate ?? src.DataZamowienia,
            DataZamowienia = src.DataZamowienia,
            DataDostawy = src.DataDostawy,
            SupplierId = src.SupplierId,
            SupplierName = src.SupplierName ?? src.DostawcaNazwa,
            SupplierEmail = src.SupplierEmail,
            Notes = src.Notes ?? src.Uwagi,
            Status = src.Status,
            StatusZamowienia = src.StatusZamowienia,
            StatusPlatnosci = src.StatusPlatnosci,
            NrFaktury = src.NrFaktury,
            DataFaktury = src.DataFaktury,
            DataPlatnosci = src.DataPlatnosci,
            Wartosc = src.Wartosc,
            DlaKogo = src.DlaKogo,
            Waluta = src.Waluta,
            TabelaNbp = src.TabelaNbp,
            DataTabeliNbp = src.DataTabeliNbp,
            Kurs = src.Kurs,
            SkopiowanoNiedostarczone = src.SkopiowanoNiedostarczone,
            SkopiowanoDoMagazynu = src.SkopiowanoDoMagazynu,
            Uwagi = src.Uwagi,
            DostawcaNazwa = src.DostawcaNazwa
        };
    }

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    public bool IsNew { get; }
    public string WindowTitle { get; }
    public int? SavedOrderId { get; private set; }

    public int Id => _order.Id;

    public int CompanyId { get => _order.CompanyId; set { _order.CompanyId = value; OnPropertyChanged(); } }

    public int? OrderNumber
    {
        get => _order.OrderNumber ?? _order.Nr;
        set { _order.OrderNumber = value; _order.Nr = value; OnPropertyChanged(); ((RelayCommand)SaveCommand).RaiseCanExecuteChanged(); }
    }

    public DateTime? OrderDate
    {
        get => _order.OrderDate ?? _order.DataZamowienia;
        set
        {
            _order.OrderDate = value;
            _order.DataZamowienia = value;
            _order.OrderDateInt = ClarionDateConverter.DateToClarionInt(value);
            OnPropertyChanged();
        }
    }

    public DateTime? DataDostawy { get => _order.DataDostawy; set { _order.DataDostawy = value; OnPropertyChanged(); } }

    public int? SupplierId { get => _order.SupplierId; set { _order.SupplierId = value; OnPropertyChanged(); } }

    public string? SupplierName
    {
        get => _order.SupplierName ?? _order.DostawcaNazwa;
        set { _order.SupplierName = value; _order.DostawcaNazwa = value; OnPropertyChanged(); }
    }

    public int? SelectedKontrahentId
    {
        get => _order.SupplierId;
        set
        {
            _order.SupplierId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SupplierId));
        }
    }

    public string? SelectedKontrahentNazwa
    {
        get => _order.SupplierName ?? _order.DostawcaNazwa;
        set
        {
            _order.SupplierName = value;
            _order.DostawcaNazwa = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SupplierName));
        }
    }

    public string? SelectedKontrahentEmail
    {
        get => _order.SupplierEmail;
        set
        {
            _order.SupplierEmail = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SupplierEmail));
        }
    }

    public string? SelectedKontrahentWaluta
    {
        get => _order.Waluta;
        set
        {
            _order.Waluta = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Waluta));
        }
    }

    public string? SupplierEmail { get => _order.SupplierEmail; set { _order.SupplierEmail = value; OnPropertyChanged(); } }

    public string? Notes
    {
        get => _order.Notes ?? _order.Uwagi;
        set { _order.Notes = value; _order.Uwagi = value; OnPropertyChanged(); }
    }

    public string? Status
    {
        get => _order.Status ?? OrderStatusMapping.ToDb(OrderStatus.Draft);
        set { _order.Status = value; OnPropertyChanged(); }
    }

    public string? StatusZamowienia { get => _order.StatusZamowienia; set { _order.StatusZamowienia = value; OnPropertyChanged(); } }
    public string? StatusPlatnosci { get => _order.StatusPlatnosci; set { _order.StatusPlatnosci = value; OnPropertyChanged(); } }
    public string? NrFaktury { get => _order.NrFaktury; set { _order.NrFaktury = value; OnPropertyChanged(); } }
    public DateTime? DataFaktury { get => _order.DataFaktury; set { _order.DataFaktury = value; OnPropertyChanged(); } }
    public DateTime? DataPlatnosci { get => _order.DataPlatnosci; set { _order.DataPlatnosci = value; OnPropertyChanged(); } }
    public decimal? Wartosc { get => _order.Wartosc; set { _order.Wartosc = value; OnPropertyChanged(); } }
    public string? DlaKogo { get => _order.DlaKogo; set { _order.DlaKogo = value; OnPropertyChanged(); } }
    public string? Waluta { get => _order.Waluta; set { _order.Waluta = value; OnPropertyChanged(); } }
    public string? TabelaNbp { get => _order.TabelaNbp; set { _order.TabelaNbp = value; OnPropertyChanged(); } }
    public string? DataTabeliNbp { get => _order.DataTabeliNbp; set { _order.DataTabeliNbp = value; OnPropertyChanged(); } }
    public decimal? Kurs { get => _order.Kurs; set { _order.Kurs = value; OnPropertyChanged(); } }
    public bool? SkopiowanoNiedostarczone { get => _order.SkopiowanoNiedostarczone; set { _order.SkopiowanoNiedostarczone = value; OnPropertyChanged(); } }
    public bool? SkopiowanoDoMagazynu { get => _order.SkopiowanoDoMagazynu; set { _order.SkopiowanoDoMagazynu = value; OnPropertyChanged(); } }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand PobierzNbpCommand { get; }
    public ICommand PickKontrahentCommand { get; }

    public OrderMainDto GetOrder() => _order;

    private bool CanSave() => CompanyId > 0;

    private void PobierzNbp()
    {
        System.Windows.MessageBox.Show("Integracja z API NBP – w przygotowaniu.", "Pobierz NBP",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private void PickKontrahent()
    {
        if (System.Windows.Application.Current is not App app)
            return;
        var viewModel = app.GetService<KontrahenciViewModel>();
        var window = new KontrahenciPickerWindow(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (window.ShowDialog() == true && window.SelectedKontrahent != null)
        {
            var selected = window.SelectedKontrahent;
            SelectedKontrahentId = selected.Id;
            SelectedKontrahentNazwa = selected.Nazwa;
            SelectedKontrahentEmail = selected.Email;
            SelectedKontrahentWaluta = selected.Waluta;
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            if (IsNew)
            {
                _order.CompanyId = CompanyId;
                var newId = await _orderMainService.AddAsync(_order);
                _order.Id = newId;
                SavedOrderId = newId;
            }
            else
            {
                await _orderMainService.UpdateAsync(_order);
                SavedOrderId = _order.Id;
            }
            OnSaved();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas zapisywania zamówienia: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    protected virtual void OnSaved() => Saved?.Invoke(this, EventArgs.Empty);
    protected virtual void OnCancelled() => Cancelled?.Invoke(this, EventArgs.Empty);
}
