using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.Application.Services;
using ERP.Domain.Enums;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla widoku zamówień z pozycjami
/// </summary>
public class OrdersMainViewModel : ViewModelBase
{
    private readonly IOrderMainService _orderMainService;
    private OrderMainDto? _selectedOrder;
    private OrderPositionMainDto? _selectedOrderPosition;
    private string _searchText = string.Empty;
    private CollectionViewSource _ordersViewSource;

    public OrdersMainViewModel(IOrderMainService orderMainService)
    {
        _orderMainService = orderMainService ?? throw new ArgumentNullException(nameof(orderMainService));
        
        Orders = new ObservableCollection<OrderMainDto>();
        OrderPositions = new ObservableCollection<OrderPositionMainDto>();
        
        _ordersViewSource = new CollectionViewSource { Source = Orders };
        _ordersViewSource.View.Filter = FilterOrders;
        
        LoadOrdersCommand = new RelayCommand(async () => await LoadOrdersAsync());
        AddOrderCommand = new RelayCommand(async () => await AddOrderAsync());
        EditOrderCommand = new RelayCommand(() => EditOrder(), () => SelectedOrder != null);
        DeleteOrderCommand = new RelayCommand(async () => await DeleteOrderAsync(), () => SelectedOrder != null);
        ChangeStatusCommand = new RelayCommand(async () => await ChangeStatusAsync(), () => SelectedOrder != null);
        AddPositionCommand = new RelayCommand(async () => await AddPositionAsync(), () => SelectedOrder != null);
        EditPositionCommand = new RelayCommand(() => EditPosition(), () => SelectedOrderPosition != null);
        DeletePositionCommand = new RelayCommand(async () => await DeletePositionAsync(), () => SelectedOrderPosition != null);
        
        // Automatyczne ładowanie przy starcie
        _ = LoadOrdersAsync();
    }

    public ObservableCollection<OrderMainDto> Orders { get; }
    public ObservableCollection<OrderPositionMainDto> OrderPositions { get; }
    
    public ICollectionView FilteredOrders => _ordersViewSource.View;
    
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged();
                FilteredOrders.Refresh();
            }
        }
    }

    public OrderMainDto? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            if (_selectedOrder != value)
            {
                _selectedOrder = value;
                OnPropertyChanged();
                if (EditOrderCommand is RelayCommand editCmd)
                    editCmd.RaiseCanExecuteChanged();
                if (DeleteOrderCommand is RelayCommand deleteCmd)
                    deleteCmd.RaiseCanExecuteChanged();
                if (ChangeStatusCommand is RelayCommand changeStatusCmd)
                    changeStatusCmd.RaiseCanExecuteChanged();
                if (AddPositionCommand is RelayCommand addPosCmd)
                    addPosCmd.RaiseCanExecuteChanged();
                if (value != null)
                {
                    _ = LoadOrderPositionsAsync(value.Id);
                }
                else
                {
                    OrderPositions.Clear();
                }
            }
        }
    }

    public OrderPositionMainDto? SelectedOrderPosition
    {
        get => _selectedOrderPosition;
        set
        {
            if (_selectedOrderPosition != value)
            {
                _selectedOrderPosition = value;
                OnPropertyChanged();
                if (EditPositionCommand is RelayCommand editCmd)
                    editCmd.RaiseCanExecuteChanged();
                if (DeletePositionCommand is RelayCommand deleteCmd)
                    deleteCmd.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand LoadOrdersCommand { get; }
    public ICommand AddOrderCommand { get; }
    public ICommand EditOrderCommand { get; }
    public ICommand DeleteOrderCommand { get; }
    public ICommand ChangeStatusCommand { get; }
    public ICommand AddPositionCommand { get; }
    public ICommand EditPositionCommand { get; }
    public ICommand DeletePositionCommand { get; }

    private async Task LoadOrdersAsync()
    {
        try
        {
            Orders.Clear();
            var orders = await _orderMainService.GetAllAsync();
            foreach (var order in orders)
            {
                Orders.Add(order);
            }
            
            if (Orders.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "Brak zamówień w bazie danych lub tabela 'zamowienia' nie istnieje.\n\nSprawdź:\n1. Czy tabela 'zamowienia' istnieje w bazie\n2. Czy są rekordy w tabeli\n3. Czy id_firmy jest poprawne",
                    "Brak danych",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas ładowania zamówień: {ex.Message}\n\nSzczegóły: {ex.GetType().Name}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task LoadOrderPositionsAsync(int orderId)
    {
        try
        {
            OrderPositions.Clear();
            var positions = await _orderMainService.GetPositionsByOrderIdAsync(orderId);
            foreach (var position in positions)
            {
                OrderPositions.Add(position);
            }
            
            if (OrderPositions.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"Brak pozycji dla zamówienia ID: {orderId}");
                // Nie pokazujemy komunikatu, bo może po prostu nie ma pozycji
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Załadowano {OrderPositions.Count} pozycji dla zamówienia ID: {orderId}");
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas ładowania pozycji zamówienia: {ex.Message}\n\nSzczegóły: {ex.GetType().Name}\n\nStack trace: {ex.StackTrace}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private bool FilterOrders(object obj)
    {
        if (obj is not OrderMainDto order)
            return false;
        
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;
        
        var searchTextLower = SearchText.ToLowerInvariant();
        
        return (order.Id.ToString().Contains(searchTextLower)) ||
               (order.OrderNumber?.ToString().Contains(searchTextLower) ?? false) ||
               (order.SupplierName?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (order.Notes?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (order.Status?.ToLowerInvariant().Contains(searchTextLower) ?? false);
    }

    private async Task AddOrderAsync()
    {
        System.Windows.MessageBox.Show("Funkcjonalność dodawania zamówienia - w przygotowaniu", "Info", 
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private void EditOrder()
    {
        System.Windows.MessageBox.Show("Funkcjonalność edycji zamówienia - w przygotowaniu", "Info", 
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private async Task ChangeStatusAsync()
    {
        if (SelectedOrder == null) return;

        var current = OrderStatusMapping.FromDb(SelectedOrder.Status);
        if (current != OrderStatus.Draft)
        {
            System.Windows.MessageBox.Show(
                "Test: tylko przejście Draft→Confirmed. Wybierz zamówienie w statusie Draft.",
                "Zmień status",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        var result = System.Windows.MessageBox.Show(
            "Ustaw status: Potwierdzone (Confirmed)?",
            "Zmień status",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            await _orderMainService.SetStatusAsync(SelectedOrder.Id, OrderStatus.Confirmed);
            await LoadOrdersAsync();
            System.Windows.MessageBox.Show("Status zmieniony na Confirmed.", "Zmień status",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (ERP.Domain.Exceptions.BusinessRuleException ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Reguła biznesowa",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd: {ex.Message}", "Błąd",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task DeleteOrderAsync()
    {
        System.Windows.MessageBox.Show("Funkcjonalność usuwania zamówienia - w przygotowaniu", "Info", 
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private async Task AddPositionAsync()
    {
        System.Windows.MessageBox.Show("Funkcjonalność dodawania pozycji zamówienia - w przygotowaniu", "Info", 
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private void EditPosition()
    {
        System.Windows.MessageBox.Show("Funkcjonalność edycji pozycji zamówienia - w przygotowaniu", "Info", 
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private async Task DeletePositionAsync()
    {
        System.Windows.MessageBox.Show("Funkcjonalność usuwania pozycji zamówienia - w przygotowaniu", "Info", 
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
}
