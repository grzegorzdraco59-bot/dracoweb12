using System.Collections.ObjectModel;
using System.Windows;
using ERP.Application.DTOs;
using ERP.Infrastructure.Repositories;
using ERP.UI.WPF.Views;

namespace ERP.UI.WPF.Services;

public class TowarPicker : ITowarPicker
{
    private readonly ProductRepository _productRepository;

    public TowarPicker(ProductRepository productRepository)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
    }

    public async Task<ProductDto?> PickAsync(int companyId)
    {
        var products = await _productRepository.GetAllAsync();
        var list = new ObservableCollection<ProductDto>(products);
        var window = new ProductSelectionWindow(list, _productRepository, companyId)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        if (window.ShowDialog() == true)
            return window.SelectedProduct;
        return null;
    }
}
