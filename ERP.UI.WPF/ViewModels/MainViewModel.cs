using System.Windows.Controls;
using System.Windows.Input;
using ERP.Application.Services;
using ERP.UI.WPF.Views;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla głównego okna aplikacji - nawigacja między modułami
/// </summary>
public class MainViewModel : ViewModelBase
{
    private UserControl? _currentView;
    private readonly IServiceProvider _serviceProvider;

    public MainViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        
        // Komenda nawigacji z parametrem (numer przycisku)
        NavigateCommand = new RelayCommandWithParameter((parameter) =>
        {
            if (parameter is string viewNumberStr && int.TryParse(viewNumberStr, out int number))
            {
                NavigateToView(number);
            }
            else if (parameter is int viewNumber)
            {
                NavigateToView(viewNumber);
            }
        });
    }

    public UserControl? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public ICommand NavigateCommand { get; }

    private void NavigateToView(int viewNumber)
    {
        try
        {
            switch (viewNumber)
            {
                case 1: // Oferty
                    var offersViewModel = _serviceProvider.GetRequiredService<OffersViewModel>();
                    CurrentView = new OffersView { DataContext = offersViewModel };
                    break;
                case 8: // Dostawcy
                    var suppliersViewModel = _serviceProvider.GetRequiredService<SuppliersViewModel>();
                    CurrentView = new SuppliersView { DataContext = suppliersViewModel };
                    break;
                case 9: // Odbiorcy
                    var customersViewModel = _serviceProvider.GetRequiredService<CustomersViewModel>();
                    CurrentView = new CustomersView { DataContext = customersViewModel };
                    break;
                case 10: // Towary
                    var productsViewModel = _serviceProvider.GetRequiredService<ProductsViewModel>();
                    CurrentView = new ProductsView { DataContext = productsViewModel };
                    break;
                case 11: // Zamówienia
                    var ordersMainViewModel = _serviceProvider.GetRequiredService<OrdersMainViewModel>();
                    CurrentView = new OrdersMainView { DataContext = ordersMainViewModel };
                    break;
                case 12: // Pozycje Zamówienia
                    var orderPositionsViewModel = _serviceProvider.GetRequiredService<OrderPositionsViewModel>();
                    CurrentView = new OrderPositionsView { DataContext = orderPositionsViewModel };
                    break;
                case 13: // Zamówienia hala
                    var ordersViewModel = _serviceProvider.GetRequiredService<OrdersViewModel>();
                    CurrentView = new OrdersView { DataContext = ordersViewModel };
                    break;
                case 23: // Admin
                    var adminViewModel = _serviceProvider.GetRequiredService<AdminViewModel>();
                    CurrentView = new AdminView { DataContext = adminViewModel };
                    break;
                default:
                    // Placeholder dla pozostałych widoków
                    CurrentView = new UserControl
                    {
                        Content = new System.Windows.Controls.TextBlock
                        {
                            Text = $"Widok {viewNumber} - W przygotowaniu",
                            FontSize = 24,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                            VerticalAlignment = System.Windows.VerticalAlignment.Center
                        }
                    };
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas otwierania widoku {viewNumber}:\n\n{ex.Message}\n\n{ex.StackTrace}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
}