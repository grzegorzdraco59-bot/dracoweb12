using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERP.Application.DTOs;
using ERP.Infrastructure.Repositories;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Interaction logic for ProductSelectionWindow.xaml
/// </summary>
public partial class ProductSelectionWindow : Window, System.ComponentModel.INotifyPropertyChanged
{
    private readonly ObservableCollection<ProductDto> _allProducts;
    private readonly ObservableCollection<ProductDto> _filteredProducts;
    private readonly ProductRepository _productRepository;
    private readonly int _companyId;
    private ProductDto? _selectedProduct;
    public ProductDto? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (!ReferenceEquals(_selectedProduct, value))
            {
                _selectedProduct = value;
                OnPropertyChanged(nameof(SelectedProduct));
            }
        }
    }

    public ProductSelectionWindow(ObservableCollection<ProductDto> products, ProductRepository productRepository, int companyId)
    {
        InitializeComponent();
        DataContext = this;
        _allProducts = new ObservableCollection<ProductDto>(products);
        _filteredProducts = new ObservableCollection<ProductDto>(products);
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _companyId = companyId;
        SelectButton.IsEnabled = false;
        ChangeButton.IsEnabled = false;
        DeleteButton.IsEnabled = false;
        
        // Ustawienie fokusa na pole wyszukiwania
        Loaded += (s, e) => SearchTextBox.Focus();
    }

    public ObservableCollection<ProductDto> FilteredProducts => _filteredProducts;
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            FilterProducts(textBox.Text);
        }
    }

    private void FilterProducts(string searchText)
    {
        _filteredProducts.Clear();
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            foreach (var product in _allProducts)
            {
                _filteredProducts.Add(product);
            }
        }
        else
        {
            var searchTextLower = searchText.ToLowerInvariant();
            foreach (var product in _allProducts)
            {
                // Szukaj po wszystkich kolumnach
                if (product.Id.ToString().Contains(searchTextLower) ||
                    (product.NamePl?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                    (product.NameEng?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                    (product.SupplierName?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                    (product.Status?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                    (product.Group?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                    (product.Unit?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                    (product.PricePln?.ToString().Contains(searchTextLower) ?? false))
                {
                    _filteredProducts.Add(product);
                }
            }
        }
    }

    private void ProductsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedProduct = ProductsDataGrid.SelectedItem as ProductDto;
        var hasSelection = SelectedProduct != null;
        SelectButton.IsEnabled = hasSelection;
        ChangeButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
    }

    private void ProductsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ProductsDataGrid.SelectedItem is ProductDto product)
        {
            SelectedProduct = product;
            DialogResult = true;
            Close();
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProductsDataGrid.SelectedItem is ProductDto product)
        {
            SelectedProduct = product;
            DialogResult = true;
            Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void InsertButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var newProduct = new ProductDto
            {
                Id = 0,
                CompanyId = _companyId
            };
            var vm = new ProductEditViewModel(_productRepository, newProduct, _companyId);
            var win = new ProductEditWindow(vm) { Owner = this };
            if (win.ShowDialog() == true)
                await ReloadProductsAsync(vm.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Błąd podczas dodawania towaru: {ex.Message}", "Towary",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ChangeButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedProduct == null)
            return;
        try
        {
            var vm = new ProductEditViewModel(_productRepository, SelectedProduct, _companyId);
            var win = new ProductEditWindow(vm) { Owner = this };
            if (win.ShowDialog() == true)
                await ReloadProductsAsync(SelectedProduct.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Błąd podczas edycji towaru: {ex.Message}", "Towary",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedProduct == null)
            return;

        var result = MessageBox.Show(
            $"Czy na pewno chcesz usunąć towar ID: {SelectedProduct.Id}?",
            "Potwierdzenie usunięcia",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            await _productRepository.DeleteAsync(SelectedProduct.Id);
            await ReloadProductsAsync(null);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Błąd podczas usuwania towaru: {ex.Message}", "Towary",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ReloadProductsAsync(int? selectedId)
    {
        var products = await _productRepository.GetAllAsync();
        _allProducts.Clear();
        _filteredProducts.Clear();
        foreach (var product in products)
        {
            _allProducts.Add(product);
            _filteredProducts.Add(product);
        }

        if (selectedId.HasValue)
        {
            var match = _filteredProducts.FirstOrDefault(p => p.Id == selectedId.Value);
            if (match != null)
                ProductsDataGrid.SelectedItem = match;
        }
    }
}
