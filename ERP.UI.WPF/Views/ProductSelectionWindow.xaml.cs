using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERP.Application.DTOs;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Interaction logic for ProductSelectionWindow.xaml
/// </summary>
public partial class ProductSelectionWindow : Window
{
    private readonly ObservableCollection<ProductDto> _allProducts;
    private readonly ObservableCollection<ProductDto> _filteredProducts;
    public ProductDto? SelectedProduct { get; private set; }

    public ProductSelectionWindow(ObservableCollection<ProductDto> products)
    {
        InitializeComponent();
        
        _allProducts = new ObservableCollection<ProductDto>(products);
        _filteredProducts = new ObservableCollection<ProductDto>(products);
        
        ProductsDataGrid.DataContext = _filteredProducts;
        SelectButton.IsEnabled = false;
        
        // Ustawienie fokusa na pole wyszukiwania
        Loaded += (s, e) => SearchTextBox.Focus();
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
        SelectButton.IsEnabled = ProductsDataGrid.SelectedItem != null;
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
}
