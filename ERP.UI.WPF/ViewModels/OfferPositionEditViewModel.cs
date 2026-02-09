using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using System.Windows;
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
/// ViewModel dla okna edycji pozycji oferty
/// </summary>
public class OfferPositionEditViewModel : ViewModelBase
{
    private readonly IOfferService _offerService;
    private readonly ProductRepository _productRepository;
    private readonly IUserContext _userContext;
    private readonly OfferPositionDto _originalPosition;
    private OfferPositionDto _position;
    private readonly ObservableCollection<ProductDto> _allProducts;
    private readonly string _offerCurrency;

    public OfferPositionEditViewModel(
        IOfferService offerService, 
        ProductRepository productRepository, 
        OfferPositionDto position,
        IUserContext userContext,
        string offerCurrency)
    {
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _originalPosition = position ?? throw new ArgumentNullException(nameof(position));
        _offerCurrency = (offerCurrency ?? "").Trim().ToUpperInvariant();
        
        // Tworzymy kopię do edycji
        _position = new OfferPositionDto
        {
            Id = position.Id,
            CompanyId = position.CompanyId,
            OfferId = position.OfferId,
            ProductId = position.ProductId,
            SupplierId = position.SupplierId,
            ProductCode = position.ProductCode,
            Name = position.Name,
            NameEng = position.NameEng,
            Unit = position.Unit,
            UnitEng = position.UnitEng,
            Ilosc = position.Ilosc,
            CenaNetto = position.CenaNetto,
            Discount = position.Discount,
            PriceAfterDiscount = position.PriceAfterDiscount,
            NettoPoz = position.NettoPoz,
            VatRate = position.VatRate,
            VatPoz = position.VatPoz,
            BruttoPoz = position.BruttoPoz,
            OfferNotes = position.OfferNotes,
            InvoiceNotes = position.InvoiceNotes,
            Other1 = position.Other1,
            GroupNumber = position.GroupNumber,
            CreatedAt = position.CreatedAt,
            UpdatedAt = position.UpdatedAt
        };

        _allProducts = new ObservableCollection<ProductDto>();
        
        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => CanSave());
        CancelCommand = new RelayCommand(() => OnCancelled());
        SelectProductCommand = new RelayCommand(async () => await SelectProductAsync());
        
