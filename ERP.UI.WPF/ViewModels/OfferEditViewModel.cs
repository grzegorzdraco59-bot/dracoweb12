using System.Collections.ObjectModel;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.Domain.Enums;
using ERP.Application.Services;
using ERP.Domain.Entities;
using IUserContext = ERP.UI.WPF.Services.IUserContext;
using ERP.UI.WPF.Views;
using ERP.UI.WPF.Services;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla okna edycji oferty
/// </summary>
public class OfferEditViewModel : ViewModelBase
{
    private readonly IOfferService _offerService;
    private readonly IKontrahenciQueryRepository _kontrahenciRepository;
    private readonly IUserContext _userContext;
    private readonly OfferDto _originalOffer;
    private OfferDto _offer;
    private ObservableCollection<KontrahentLookupDto> _allKontrahenci = new();

    public OfferEditViewModel(
        IOfferService offerService, 
        IKontrahenciQueryRepository kontrahenciRepository, 
        OfferDto offer,
        IUserContext userContext)
    {
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
        _kontrahenciRepository = kontrahenciRepository ?? throw new ArgumentNullException(nameof(kontrahenciRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _originalOffer = offer ?? throw new ArgumentNullException(nameof(offer));
        
        // Tworzymy kopię do edycji – wartości domyślne dla null (spójność z DB NOT NULL, UI „Ceny”)
        _offer = new OfferDto
        {
            Id = offer.Id,
            CompanyId = offer.CompanyId,
            ForProforma = offer.ForProforma ?? false,
            ForOrder = offer.ForOrder ?? false,
            OfferDate = offer.OfferDate,
            FormattedOfferDate = offer.FormattedOfferDate ?? DateTime.Today.ToString("dd/MM/yyyy"),
            OfferNumber = offer.OfferNumber,
            CustomerId = offer.CustomerId,
            CustomerName = offer.CustomerName,
            CustomerStreet = offer.CustomerStreet,
            CustomerPostalCode = offer.CustomerPostalCode,
            CustomerCity = offer.CustomerCity,
            CustomerCountry = offer.CustomerCountry,
            CustomerNip = offer.CustomerNip,
            CustomerEmail = offer.CustomerEmail,
            RecipientName = offer.RecipientName,
            Currency = offer.Currency ?? "PLN",
            TotalPrice = offer.TotalPrice ?? 0m,
            VatRate = offer.VatRate ?? 23m,
            TotalVat = offer.TotalVat ?? 0m,
            TotalBrutto = offer.TotalBrutto ?? 0m,
            SumBrutto = offer.SumBrutto ?? 0m,
            OfferNotes = offer.OfferNotes,
            AdditionalData = offer.AdditionalData,
            Operator = offer.Operator ?? "",
            TradeNotes = offer.TradeNotes ?? "",
            ForInvoice = offer.ForInvoice,
            History = offer.History ?? "",
            Status = offer.Status ?? "Draft",
            CreatedAt = offer.CreatedAt,
            UpdatedAt = offer.UpdatedAt
        };

        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => CanSave());
        CancelCommand = new RelayCommand(() => OnCancelled());
        SelectRecipientCommand = new RelayCommand(async () => await SelectRecipientAsync());
        PickKontrahentCommand = new RelayCommand(PickKontrahent);
        
        _ = LoadKontrahenciAsync();
    }

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    // Właściwości do edycji
    public int Id => _offer.Id;
    public int CompanyId => _offer.CompanyId;

    public int? OfferNumber
    {
        get => _offer.OfferNumber;
        set
        {
            _offer.OfferNumber = value;
            OnPropertyChanged();
        }
    }

    public string? FormattedOfferDate
    {
        get => _offer.FormattedOfferDate;
        set
        {
            _offer.FormattedOfferDate = value;
            OnPropertyChanged();
        }
    }

    public string? CustomerName
    {
        get => _offer.CustomerName;
        set
        {
            _offer.CustomerName = value;
            OnPropertyChanged();
        }
    }

    public string? Currency
    {
        get => _offer.Currency;
        set
        {
            _offer.Currency = value;
            OnPropertyChanged();
        }
    }

    public decimal? TotalBrutto
    {
        get => _offer.TotalBrutto;
        set
        {
            _offer.TotalBrutto = value;
            OnPropertyChanged();
        }
    }

    public string? OfferNotes
    {
        get => _offer.OfferNotes;
        set
        {
            _offer.OfferNotes = value;
            OnPropertyChanged();
        }
    }

    public string TradeNotes
    {
        get => _offer.TradeNotes;
        set
        {
            _offer.TradeNotes = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public string Operator
    {
        get => _offer.Operator;
        set
        {
            _offer.Operator = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public bool? ForProforma
    {
        get => _offer.ForProforma;
        set
        {
            _offer.ForProforma = value;
            OnPropertyChanged();
        }
    }

    public bool? ForOrder
    {
        get => _offer.ForOrder;
        set
        {
            _offer.ForOrder = value;
            OnPropertyChanged();
        }
    }

    public bool ForInvoice
    {
        get => _offer.ForInvoice;
        set
        {
            _offer.ForInvoice = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Lista statusów oferty dla ComboBox.</summary>
    public string[] OfferStatuses { get; } = Enum.GetNames(typeof(OfferStatus));

    public string? Status
    {
        get => _offer.Status;
        set
        {
            _offer.Status = value;
            OnPropertyChanged();
        }
    }

    public int? CustomerId
    {
        get => _offer.CustomerId;
        set
        {
            _offer.CustomerId = value;
            OnPropertyChanged();
        }
    }

    public int? SelectedKontrahentId
    {
        get => _offer.CustomerId;
        set
        {
            _offer.CustomerId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CustomerId));
        }
    }

    public string? CustomerStreet
    {
        get => _offer.CustomerStreet;
        set
        {
            _offer.CustomerStreet = value;
            OnPropertyChanged();
        }
    }

    public string? CustomerPostalCode
    {
        get => _offer.CustomerPostalCode;
        set
        {
            _offer.CustomerPostalCode = value;
            OnPropertyChanged();
        }
    }

    public string? CustomerCity
    {
        get => _offer.CustomerCity;
        set
        {
            _offer.CustomerCity = value;
            OnPropertyChanged();
        }
    }

    public string? CustomerCountry
    {
        get => _offer.CustomerCountry;
        set
        {
            _offer.CustomerCountry = value;
            OnPropertyChanged();
        }
    }

    public string? CustomerNip
    {
        get => _offer.CustomerNip;
        set
        {
            _offer.CustomerNip = value;
            OnPropertyChanged();
        }
    }

    public string? CustomerEmail
    {
        get => _offer.CustomerEmail;
        set
        {
            _offer.CustomerEmail = value;
            OnPropertyChanged();
        }
    }

    public string? RecipientName
    {
        get => _offer.RecipientName;
        set
        {
            _offer.RecipientName = value;
            OnPropertyChanged();
        }
    }

    public string? SelectedKontrahentNazwa
    {
        get => _offer.RecipientName ?? _offer.CustomerName;
        set
        {
            _offer.RecipientName = value;
            _offer.CustomerName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RecipientName));
            OnPropertyChanged(nameof(CustomerName));
        }
    }

    public string? SelectedKontrahentEmail
    {
        get => _offer.CustomerEmail;
        set
        {
            _offer.CustomerEmail = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CustomerEmail));
        }
    }

    public string? SelectedKontrahentWaluta
    {
        get => _offer.Currency;
        set
        {
            _offer.Currency = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Currency));
        }
    }

    public decimal? TotalPrice
    {
        get => _offer.TotalPrice;
        set
        {
            _offer.TotalPrice = value;
            OnPropertyChanged();
        }
    }

    public decimal? VatRate
    {
        get => _offer.VatRate;
        set
        {
            _offer.VatRate = value;
            OnPropertyChanged();
        }
    }

    public decimal? TotalVat
    {
        get => _offer.TotalVat;
        set
        {
            _offer.TotalVat = value;
            OnPropertyChanged();
        }
    }

    public string? AdditionalData
    {
        get => _offer.AdditionalData;
        set
        {
            _offer.AdditionalData = value;
            OnPropertyChanged();
        }
    }

    public string History
    {
        get => _offer.History;
        set
        {
            _offer.History = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SelectRecipientCommand { get; }
    public ICommand PickKontrahentCommand { get; }

    private bool CanSave()
    {
        return true; // Zawsze można zapisać ofertę
    }

    private async Task SaveAsync()
    {
        try
        {
            if (!_userContext.CompanyId.HasValue)
            {
                System.Windows.MessageBox.Show(
                    "Brak wybranej firmy.",
                    "Błąd",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return;
            }

            // Pobierz encję z bazy
            var offer = await _offerService.GetByIdAsync(_offer.Id, _userContext.CompanyId.Value);
            if (offer == null)
            {
                System.Windows.MessageBox.Show(
                    "Oferta nie została znaleziona w bazie danych.",
                    "Błąd",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return;
            }

            // Konwersja daty z formatu dd/MM/yyyy na Clarion date
            int? offerDate = null;
            if (!string.IsNullOrWhiteSpace(FormattedOfferDate))
            {
                if (DateTime.TryParseExact(FormattedOfferDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
                {
                    var baseDate = new DateTime(1800, 12, 28);
                    offerDate = (int)(parsedDate - baseDate).TotalDays;
                }
            }

            // Aktualizujemy wszystkie pola
            offer.UpdateOfferInfo(offerDate, _offer.OfferNumber, _offer.Currency);
            // Używamy RecipientName jeśli jest ustawione, w przeciwnym razie CustomerName
            var recipientOrCustomerName = !string.IsNullOrWhiteSpace(_offer.RecipientName) ? _offer.RecipientName : _offer.CustomerName;
            offer.UpdateCustomerInfo(_offer.CustomerId, recipientOrCustomerName, _offer.CustomerStreet,
                _offer.CustomerPostalCode, _offer.CustomerCity, _offer.CustomerCountry, _offer.CustomerNip, _offer.CustomerEmail);
            offer.UpdatePricing(_offer.TotalPrice, _offer.VatRate, _offer.TotalVat, _offer.TotalBrutto);
            offer.UpdateFlags(_offer.ForProforma, _offer.ForOrder, _offer.ForInvoice);
            offer.UpdateNotes(_offer.OfferNotes, _offer.AdditionalData, _offer.TradeNotes);
            offer.UpdateHistory(_offer.History);
            var newStatus = OfferStatusMapping.FromDb(_offer.Status);
            offer.UpdateStatus(newStatus);

            await _offerService.UpdateAsync(offer);
            
            System.Windows.MessageBox.Show(
                "Oferta została zaktualizowana.",
                "Sukces",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);

            OnSaved();
        }
        catch (ERP.Domain.Exceptions.BusinessRuleException ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Reguła biznesowa",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas zapisywania oferty: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    protected virtual void OnSaved()
    {
        Saved?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnCancelled()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private async Task LoadKontrahenciAsync()
    {
        try
        {
            if (!_userContext.CompanyId.HasValue)
            {
                System.Windows.MessageBox.Show(
                    "Brak wybranej firmy.",
                    "Błąd",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return;
            }

            var kontrahenci = await _kontrahenciRepository.GetAllForCompanyAsync(_userContext.CompanyId.Value);
            _allKontrahenci.Clear();
            foreach (var kontrahent in kontrahenci)
            {
                _allKontrahenci.Add(kontrahent);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas ładowania kontrahentów: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task SelectRecipientAsync()
    {
        try
        {
            var selectionWindow = new CustomerSelectionWindow(_allKontrahenci)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (selectionWindow.ShowDialog() == true && selectionWindow.SelectedKontrahent != null)
            {
                var selectedCustomer = selectionWindow.SelectedKontrahent;
                
                // Przepisujemy nazwę kontrahenta
                RecipientName = selectedCustomer.Nazwa;
                
                // Przepisujemy możliwe pola do danych kontrahenta
                CustomerId = selectedCustomer.Id;
                CustomerCity = selectedCustomer.Miasto;
                CustomerEmail = selectedCustomer.Email;
                SelectedKontrahentWaluta = selectedCustomer.Waluta;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas wyboru kontrahenta: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
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
            SelectedKontrahentNazwa = selected.Nazwa;
            SelectedKontrahentEmail = selected.Email;
            SelectedKontrahentWaluta = selected.Waluta;
            CustomerId = selected.Id;
            CustomerStreet = selected.UlicaINr;
            CustomerPostalCode = selected.KodPocztowy;
            CustomerCity = selected.Miasto;
            CustomerCountry = selected.Panstwo;
            CustomerNip = selected.Nip;
            CustomerEmail = selected.Email;
        }
    }
}
