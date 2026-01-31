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

    public OfferPositionEditViewModel(
        IOfferService offerService, 
        ProductRepository productRepository, 
        OfferPositionDto position,
        IUserContext userContext)
    {
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _originalPosition = position ?? throw new ArgumentNullException(nameof(position));
        
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
            Quantity = position.Quantity,
            Price = position.Price,
            Discount = position.Discount,
            PriceAfterDiscount = position.PriceAfterDiscount,
            PriceAfterDiscountAndQuantity = position.PriceAfterDiscountAndQuantity,
            VatRate = position.VatRate,
            Vat = position.Vat,
            PriceBrutto = position.PriceBrutto,
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

    // Właściwości do edycji
    public int Id => _position.Id;
    public int OfferId => _position.OfferId;

    public int? ProductId
    {
        get => _position.ProductId;
        set
        {
            _position.ProductId = value;
            OnPropertyChanged();
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

    public decimal? Quantity
    {
        get => _position.Quantity;
        set
        {
            _position.Quantity = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(QuantityTimesPrice));
            CalculatePrices();
        }
    }

    public decimal? Price
    {
        get => _position.Price;
        set
        {
            _position.Price = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(QuantityTimesPrice));
            CalculatePrices();
        }
    }

    public decimal? Discount
    {
        get => _position.Discount;
        set
        {
            _position.Discount = value;
            OnPropertyChanged();
            CalculatePrices();
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
            CalculatePrices();
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

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Name);
    }

    private void CalculatePrices()
    {
        // Obliczanie ceny po rabacie
        if (Price.HasValue && Discount.HasValue)
        {
            _position.PriceAfterDiscount = Price.Value * (1 - Discount.Value / 100);
        }
        else if (Price.HasValue)
        {
            _position.PriceAfterDiscount = Price.Value;
        }

        // Obliczanie ceny po rabacie * ilość
        if (_position.PriceAfterDiscount.HasValue && Quantity.HasValue)
        {
            _position.PriceAfterDiscountAndQuantity = _position.PriceAfterDiscount.Value * Quantity.Value;
        }

        // Obliczanie VAT i brutto
        if (_position.PriceAfterDiscountAndQuantity.HasValue && VatRate != null && decimal.TryParse(VatRate, out var vatRateDecimal))
        {
            _position.Vat = _position.PriceAfterDiscountAndQuantity.Value * vatRateDecimal / 100;
            _position.PriceBrutto = _position.PriceAfterDiscountAndQuantity.Value + _position.Vat;
        }

        OnPropertyChanged(nameof(PriceAfterDiscount));
        OnPropertyChanged(nameof(PriceAfterDiscountAndQuantity));
        OnPropertyChanged(nameof(Vat));
        OnPropertyChanged(nameof(PriceBrutto));
        OnPropertyChanged(nameof(QuantityTimesPrice));
    }

    public decimal? PriceAfterDiscount => _position.PriceAfterDiscount;
    public decimal? PriceAfterDiscountAndQuantity => _position.PriceAfterDiscountAndQuantity;
    public decimal? Vat => _position.Vat;
    public decimal? PriceBrutto => _position.PriceBrutto;
    
    /// <summary>
    /// Wylicza ilość * cena (sztuki * cena)
    /// </summary>
    public decimal? QuantityTimesPrice
    {
        get
        {
            if (Quantity.HasValue && Price.HasValue)
            {
                return Quantity.Value * Price.Value;
            }
            return null;
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            // Sprawdzamy, czy to nowa pozycja (ID = 0) czy edycja istniejącej
            bool isNewPosition = _position.Id == 0;
            
            OfferPosition position;
            
            if (isNewPosition)
            {
                // Tworzymy nową pozycję
                var companyId = _userContext.CompanyId 
                    ?? throw new InvalidOperationException("Brak wybranej firmy. Użytkownik musi być zalogowany i wybrać firmę.");
                
                position = new OfferPosition(companyId, _position.OfferId, _position.Unit ?? "szt");
            }
            else
            {
                // Pobierz istniejącą encję z bazy
                position = await _offerService.GetPositionByIdAsync(_position.Id);
                if (position == null)
                {
                    System.Windows.MessageBox.Show(
                        "Pozycja oferty nie została znaleziona w bazie danych.",
                        "Błąd",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                    return;
                }
            }

            // Aktualizujemy wszystkie pola
            position.UpdateProductInfo(_position.ProductId, _position.SupplierId, _position.ProductCode, _position.Name, _position.NameEng);
            position.UpdateUnits(_position.Unit ?? "szt", _position.UnitEng);
            position.UpdatePricing(_position.Quantity, _position.Price, _position.Discount, 
                _position.PriceAfterDiscount, _position.PriceAfterDiscountAndQuantity);
            position.UpdateVatInfo(_position.VatRate, _position.Vat, _position.PriceBrutto);
            position.UpdateNotes(_position.OfferNotes, _position.InvoiceNotes, _position.Other1);
            if (_position.GroupNumber.HasValue)
            {
                position.UpdateGroupNumber(_position.GroupNumber);
            }

            if (isNewPosition)
            {
                // Dodajemy nową pozycję do bazy
                var newId = await _offerService.AddPositionAsync(position);
                // Aktualizujemy ID w DTO, aby w razie potrzeby można było ponownie edytować
                _position.Id = newId;
            }
            else
            {
                // Aktualizujemy istniejącą pozycję
                await _offerService.UpdatePositionAsync(position);
            }
            
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
                $"Błąd podczas zapisywania pozycji oferty: {ex.Message}",
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

    private async Task SelectProductAsync()
    {
        try
        {
            // Upewniamy się, że produkty są załadowane
            if (_allProducts.Count == 0)
            {
                await LoadProductsAsync();
            }

            var productSelectionWindow = new ProductSelectionWindow(_allProducts)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (productSelectionWindow.ShowDialog() == true && productSelectionWindow.SelectedProduct != null)
            {
                var selectedProduct = productSelectionWindow.SelectedProduct;
                
                _position.ProductId = selectedProduct.Id;
                _position.Name = selectedProduct.NamePl;
                _position.NameEng = selectedProduct.NameEng;
                _position.Unit = selectedProduct.Unit ?? "szt"; // jednostki_sprzedazy -> jednostki
                _position.Price = selectedProduct.PricePln; // cena_sprzedazy -> cena
                
                OnPropertyChanged(nameof(ProductId));
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(NameEng));
                OnPropertyChanged(nameof(Unit));
                OnPropertyChanged(nameof(Price));
                CalculatePrices(); // Przeliczamy ceny po zmianie ceny i jednostek
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