        // Ładujemy listę produktów asynchronicznie
        _ = LoadProductsAsync();
    }

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    // Właściwości do edycji (mapowane na ofertypozycje.id i ofertypozycje.oferta_id)
    public long Id => _position.Id;
    public long OfferId => _position.OfferId;

    public int? ProductId
    {
        get => _position.ProductId;
        set
        {
            _position.ProductId = value;
            OnPropertyChanged();
            if (SaveCommand is RelayCommand saveCmd)
                saveCmd.RaiseCanExecuteChanged();
        }
    }

    public int? SupplierId
    {
        get => _position.SupplierId;
        set
        {
            _position.SupplierId = value;
            OnPropertyChanged();
        }
    }

    public string? ProductCode
    {
        get => _position.ProductCode;
        set
        {
            _position.ProductCode = value;
            OnPropertyChanged();
        }
    }

    public string? Name
    {
        get => _position.Name;
        set
        {
            _position.Name = value;
            OnPropertyChanged();
            if (SaveCommand is RelayCommand saveCmd)
                saveCmd.RaiseCanExecuteChanged();
        }
    }

    public string? NameEng
    {
        get => _position.NameEng;
        set
        {
            _position.NameEng = value;
            OnPropertyChanged();
        }
    }

    public decimal? Ilosc
    {
        get => _position.Ilosc;
        set
        {
            _position.Ilosc = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(QuantityTimesPrice));
            if (SaveCommand is RelayCommand saveCmd)
                saveCmd.RaiseCanExecuteChanged();
        }
    }

    public decimal? CenaNetto
    {
        get => _position.CenaNetto;
        set
        {
            _position.CenaNetto = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(QuantityTimesPrice));
            if (SaveCommand is RelayCommand saveCmd)
                saveCmd.RaiseCanExecuteChanged();
        }
    }

    public decimal? Discount
    {
        get => _position.Discount;
        set
        {
            _position.Discount = value;
            OnPropertyChanged();
        }
    }

    public string Unit
    {
        get => _position.Unit;
        set
        {
            _position.Unit = value ?? "szt";
            OnPropertyChanged();
        }
    }

    public string? UnitEng
    {
        get => _position.UnitEng;
        set
        {
            _position.UnitEng = value;
            OnPropertyChanged();
        }
    }

    public string? VatRate
    {
        get => _position.VatRate;
        set
        {
            _position.VatRate = value;
            OnPropertyChanged();
        }
    }

    public string? OfferNotes
    {
        get => _position.OfferNotes;
        set
        {
            _position.OfferNotes = value;
            OnPropertyChanged();
        }
    }

    public string? InvoiceNotes
    {
        get => _position.InvoiceNotes;
        set
        {
            _position.InvoiceNotes = value;
            OnPropertyChanged();
        }
    }

    public string? Other1
    {
        get => _position.Other1;
        set
        {
            _position.Other1 = value;
            OnPropertyChanged();
        }
    }

    public decimal? GroupNumber
    {
        get => _position.GroupNumber;
        set
        {
            _position.GroupNumber = value;
            OnPropertyChanged();
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SelectProductCommand { get; }

    /// <summary>Przycisk Zapisz aktywny gdy: wybrany towar + oferta. Walidacja waluty/ceny tylko w Execute.</summary>
    private bool CanSave()
    {
        return _position.OfferId > 0
            && _position.ProductId.HasValue
            && _position.ProductId > 0;
    }

    private static bool IsValidCurrency(string currency)
    {
        return currency == "PLN" || currency == "EUR" || currency == "USD";
    }

    private decimal? GetPriceForCurrency(ProductDto product)
    {
        if (product == null || !IsValidCurrency(_offerCurrency))
            return product?.PricePln;
        return _offerCurrency switch
        {
            "PLN" => product.PricePln,
            "EUR" => product.PriceEur,
            "USD" => product.PriceUsd,
            _ => product.PricePln
        };
    }

    private decimal? GetNettoForCurrentProduct()
    {
        if (!_position.ProductId.HasValue || _position.ProductId <= 0)
            return null;
        if (!IsValidCurrency(_offerCurrency))
            return null;
        var product = _allProducts.FirstOrDefault(p => p.Id == _position.ProductId);
        if (product == null)
            return null;
        return _offerCurrency switch
        {
            "PLN" => product.PricePln,
            "EUR" => product.PriceEur,
            "USD" => product.PriceUsd,
            _ => null
        };
    }

    // Kwoty (netto_poz, vat_poz, brutto_poz) liczone w serwisie/DB – UI tylko pokazuje (zgodnie z zasadą architektoniczną).
    public decimal? PriceAfterDiscount => _position.PriceAfterDiscount;
    public decimal? NettoPoz => _position.NettoPoz;
    public decimal? VatPoz => _position.VatPoz;
    public decimal? BruttoPoz => _position.BruttoPoz;
    
    /// <summary>Ilość × cena netto (jednostkowa) – tylko podgląd, nie używane do zapisu.</summary>
    public decimal? QuantityTimesPrice
    {
        get
        {
            if (Ilosc.HasValue && CenaNetto.HasValue)
            {
                return Ilosc.Value * CenaNetto.Value;
            }
            return null;
        }
    }

    private async Task SaveAsync()
    {
        System.Windows.MessageBox.Show("SAVE_EXECUTE_START");
        Debug.WriteLine("SAVE_POS_START");
        try
        {
            // Walidacja przed zapisem
            if (_position.OfferId <= 0)
            {
                System.Windows.MessageBox.Show("Nieprawidłowy identyfikator oferty (OfertaId > 0).", "Walidacja",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (!(_position.Ilosc.HasValue && _position.Ilosc.Value > 0))
            {
                System.Windows.MessageBox.Show("Ilość musi być większa od zera.", "Walidacja",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (!(_position.ProductId.HasValue && _position.ProductId > 0))
            {
                System.Windows.MessageBox.Show("Wybierz towar.", "Walidacja",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Waluta oferty → cena netto z towaru (cena_PLN / cena_EUR / cena_USD)
            var currency = _offerCurrency;
            if (!IsValidCurrency(currency))
            {
                System.Windows.MessageBox.Show(
                    $"Nieznana waluta oferty: {(string.IsNullOrEmpty(currency) ? "(pusta)" : currency)}",
                    "Brak ceny",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var product = _allProducts.FirstOrDefault(p => p.Id == _position.ProductId);
            if (product == null)
            {
                System.Windows.MessageBox.Show("Towar nie został znaleziony na liście. Odśwież listę produktów.",
                    "Brak ceny",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            decimal? netto = currency switch
            {
                "PLN" => product.PricePln,
                "EUR" => product.PriceEur,
                "USD" => product.PriceUsd,
                _ => null
            };

            if (!netto.HasValue || netto.Value <= 0)
            {
                System.Windows.MessageBox.Show(
                    $"Brak ceny dla waluty {currency}. Uzupełnij cenę w towarze (cena_PLN/EUR/USD).",
                    "Brak ceny",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            _position.CenaNetto = netto.Value;
            OnPropertyChanged(nameof(CenaNetto));
            OnPropertyChanged(nameof(QuantityTimesPrice));

            bool isNewPosition = _position.Id == 0;
            OfferPosition position;

            if (isNewPosition)
            {
                var companyId = _userContext.CompanyId
                    ?? throw new InvalidOperationException("Brak wybranej firmy. Użytkownik musi być zalogowany i wybrać firmę.");
                position = new OfferPosition(companyId, (int)_position.OfferId, _position.Unit ?? "szt");
            }
            else
            {
                var positionOrNull = await _offerService.GetPositionByIdAsync((int)_position.Id);
                if (positionOrNull == null)
                {
                    System.Windows.MessageBox.Show(
                        "Pozycja oferty nie została znaleziona w bazie danych.",
                        "Błąd",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                    return;
                }
                position = positionOrNull;
            }

            position.UpdateProductInfo(_position.ProductId, _position.SupplierId, _position.ProductCode, _position.Name, _position.NameEng);
            position.UpdateUnits(_position.Unit ?? "szt", _position.UnitEng);
            position.UpdatePricing(_position.Ilosc, _position.CenaNetto, _position.Discount,
                _position.PriceAfterDiscount, _position.NettoPoz);
            position.UpdateVatInfo(_position.VatRate, _position.VatPoz, _position.BruttoPoz);
            position.UpdateNotes(_position.OfferNotes, _position.InvoiceNotes, _position.Other1);
            if (_position.GroupNumber.HasValue)
                position.UpdateGroupNumber(_position.GroupNumber);

            if (isNewPosition)
            {
                var newId = await _offerService.AddPositionAsync(position);
                _position.Id = (long)newId;
            }
            else
            {
                await _offerService.UpdatePositionAsync(position);
            }

            // Zamykanie okna (DialogResult + Close) musi być na wątku UI
            Debug.WriteLine("SAVE_POS_OK");
            System.Windows.Application.Current.Dispatcher.Invoke(() => OnSaved());
        }
        catch (ERP.Domain.Exceptions.BusinessRuleException ex)
        {
            Debug.WriteLine("SAVE_POS_ERR: " + ex.ToString());
            System.Windows.MessageBox.Show(ex.Message, "Reguła biznesowa",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("SAVE_POS_ERR: " + ex.ToString());
            var text = ex.ToString();
            if (ex.InnerException != null)
                text += "\n\nInner: " + ex.InnerException.ToString();
            System.Windows.MessageBox.Show(text, "Błąd zapisu",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            throw;
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

    private async Task SelectProductAsync()
    {
        try
        {
            // Upewniamy się, że produkty są załadowane
            if (_allProducts.Count == 0)
            {
                await LoadProductsAsync();
            }

            var companyId = _userContext.CompanyId ?? 0;
            var productSelectionWindow = new ProductSelectionWindow(_allProducts, _productRepository, companyId)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (productSelectionWindow.ShowDialog() == true && productSelectionWindow.SelectedProduct != null)
            {
                var selectedProduct = productSelectionWindow.SelectedProduct;

                _position.ProductId = selectedProduct.Id;
                _position.Name = selectedProduct.NamePl;
                _position.NameEng = selectedProduct.NameEng;
                _position.Unit = selectedProduct.Unit ?? "szt";
                _position.CenaNetto = GetPriceForCurrency(selectedProduct);

                OnPropertyChanged(nameof(ProductId));
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(NameEng));
                OnPropertyChanged(nameof(Unit));
                OnPropertyChanged(nameof(CenaNetto));
                OnPropertyChanged(nameof(QuantityTimesPrice));
                if (SaveCommand is RelayCommand saveCmd)
                    saveCmd.RaiseCanExecuteChanged();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas wyboru produktu: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            _allProducts.Clear();
            var products = await _productRepository.GetAllAsync();
            
            foreach (var product in products)
            {
                _allProducts.Add(product);
            }
            if (SaveCommand is RelayCommand saveCmd)
                saveCmd.RaiseCanExecuteChanged();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas ładowania listy produktów: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
}
