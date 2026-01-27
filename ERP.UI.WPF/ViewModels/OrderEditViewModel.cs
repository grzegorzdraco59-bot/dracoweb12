using System.Collections.ObjectModel;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Services;
using ERP.Infrastructure.Repositories;
using ERP.UI.WPF.Views;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla okna edycji zamówienia
/// </summary>
public class OrderEditViewModel : ViewModelBase
{
    private readonly IOrderService _orderService;
    private readonly ProductRepository _productRepository;
    private readonly OrderDto _originalOrder;
    private OrderDto _order;
    private ProductDto? _selectedProduct;
    private readonly ObservableCollection<ProductDto> _allProducts;

    public OrderEditViewModel(IOrderService orderService, ProductRepository productRepository, OrderDto order)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _originalOrder = order ?? throw new ArgumentNullException(nameof(order));
        
        // Tworzymy kopię do edycji
        _order = new OrderDto
        {
            Id = order.Id,
            CompanyId = order.CompanyId,
            OrderNumberInt = order.OrderNumberInt,
            OrderDateInt = order.OrderDateInt,
            OrderDate = order.OrderDate,
            SupplierId = order.SupplierId,
            SupplierName = order.SupplierName,
            SupplierEmail = order.SupplierEmail,
            SupplierCurrency = order.SupplierCurrency,
            ProductId = order.ProductId,
            ProductNameDraco = order.ProductNameDraco,
            ProductName = order.ProductName,
            ProductStatus = order.ProductStatus,
            PurchaseUnit = order.PurchaseUnit,
            SalesUnit = order.SalesUnit,
            PurchasePrice = order.PurchasePrice,
            ConversionFactor = order.ConversionFactor,
            Quantity = order.Quantity,
            Notes = order.Notes,
            Status = order.Status,
            SentToOrder = order.SentToOrder,
            Delivered = order.Delivered,
            QuantityInPackage = order.QuantityInPackage,
            VatRate = order.VatRate,
            Operator = order.Operator,
            ScannerOrderNumber = order.ScannerOrderNumber,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };

