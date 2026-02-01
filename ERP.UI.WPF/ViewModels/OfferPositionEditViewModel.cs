using System.Collections.ObjectModel;
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

    /// <summary>Przycisk Zapisz włączony gdy OfertaId &gt; 0, ilość &gt; 0, cena netto &gt;= 0. Walidacja szczegółowa w SaveAsync.</summary>
    private bool CanSave()
    {
        return _position.OfferId > 0
            && (_position.Ilosc ?? 0) > 0
            && (_position.CenaNetto ?? -1) >= 0;
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
        if (!_position.CenaNetto.HasValue || _position.CenaNetto.Value < 0)
        {
            System.Windows.MessageBox.Show("Cena netto musi być podana i nieujemna.", "Walidacja",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
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
                position = await _offerService.GetPositionByIdAsync((int)_position.Id);
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
            System.Windows.Application.Current.Dispatcher.Invoke(() => OnSaved());
        }
        catch (ERP.Domain.Exceptions.BusinessRuleException ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Reguła biznesowa",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            var text = ex.ToString();
            if (ex.InnerException != null)
                text += "\n\nInner: " + ex.InnerException.ToString();
            System.Windows.MessageBox.Show(text, "Błąd zapisu",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                _position.CenaNetto = selectedProduct.PricePln;
                
                OnPropertyChanged(nameof(ProductId));
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(NameEng));
                OnPropertyChanged(nameof(Unit));
                OnPropertyChanged(nameof(CenaNetto));
                OnPropertyChanged(nameof(QuantityTimesPrice));
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
