using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Services;
using ERP.Infrastructure.Repositories;
using ERP.UI.WPF.Views;
using ERP.UI.WPF.Services;
using IUserContext = ERP.UI.WPF.Services.IUserContext;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla widoku zamówień
/// </summary>
public class OrdersViewModel : ViewModelBase
{
    private OrderDto? _selectedOrder;
    private string _daysToDeleteRealized = string.Empty;
    private string _numericValue = string.Empty;
    private readonly IOrderService _orderService;
    private readonly ProductRepository _productRepository;
    private readonly IUserContext _userContext;

    public OrdersViewModel(
        IOrderService orderService, 
        ProductRepository productRepository,
        IUserContext userContext)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        Orders = new ObservableCollection<OrderDto>();
        
        LoadOrdersCommand = new RelayCommand(async () => await LoadOrdersAsync());
        AddOrderCommand = new RelayCommand(async () => await AddOrderAsync());
        EditOrderCommand = new RelayCommand(() => EditOrder(), () => SelectedOrder != null);
        DeleteOrderCommand = new RelayCommand(async () => await DeleteOrderAsync(), () => SelectedOrder != null);
        
        // Przyciski z górnego paska
        DeleteOldCommand = new RelayCommand(async () => await DeleteOldAsync());
        SendToOfferCommand = new RelayCommand(async () => await SendToOfferAsync());
        SelectAllFromSupplierCommand = new RelayCommand(async () => await SelectAllFromSupplierAsync());
        SelectForOrderCommand = new RelayCommand(async () => await SelectForOrderAsync());
        SendToOrderCommand = new RelayCommand(async () => await SendToOrderAsync());
        
        // Automatyczne ładowanie przy starcie
        _ = LoadOrdersAsync();
    }

    public ObservableCollection<OrderDto> Orders { get; }

    public OrderDto? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            _selectedOrder = value;
            OnPropertyChanged();
            if (EditOrderCommand is RelayCommand editCmd)
                editCmd.RaiseCanExecuteChanged();
            if (DeleteOrderCommand is RelayCommand deleteCmd)
                deleteCmd.RaiseCanExecuteChanged();
        }
    }

    public string DaysToDeleteRealized
    {
        get => _daysToDeleteRealized;
        set => SetProperty(ref _daysToDeleteRealized, value);
    }

    public string NumericValue
    {
        get => _numericValue;
        set => SetProperty(ref _numericValue, value);
    }

    public ICommand LoadOrdersCommand { get; }
    public ICommand AddOrderCommand { get; }
    public ICommand EditOrderCommand { get; }
    public ICommand DeleteOrderCommand { get; }
    
    public ICommand DeleteOldCommand { get; }
    public ICommand SendToOfferCommand { get; }
    public ICommand SelectAllFromSupplierCommand { get; }
    public ICommand SelectForOrderCommand { get; }
    public ICommand SendToOrderCommand { get; }

    private async Task LoadOrdersAsync()
    {
        try
        {
            Orders.Clear();
            var orders = await _orderService.GetAllAsync();
            foreach (var order in orders)
            {
                Orders.Add(order);
            }

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var first = Orders.FirstOrDefault();
                if (first != null)
                    SelectedOrder = first;
            });
        }
        catch (Exception ex)
        {
            var errorMessage = $"Błąd podczas ładowania zamówień: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nWewnętrzny błąd: {ex.InnerException.Message}";
            }
            System.Windows.MessageBox.Show(
                errorMessage,
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task AddOrderAsync()
    {
        try
        {
            // Sprawdzamy, czy użytkownik jest zalogowany i ma wybraną firmę
            if (!_userContext.IsLoggedIn || !_userContext.CompanyId.HasValue)
            {
                System.Windows.MessageBox.Show(
                    "Musisz być zalogowany i mieć wybraną firmę, aby dodać zamówienie.",
                    "Brak sesji",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Tworzymy nowe puste zamówienie
            var newOrder = new OrderDto
            {
                Id = 0, // Nowe zamówienie
                CompanyId = _userContext.CompanyId.Value,
                OrderDate = DateTime.Now, // Domyślna data to dzisiaj
                Status = null,
                SentToOrder = false,
                Delivered = false,
                CreatedAt = DateTime.UtcNow
            };

            var editViewModel = new OrderEditViewModel(_orderService, _productRepository, newOrder);
            var editWindow = new ERP.UI.WPF.Views.OrderEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                // Odświeżamy listę, aby pokazać nowo dodane zamówienie
                _ = LoadOrdersAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas dodawania zamówienia: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void EditOrder()
    {
        if (SelectedOrder == null) return;
        
        try
        {
            var editViewModel = new OrderEditViewModel(_orderService, _productRepository, SelectedOrder);
            var editWindow = new ERP.UI.WPF.Views.OrderEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                // Odświeżamy listę, aby pokazać zaktualizowane dane
                _ = LoadOrdersAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas otwierania okna edycji zamówienia: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task DeleteOrderAsync()
    {
        if (SelectedOrder == null) return;

        try
        {
            var result = System.Windows.MessageBox.Show(
                $"Czy na pewno chcesz usunąć zamówienie ID: {SelectedOrder.Id}?",
                "Potwierdzenie usunięcia",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                await _orderService.DeleteAsync(SelectedOrder.Id);
                await LoadOrdersAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas usuwania zamówienia: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task DeleteOldAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(DaysToDeleteRealized) || !int.TryParse(DaysToDeleteRealized, out int days))
            {
                System.Windows.MessageBox.Show(
                    "Podaj liczbę dni do usunięcia zrealizowanych zamówień.",
                    "Błąd",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"Czy na pewno chcesz usunąć zrealizowane zamówienia starsze niż {days} dni?",
                "Potwierdzenie usunięcia",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                // TODO: Implementuj usuwanie starych zamówień
                System.Windows.MessageBox.Show(
                    "Funkcjonalność usuwania starych zamówień - w przygotowaniu",
                    "Info",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                await LoadOrdersAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas usuwania starych zamówień: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task SendToOfferAsync()
    {
        try
        {
            if (SelectedOrder == null)
            {
                System.Windows.MessageBox.Show(
                    "Wybierz zamówienie do wysłania do oferty.",
                    "Brak wybranego zamówienia",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // TODO: Implementuj wysyłanie do oferty
            System.Windows.MessageBox.Show(
                "Funkcjonalność wysyłania do oferty - w przygotowaniu",
                "Info",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas wysyłania do oferty: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task SelectAllFromSupplierAsync()
    {
        try
        {
            // TODO: Implementuj zaznaczanie wszystkich zamówień od kontrahenta
            System.Windows.MessageBox.Show(
                "Funkcjonalność zaznaczania wszystkich zamówień od kontrahenta - w przygotowaniu",
                "Info",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task SelectForOrderAsync()
    {
        try
        {
            if (SelectedOrder == null)
            {
                System.Windows.MessageBox.Show(
                    "Wybierz zamówienie do zaznaczenia.",
                    "Brak wybranego zamówienia",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // TODO: Implementuj zaznaczanie do zamówienia
            System.Windows.MessageBox.Show(
                "Funkcjonalność zaznaczania do zamówienia - w przygotowaniu",
                "Info",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task SendToOrderAsync()
    {
        try
        {
            if (SelectedOrder == null)
            {
                System.Windows.MessageBox.Show(
                    "Wybierz zamówienie do wysłania.",
                    "Brak wybranego zamówienia",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // TODO: Implementuj wysyłanie do zamówienia
            System.Windows.MessageBox.Show(
                "Funkcjonalność wysyłania do zamówienia - w przygotowaniu",
                "Info",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
}