        _allProducts = new ObservableCollection<ProductDto>();
        Products = new ObservableCollection<ProductDto>();
        
        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => CanSave());
        CancelCommand = new RelayCommand(() => OnCancelled());
        SelectProductCommand = new RelayCommand(async () => await SelectProductAsync());
        
        // Ładujemy listę produktów asynchronicznie
        _ = LoadProductsAsync();
    }

    /// <summary>
    /// Sprawdza, czy to jest nowe zamówienie (dodawanie) czy edycja istniejącego
    /// </summary>
    public bool IsNewOrder => _order.Id == 0;

    /// <summary>
    /// Tytuł okna - różny dla dodawania i edycji
    /// </summary>
    public string WindowTitle => IsNewOrder ? "Dodawanie zamówienia" : "Edycja zamówienia";

    /// <summary>
    /// Nagłówek w oknie - różny dla dodawania i edycji
    /// </summary>
    public string HeaderText => IsNewOrder ? "Dodawanie zamówienia" : "Edycja zamówienia hala";

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    // Właściwości do edycji
    public int Id => _order.Id;
    public int CompanyId => _order.CompanyId;

    public ObservableCollection<ProductDto> Products { get; }

    public ProductDto? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            _selectedProduct = value;
            OnPropertyChanged();
        }
    }

    public int? OrderNumberInt
    {
        get => _order.OrderNumberInt;
        set
        {
            _order.OrderNumberInt = value;
            OnPropertyChanged();
        }
    }

    public DateTime? OrderDate
    {
        get => _order.OrderDate;
        set
        {
            _order.OrderDate = value;
            if (value.HasValue)
            {
                _order.OrderDateInt = (int)(value.Value - new DateTime(1800, 12, 28)).TotalDays;
            }
            OnPropertyChanged();
        }
    }

    public int? SupplierId
    {
        get => _order.SupplierId;
        set
        {
            _order.SupplierId = value;
            OnPropertyChanged();
        }
    }

    public string? SupplierName
    {
        get => _order.SupplierName;
        set
        {
            _order.SupplierName = value;
            OnPropertyChanged();
        }
    }

    public string? SupplierEmail
    {
        get => _order.SupplierEmail;
        set
        {
            _order.SupplierEmail = value;
            OnPropertyChanged();
        }
    }

    public string? SupplierCurrency
    {
        get => _order.SupplierCurrency;
        set
        {
            _order.SupplierCurrency = value;
            OnPropertyChanged();
        }
    }

    public int? ProductId
    {
        get => _order.ProductId;
        set
        {
            _order.ProductId = value;
            OnPropertyChanged();
        }
    }

    public string? ProductNameDraco
    {
        get => _order.ProductNameDraco;
        set
        {
            _order.ProductNameDraco = value;
            OnPropertyChanged();
        }
    }

    public string? ProductName
    {
        get => _order.ProductName;
        set
        {
            _order.ProductName = value;
            OnPropertyChanged();
        }
    }

    public string? ProductStatus
    {
        get => _order.ProductStatus;
        set
        {
            _order.ProductStatus = value;
            OnPropertyChanged();
        }
    }

    public string? PurchaseUnit
    {
        get => _order.PurchaseUnit;
        set
        {
            _order.PurchaseUnit = value;
            OnPropertyChanged();
        }
    }

    public string? SalesUnit
    {
        get => _order.SalesUnit;
        set
        {
            _order.SalesUnit = value;
            OnPropertyChanged();
        }
    }

    public decimal? PurchasePrice
    {
        get => _order.PurchasePrice;
        set
        {
            _order.PurchasePrice = value;
            OnPropertyChanged();
        }
    }

    public decimal? ConversionFactor
    {
        get => _order.ConversionFactor;
        set
        {
            _order.ConversionFactor = value;
            OnPropertyChanged();
        }
    }

    public decimal? Quantity
    {
        get => _order.Quantity;
        set
        {
            _order.Quantity = value;
            OnPropertyChanged();
        }
    }

    public string? Notes
    {
        get => _order.Notes;
        set
        {
            _order.Notes = value;
            OnPropertyChanged();
        }
    }

    public int? Status
    {
        get => _order.Status;
        set
        {
            _order.Status = value;
            OnPropertyChanged();
        }
    }

    public bool? SentToOrder
    {
        get => _order.SentToOrder;
        set
        {
            _order.SentToOrder = value;
            OnPropertyChanged();
        }
    }

    public bool? Delivered
    {
        get => _order.Delivered;
        set
        {
            _order.Delivered = value;
            OnPropertyChanged();
        }
    }

    public decimal? QuantityInPackage
    {
        get => _order.QuantityInPackage;
        set
        {
            _order.QuantityInPackage = value;
            OnPropertyChanged();
        }
    }

    public string? VatRate
    {
        get => _order.VatRate;
        set
        {
            _order.VatRate = value;
            OnPropertyChanged();
        }
    }

    public string? Operator
    {
        get => _order.Operator;
        set
        {
            _order.Operator = value;
            OnPropertyChanged();
        }
    }

    public int? ScannerOrderNumber
    {
        get => _order.ScannerOrderNumber;
        set
        {
            _order.ScannerOrderNumber = value;
            OnPropertyChanged();
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SelectProductCommand { get; }

    private bool CanSave()
    {
        return true; // Można zapisać zawsze
    }

    private async Task SaveAsync()
    {
        try
        {
            if (IsNewOrder)
            {
                // Dodajemy nowe zamówienie
                await _orderService.AddAsync(_order);
                
                System.Windows.MessageBox.Show(
                    "Nowe zamówienie zostało dodane.",
                    "Sukces",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            else
            {
                // Aktualizujemy istniejące zamówienie
                await _orderService.UpdateAsync(_order);
                
                System.Windows.MessageBox.Show(
                    "Dane zamówienia zostały zaktualizowane.",
                    "Sukces",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }

            OnSaved();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas zapisywania danych: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void OnSaved()
    {
        Saved?.Invoke(this, EventArgs.Empty);
    }

    private void OnCancelled()
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
                _selectedProduct = selectedProduct;
                
                // Przepisujemy wszystkie dostępne pola z produktu
                _order.ProductId = selectedProduct.Id;
                _order.ProductName = selectedProduct.NamePl;
                _order.ProductNameDraco = selectedProduct.NamePl; // nazwa_PL_draco -> nazwa_towaru_draco
                _order.ProductStatus = selectedProduct.Status;
                _order.SalesUnit = selectedProduct.Unit; // jednostki sprzedaży
                _order.PurchaseUnit = selectedProduct.Unit; // jednostki zakupu - ustawiamy takie same jak jednostki sprzedaży
                
                // Przepisujemy dostawcę, jeśli jest dostępny
                if (selectedProduct.SupplierId.HasValue)
                {
                    _order.SupplierId = selectedProduct.SupplierId;
                }
                if (!string.IsNullOrEmpty(selectedProduct.SupplierName))
                {
                    _order.SupplierName = selectedProduct.SupplierName;
                }
                
                // Powiadamiamy o zmianach wszystkich właściwości
                OnPropertyChanged(nameof(ProductId));
                OnPropertyChanged(nameof(ProductName));
                OnPropertyChanged(nameof(ProductNameDraco));
                OnPropertyChanged(nameof(ProductStatus));
                OnPropertyChanged(nameof(SalesUnit));
                OnPropertyChanged(nameof(PurchaseUnit));
                OnPropertyChanged(nameof(SupplierId));
                OnPropertyChanged(nameof(SupplierName));
                OnPropertyChanged(nameof(SelectedProduct));
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
            Products.Clear();
            var products = await _productRepository.GetAllAsync();
            
            foreach (var product in products)
            {
                _allProducts.Add(product);
            }

            // Ustawiamy SelectedProduct na podstawie ProductId
            if (_order.ProductId.HasValue && _order.ProductId.Value > 0)
            {
                _selectedProduct = _allProducts.FirstOrDefault(p => p.Id == _order.ProductId.Value);
                OnPropertyChanged(nameof(SelectedProduct));
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
