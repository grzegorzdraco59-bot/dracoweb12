using System.Collections.ObjectModel;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Services;
using ERP.Domain.Entities;
using IUserContext = ERP.UI.WPF.Services.IUserContext;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Repositories;
using ERP.UI.WPF.Views;
using ERP.UI.WPF.Services;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla okna edycji oferty
/// </summary>
public class OfferEditViewModel : ViewModelBase
{
    private readonly IOfferService _offerService;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserContext _userContext;
    private readonly OfferDto _originalOffer;
    private OfferDto _offer;
    private ObservableCollection<CustomerDto> _allCustomers = new();

    public OfferEditViewModel(
        IOfferService offerService, 
        ICustomerRepository customerRepository, 
        OfferDto offer,
        IUserContext userContext)
    {
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _originalOffer = offer ?? throw new ArgumentNullException(nameof(offer));
        
        // Tworzymy kopię do edycji
        _offer = new OfferDto
        {
            Id = offer.Id,
            CompanyId = offer.CompanyId,
            ForProforma = offer.ForProforma,
            ForOrder = offer.ForOrder,
            OfferDate = offer.OfferDate,
            FormattedOfferDate = offer.FormattedOfferDate,
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
            Currency = offer.Currency,
            TotalPrice = offer.TotalPrice,
            VatRate = offer.VatRate,
            TotalVat = offer.TotalVat,
            TotalBrutto = offer.TotalBrutto,
            OfferNotes = offer.OfferNotes,
            AdditionalData = offer.AdditionalData,
            Operator = offer.Operator,
            TradeNotes = offer.TradeNotes,
            ForInvoice = offer.ForInvoice,
            History = offer.History,
            CreatedAt = offer.CreatedAt,
            UpdatedAt = offer.UpdatedAt
        };

        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => CanSave());
        CancelCommand = new RelayCommand(() => OnCancelled());
        SelectRecipientCommand = new RelayCommand(async () => await SelectRecipientAsync());
        
        _ = LoadCustomersAsync();
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

    public int? CustomerId
    {
        get => _offer.CustomerId;
        set
        {
            _offer.CustomerId = value;
            OnPropertyChanged();
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

    private async Task LoadCustomersAsync()
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

            var customers = await _customerRepository.GetByCompanyIdAsync(_userContext.CompanyId.Value);
            _allCustomers.Clear();
            foreach (var customer in customers)
            {
                _allCustomers.Add(new CustomerDto
                {
                    Id = customer.Id,
                    CompanyId = customer.CompanyId,
                    Name = customer.Name,
                    Surname = customer.Surname,
                    FirstName = customer.FirstName,
                    Notes = customer.Notes,
                    Phone1 = customer.Phone1,
                    Phone2 = customer.Phone2,
                    Nip = customer.Nip,
                    Street = customer.Street,
                    PostalCode = customer.PostalCode,
                    City = customer.City,
                    Country = customer.Country,
                    ShippingStreet = customer.ShippingStreet,
                    ShippingPostalCode = customer.ShippingPostalCode,
                    ShippingCity = customer.ShippingCity,
                    ShippingCountry = customer.ShippingCountry,
                    Email1 = customer.Email1,
                    Email2 = customer.Email2,
                    Code = customer.Code,
                    Status = customer.Status,
                    Currency = customer.Currency,
                    CustomerType = customer.CustomerType,
                    OfferEnabled = customer.OfferEnabled,
                    VatStatus = customer.VatStatus,
                    Regon = customer.Regon,
                    FullAddress = customer.FullAddress,
                    CreatedAt = customer.CreatedAt,
                    UpdatedAt = customer.UpdatedAt
                });
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas ładowania odbiorców: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task SelectRecipientAsync()
    {
        try
        {
            var selectionWindow = new CustomerSelectionWindow(_allCustomers)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (selectionWindow.ShowDialog() == true && selectionWindow.SelectedCustomer != null)
            {
                var selectedCustomer = selectionWindow.SelectedCustomer;
                
                // Przepisujemy nazwę odbiorcy
                RecipientName = selectedCustomer.Name;
                
                // Przepisujemy możliwe pola do danych klienta
                CustomerId = selectedCustomer.Id;
                CustomerStreet = selectedCustomer.Street;
                CustomerPostalCode = selectedCustomer.PostalCode;
                CustomerCity = selectedCustomer.City;
                CustomerCountry = selectedCustomer.Country;
                CustomerNip = selectedCustomer.Nip;
                CustomerEmail = selectedCustomer.Email1;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas wyboru odbiorcy: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
}
