using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Infrastructure.Repositories;
using ERP.UI.WPF.Views;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla widoku Towary
/// </summary>
public class ProductsViewModel : ViewModelBase
{
    private readonly ProductRepository _productRepository;
    private readonly WarehouseRepository _warehouseRepository;
    private string _searchText = string.Empty;
    private CollectionViewSource _productsViewSource;
    private CollectionViewSource _warehouseViewSource;
    private ProductDto? _selectedProduct;
    
    public ProductsViewModel(ProductRepository productRepository, WarehouseRepository warehouseRepository)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _warehouseRepository = warehouseRepository ?? throw new ArgumentNullException(nameof(warehouseRepository));
        
        Products = new ObservableCollection<ProductDto>();
        WarehouseItems = new ObservableCollection<WarehouseItemDto>();
        
        _productsViewSource = new CollectionViewSource { Source = Products };
        _productsViewSource.View.Filter = FilterProducts;
        
        _warehouseViewSource = new CollectionViewSource { Source = WarehouseItems };
        _warehouseViewSource.View.Filter = FilterWarehouseItems;
        
        LoadProductsCommand = new RelayCommand(async () => await LoadProductsAsync());
        
        AddProductCommand = new RelayCommand(() => 
            System.Windows.MessageBox.Show("Funkcjonalność dodawania towaru - w przygotowaniu", "Info", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information));
        EditProductCommand = new RelayCommand(() => EditProduct(), 
            () => SelectedProduct != null);
        DeleteProductCommand = new RelayCommand(() => 
            System.Windows.MessageBox.Show("Funkcjonalność usuwania towaru - w przygotowaniu", "Info", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information), 
            () => SelectedProduct != null);
        
        AddWarehouseCommand = new RelayCommand(() => 
            System.Windows.MessageBox.Show("Funkcjonalność dodawania operacji magazynowej - w przygotowaniu", "Info", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information));
        EditWarehouseCommand = new RelayCommand(() => 
            System.Windows.MessageBox.Show("Funkcjonalność edycji operacji magazynowej - w przygotowaniu", "Info", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information), 
            () => SelectedWarehouseItem != null);
        DeleteWarehouseCommand = new RelayCommand(() => 
            System.Windows.MessageBox.Show("Funkcjonalność usuwania operacji magazynowej - w przygotowaniu", "Info", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information), 
            () => SelectedWarehouseItem != null);
        
        // Automatyczne ładowanie przy starcie
        _ = LoadProductsAsync();
    }

    public ObservableCollection<ProductDto> Products { get; }
    public ObservableCollection<WarehouseItemDto> WarehouseItems { get; }
    
    public ICollectionView FilteredProducts => _productsViewSource.View;
    public ICollectionView FilteredWarehouseItems => _warehouseViewSource.View;
    
    public ProductDto? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (_selectedProduct != value)
            {
                _selectedProduct = value;
                OnPropertyChanged();
                if (EditProductCommand is RelayCommand editCmd)
                    editCmd.RaiseCanExecuteChanged();
                if (DeleteProductCommand is RelayCommand deleteCmd)
                    deleteCmd.RaiseCanExecuteChanged();
                // Odśwież filtrowanie magazynu po wyborze towaru
                FilteredWarehouseItems.Refresh();
            }
        }
    }
    
    public WarehouseItemDto? SelectedWarehouseItem { get; set; }
    
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged();
                FilteredProducts.Refresh();
            }
        }
    }

    public ICommand LoadProductsCommand { get; }
    public ICommand AddProductCommand { get; }
    public ICommand EditProductCommand { get; }
    public ICommand DeleteProductCommand { get; }
    public ICommand AddWarehouseCommand { get; }
    public ICommand EditWarehouseCommand { get; }
    public ICommand DeleteWarehouseCommand { get; }

    private bool FilterProducts(object obj)
    {
        if (obj is not ProductDto product)
            return false;
        
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;
        
        var searchTextLower = SearchText.ToLowerInvariant();
        
        return (product.Id.ToString().Contains(searchTextLower)) ||
               (product.Group?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (product.NamePl?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (product.NameEng?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (product.PricePln?.ToString().Contains(searchTextLower) ?? false) ||
               (product.Unit?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (product.Status?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (product.SupplierName?.ToLowerInvariant().Contains(searchTextLower) ?? false);
    }
    
    private async Task LoadProductsAsync()
    {
        try
        {
            var products = await _productRepository.GetAllAsync();
            Products.Clear();
            foreach (var product in products)
            {
                Products.Add(product);
            }
            
            // Załaduj również magazyn
            await LoadWarehouseAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd podczas ładowania towarów: {ex.Message}\n\n{ex.GetType().Name}\n\n{ex.StackTrace}", 
                "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    
    private async Task LoadWarehouseAsync()
    {
        try
        {
            var warehouseItems = await _warehouseRepository.GetAllAsync();
            WarehouseItems.Clear();
            foreach (var item in warehouseItems)
            {
                WarehouseItems.Add(item);
            }
            FilteredWarehouseItems.Refresh();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd podczas ładowania magazynu: {ex.Message}", 
                "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    
    private bool FilterWarehouseItems(object obj)
    {
        if (obj is not WarehouseItemDto warehouseItem)
            return false;
        
        // Jeśli wybrano towar, pokaż tylko operacje dla tego towaru
        if (SelectedProduct != null)
        {
            return warehouseItem.ProductId == SelectedProduct.Id;
        }
        
        // Jeśli nie wybrano towaru, pokaż wszystkie operacje
        return true;
    }

    private void EditProduct()
    {
        if (SelectedProduct == null) return;

        try
        {
            var editViewModel = new ProductEditViewModel(_productRepository, SelectedProduct);
            var editWindow = new ProductEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                // Odświeżamy listę, aby pokazać zaktualizowane dane
                _ = LoadProductsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas otwierania okna edycji: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
}
