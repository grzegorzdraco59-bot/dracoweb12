using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.Application.Services;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla listy zamówień (górny grid).
/// </summary>
public class OrdersMainViewModel : ViewModelBase
{
    private readonly IOrderRowRepository _orderRowRepository;
    private readonly IOrderPositionRepository _orderPositionRepository;
    private readonly IOrderMainService _orderMainService;
    private readonly ERP.UI.WPF.Services.ITowarPicker _towarPicker;
    private readonly IUserContext _userContext;
    private OrderRow? _selectedOrder;
    private OrderPositionRow? _selectedPosition;
    private string _searchText = "";
    private DispatcherTimer? _searchDebounceTimer;
    private const int SearchDebounceMs = 300;

    public OrdersMainViewModel(
        IOrderRowRepository orderRowRepository,
        IOrderPositionRepository orderPositionRepository,
        IOrderMainService orderMainService,
        ERP.UI.WPF.Services.ITowarPicker towarPicker,
        IUserContext userContext)
    {
        _orderRowRepository = orderRowRepository ?? throw new ArgumentNullException(nameof(orderRowRepository));
        _orderPositionRepository = orderPositionRepository ?? throw new ArgumentNullException(nameof(orderPositionRepository));
        _orderMainService = orderMainService ?? throw new ArgumentNullException(nameof(orderMainService));
        _towarPicker = towarPicker ?? throw new ArgumentNullException(nameof(towarPicker));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));

        Orders = new ObservableCollection<OrderRow>();
        Positions = new ObservableCollection<OrderPositionRow>();

        AddOrderCommand = new RelayCommand(async () => await AddOrderAsync());
        EditOrderCommand = new RelayCommand(async () => await EditOrderAsync(), () => SelectedOrder != null);
        DeleteOrderCommand = new RelayCommand(async () => await DeleteOrderAsync(), () => SelectedOrder != null);

        AddPositionCommand = new RelayCommand(async () => await AddPositionAsync(), () => SelectedOrder != null);
        EditPositionCommand = new RelayCommand(async () => await EditPositionAsync(), () => SelectedOrder != null && SelectedPosition != null);
        DeletePositionCommand = new RelayCommand(async () => await DeletePositionAsync(), () => SelectedOrder != null && SelectedPosition != null);

        // Automatyczne ładowanie przy starcie
        _ = LoadOrdersAsync();
    }

    public ObservableCollection<OrderRow> Orders { get; }
    public ObservableCollection<OrderPositionRow> Positions { get; }

    public ICommand AddOrderCommand { get; }
    public ICommand EditOrderCommand { get; }
    public ICommand DeleteOrderCommand { get; }

    public ICommand AddPositionCommand { get; }
    public ICommand EditPositionCommand { get; }
    public ICommand DeletePositionCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value ?? "";
                OnPropertyChanged();
                ScheduleSearchDebounce();
            }
        }
    }

    public OrderRow? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            if (_selectedOrder != value)
            {
                _selectedOrder = value;
                OnPropertyChanged();
                RaiseOrderCommandStates();
                RaisePositionCommandStates();
                if (_selectedOrder != null)
                    _ = LoadPositionsAsync(_selectedOrder.id);
                else
                {
                    Positions.Clear();
                    SelectedPosition = null;
                }
            }
        }
    }

    public OrderPositionRow? SelectedPosition
    {
        get => _selectedPosition;
        set
        {
            if (_selectedPosition != value)
            {
                _selectedPosition = value;
                OnPropertyChanged();
                RaisePositionCommandStates();
            }
        }
    }

    private async Task LoadOrdersAsync()
    {
        if (!_userContext.CompanyId.HasValue)
        {
            System.Windows.MessageBox.Show("Brak wybranej firmy.", "Zamówienia",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            Orders.Clear();
            SelectedOrder = null;
            return;
        }
        try
        {
            var selectedId = SelectedOrder?.id;
            Orders.Clear();
            IEnumerable<OrderRow> orders;
            if (string.IsNullOrWhiteSpace(SearchText))
                orders = await _orderRowRepository.GetByCompanyIdAsync(_userContext.CompanyId.Value);
            else
                orders = await _orderRowRepository.GetByCompanyIdAsync(_userContext.CompanyId.Value, SearchText);
            foreach (var order in orders)
                Orders.Add(order);
            if (selectedId.HasValue)
            {
                var match = Orders.FirstOrDefault(o => o.id == selectedId.Value);
                if (match != null)
                    SelectedOrder = match;
            }
            else if (string.IsNullOrWhiteSpace(SearchText) && SelectedOrder == null && Orders.Count > 0)
            {
                SelectedOrder = Orders.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas ładowania zamówień: {ex.Message}",
                "Zamówienia",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task LoadPositionsAsync(int orderId)
    {
        if (!_userContext.CompanyId.HasValue || orderId <= 0)
        {
            Positions.Clear();
            return;
        }
        try
        {
            Positions.Clear();
            var items = await _orderPositionRepository.GetByOrderIdAsync(_userContext.CompanyId.Value, orderId);
            foreach (var item in items)
                Positions.Add(item);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas ładowania pozycji zamówienia: {ex.Message}",
                "Zamówienia",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task AddOrderAsync()
    {
        if (!_userContext.CompanyId.HasValue)
        {
            System.Windows.MessageBox.Show("Brak wybranej firmy.", "Zamówienia",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var newOrder = new OrderMainDto
        {
            Id = 0,
            CompanyId = _userContext.CompanyId.Value
        };

        var vm = new OrderMainEditViewModel(_orderMainService, newOrder);
        var win = new ERP.UI.WPF.Views.OrderMainEditWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (win.ShowDialog() == true)
        {
            if (vm.SavedOrderId.HasValue)
                await ReloadOrdersPreserveSelectionAsync(vm.SavedOrderId.Value);
            else
                await LoadOrdersAsync();
        }
    }

    private async Task EditOrderAsync()
    {
        if (SelectedOrder == null)
        {
            System.Windows.MessageBox.Show("Wybierz zamówienie do edycji.", "Zamówienia",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var order = await _orderMainService.GetByIdAsync(SelectedOrder.id);
        if (order == null)
        {
            System.Windows.MessageBox.Show("Nie znaleziono zamówienia do edycji.", "Zamówienia",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var vm = new OrderMainEditViewModel(_orderMainService, order);
        var win = new ERP.UI.WPF.Views.OrderMainEditWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (win.ShowDialog() == true)
        {
            await ReloadOrdersPreserveSelectionAsync(order.Id);
        }
    }

    private async Task DeleteOrderAsync()
    {
        if (SelectedOrder == null)
            return;

        var result = System.Windows.MessageBox.Show(
            "Czy na pewno usunąć wybrane zamówienie?",
            "Potwierdź usunięcie",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (result != System.Windows.MessageBoxResult.Yes)
            return;

        try
        {
            var deletedId = SelectedOrder.id;
            await _orderMainService.DeleteAsync(deletedId);
            await LoadOrdersAsync();
            if (SelectedOrder?.id == deletedId && Orders.Count > 0)
                SelectedOrder = Orders.FirstOrDefault();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas usuwania zamówienia: {ex.Message}",
                "Zamówienia",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task AddPositionAsync()
    {
        System.Windows.MessageBox.Show("DEBUG: AddPositionCommand", "Pozycje zamówienia",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        if (SelectedOrder == null)
        {
            System.Windows.MessageBox.Show("Wybierz zamówienie, aby dodać pozycję.", "Pozycje zamówienia",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }
        if (!_userContext.CompanyId.HasValue)
        {
            System.Windows.MessageBox.Show("Brak wybranej firmy.", "Pozycje zamówienia",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var newPosition = new OrderPositionMainDto
        {
            Id = 0,
            CompanyId = _userContext.CompanyId.Value,
            OrderId = SelectedOrder.id
        };

        var vm = new OrderPositionEditViewModel(_orderMainService, _towarPicker, newPosition, SelectedOrder.waluta);
        var win = new ERP.UI.WPF.Views.OrderPositionEditView(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (win.ShowDialog() == true)
        {
            await LoadPositionsAsync(SelectedOrder.id);
            if (vm.SavedPositionId.HasValue)
                SelectedPosition = Positions.FirstOrDefault(p => p.id_pozycji_zamowienia == vm.SavedPositionId.Value);
            await ReloadOrdersPreserveSelectionAsync(SelectedOrder.id);
        }
    }

    private async Task EditPositionAsync()
    {
        System.Windows.MessageBox.Show("DEBUG: EditPositionCommand", "Pozycje zamówienia",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        if (SelectedOrder == null || SelectedPosition == null)
        {
            System.Windows.MessageBox.Show("Wybierz pozycję do edycji.", "Pozycje zamówienia",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var full = await _orderMainService.GetPositionByIdAsync(SelectedPosition.id_pozycji_zamowienia);
        if (full == null)
        {
            System.Windows.MessageBox.Show("Nie znaleziono pozycji do edycji.", "Pozycje zamówienia",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var vm = new OrderPositionEditViewModel(_orderMainService, _towarPicker, full, SelectedOrder.waluta);
        var win = new ERP.UI.WPF.Views.OrderPositionEditView(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (win.ShowDialog() == true)
        {
            var selectedId = SelectedPosition.id_pozycji_zamowienia;
            await LoadPositionsAsync(SelectedOrder.id);
            SelectedPosition = Positions.FirstOrDefault(p => p.id_pozycji_zamowienia == selectedId);
            await ReloadOrdersPreserveSelectionAsync(SelectedOrder.id);
        }
    }

    private async Task DeletePositionAsync()
    {
        System.Windows.MessageBox.Show("DEBUG: DeletePositionCommand", "Pozycje zamówienia",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        if (SelectedOrder == null || SelectedPosition == null)
        {
            System.Windows.MessageBox.Show("Wybierz pozycję do usunięcia.", "Pozycje zamówienia",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var result = System.Windows.MessageBox.Show(
            "Czy na pewno usunąć wybraną pozycję zamówienia?",
            "Potwierdź usunięcie",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (result != System.Windows.MessageBoxResult.Yes)
            return;

        try
        {
            var deletedId = SelectedPosition.id_pozycji_zamowienia;
            await _orderMainService.DeletePositionAsync(deletedId);
            await LoadPositionsAsync(SelectedOrder.id);
            SelectedPosition = Positions.FirstOrDefault();
            await ReloadOrdersPreserveSelectionAsync(SelectedOrder.id);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas usuwania pozycji: {ex.Message}",
                "Pozycje zamówienia",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void RaisePositionCommandStates()
    {
        if (AddPositionCommand is RelayCommand addCmd)
            addCmd.RaiseCanExecuteChanged();
        if (EditPositionCommand is RelayCommand editCmd)
            editCmd.RaiseCanExecuteChanged();
        if (DeletePositionCommand is RelayCommand deleteCmd)
            deleteCmd.RaiseCanExecuteChanged();
    }

    private void RaiseOrderCommandStates()
    {
        if (EditOrderCommand is RelayCommand editCmd)
            editCmd.RaiseCanExecuteChanged();
        if (DeleteOrderCommand is RelayCommand deleteCmd)
            deleteCmd.RaiseCanExecuteChanged();
    }

    private async Task ReloadOrdersPreserveSelectionAsync(int orderId)
    {
        if (orderId <= 0 || !_userContext.CompanyId.HasValue)
            return;
        var orders = await _orderRowRepository.GetByCompanyIdAsync(_userContext.CompanyId.Value, SearchText);
        Orders.Clear();
        foreach (var order in orders)
            Orders.Add(order);
        var match = Orders.FirstOrDefault(o => o.id == orderId);
        if (match != null)
            SelectedOrder = match;
    }

    private void ScheduleSearchDebounce()
    {
        _searchDebounceTimer?.Stop();
        _searchDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(SearchDebounceMs)
        };
        _searchDebounceTimer.Tick += (_, _) =>
        {
            _searchDebounceTimer?.Stop();
            _searchDebounceTimer = null;
            _ = LoadOrdersAsync();
        };
        _searchDebounceTimer.Start();
    }
}
