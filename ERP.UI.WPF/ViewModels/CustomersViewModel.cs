using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Services;
using ERP.UI.WPF.Models;
using ERP.UI.WPF.Views;
using ERP.UI.WPF.Services;
using IUserContext = ERP.UI.WPF.Services.IUserContext;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla widoku kontrahentów.
/// ActiveItem = kontekst szczegółów/edycji (single). SelectedItems = operacje masowe (multi).
/// </summary>
public class CustomersViewModel : ViewModelBase
{
    private readonly ICustomerService _customerService;
    private readonly IUserContext _userContext;
    private CollectionViewSource _customersViewSource;
    private SelectableItem<CustomerDto>? _activeRow;
    private string _searchText = string.Empty;
    private string _newCustomerName = string.Empty;
    private string _newCustomerEmail = string.Empty;
    private string _newCustomerPhone = string.Empty;

    public CustomersViewModel(ICustomerService customerService, IUserContext userContext)
    {
        _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        Customers = new ObservableCollection<SelectableItem<CustomerDto>>();
        SelectedItems = new ObservableCollection<CustomerDto>();
        _customersViewSource = new CollectionViewSource { Source = Customers };
        _customersViewSource.View.Filter = FilterCustomers;

        LoadCustomersCommand = new RelayCommand(async () => await LoadCustomersAsync());
        AddCustomerCommand = new RelayCommand(async () => await AddCustomerAsync(), () => !string.IsNullOrWhiteSpace(NewCustomerName));
        EditCustomerCommand = new RelayCommand(() => EditCustomer(), () => ActiveItem != null);
        DeleteCustomerCommand = new RelayCommand(async () => await DeleteCustomerAsync(), () => ActiveItem != null);
        DeleteSelectedCommand = new RelayCommand(async () => await DeleteSelectedAsync(), () => SelectedItems.Count > 0);

        // Automatyczne ładowanie przy starcie - asynchronicznie, aby nie blokować konstruktora
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                await LoadCustomersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Błąd podczas ładowania kontrahentów przy starcie: {ex.Message}\n\n{ex.StackTrace}",
                    "Błąd",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        });
    }

    public ObservableCollection<SelectableItem<CustomerDto>> Customers { get; }
    public ObservableCollection<CustomerDto> SelectedItems { get; }
    public ICollectionView FilteredCustomers => _customersViewSource.View;

    /// <summary>Wiersz aktualnie zaznaczony (klik w wiersz) → kontekst szczegółów/edycji.</summary>
    public SelectableItem<CustomerDto>? ActiveRow
    {
        get => _activeRow;
        set
        {
            if (ReferenceEquals(_activeRow, value)) return;
            _activeRow = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ActiveItem));
            RaiseCommandsCanExecuteChanged();
        }
    }

    /// <summary>Kontrahent aktywny (bez wrappera) – do edycji, usuwania pojedynczego.</summary>
    public CustomerDto? ActiveItem => ActiveRow?.Item;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged();
                FilteredCustomers.Refresh();
            }
        }
    }

    public string NewCustomerName
    {
        get => _newCustomerName;
        set
        {
            _newCustomerName = value;
            OnPropertyChanged();
            if (AddCustomerCommand is RelayCommand addCmd)
                addCmd.RaiseCanExecuteChanged();
        }
    }

    public string NewCustomerEmail
    {
        get => _newCustomerEmail;
        set
        {
            _newCustomerEmail = value;
            OnPropertyChanged();
        }
    }

    public string NewCustomerPhone
    {
        get => _newCustomerPhone;
        set
        {
            _newCustomerPhone = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoadCustomersCommand { get; }
    public ICommand AddCustomerCommand { get; }
    public ICommand EditCustomerCommand { get; }
    public ICommand DeleteCustomerCommand { get; }
    public ICommand DeleteSelectedCommand { get; }

    private void SubscribeToSelectableItem(SelectableItem<CustomerDto> wrapper)
    {
        wrapper.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SelectableItem<CustomerDto>.IsSelected))
                SyncSelectedItemsFromWrapper(wrapper);
        };
    }

    private void SyncSelectedItemsFromWrapper(SelectableItem<CustomerDto> wrapper)
    {
        if (wrapper.IsSelected)
        {
            if (!SelectedItems.Contains(wrapper.Item))
                SelectedItems.Add(wrapper.Item);
        }
        else
        {
            SelectedItems.Remove(wrapper.Item);
        }

        if (DeleteSelectedCommand is RelayCommand cmd)
            cmd.RaiseCanExecuteChanged();
    }

    private void RaiseCommandsCanExecuteChanged()
    {
        if (EditCustomerCommand is RelayCommand editCmd)
            editCmd.RaiseCanExecuteChanged();
        if (DeleteCustomerCommand is RelayCommand deleteCmd)
            deleteCmd.RaiseCanExecuteChanged();
    }

    private bool FilterCustomers(object obj)
    {
        if (obj is not SelectableItem<CustomerDto> wrapper)
            return false;

        var customer = wrapper.Item;
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        var searchTextLower = SearchText.ToLowerInvariant();
        return (customer.Id.ToString().Contains(searchTextLower)) ||
               (customer.Name?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (customer.Email1?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (customer.Phone1?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (customer.City?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (customer.Nip?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (customer.Status?.ToLowerInvariant().Contains(searchTextLower) ?? false);
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            if (!_userContext.CompanyId.HasValue)
            {
                MessageBox.Show(
                    "Brak wybranej firmy. Wybierz firmę przed załadowaniem kontrahentów.",
                    "Brak firmy",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var customers = await _customerService.GetByCompanyIdAsync(_userContext.CompanyId.Value);
            Customers.Clear();
            SelectedItems.Clear();

            foreach (var customer in customers)
            {
                var wrapper = new SelectableItem<CustomerDto>(customer);
                SubscribeToSelectableItem(wrapper);
                Customers.Add(wrapper);
            }

            FilteredCustomers.Refresh();

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var first = FilteredCustomers.OfType<SelectableItem<CustomerDto>>().FirstOrDefault();
                if (first != null)
                    ActiveRow = first;
                else
                    ActiveRow = null;
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Błąd podczas ładowania kontrahentów: {ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task AddCustomerAsync()
    {
        try
        {
            if (!_userContext.CompanyId.HasValue)
            {
                MessageBox.Show(
                    "Brak wybranej firmy. Wybierz firmę przed dodaniem kontrahenta.",
                    "Brak firmy",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var customerDto = new CustomerDto
            {
                CompanyId = _userContext.CompanyId.Value,
                Name = NewCustomerName,
                Email1 = NewCustomerEmail,
                Phone1 = NewCustomerPhone
            };

            var customer = await _customerService.CreateAsync(customerDto);
            var wrapper = new SelectableItem<CustomerDto>(customer);
            SubscribeToSelectableItem(wrapper);
            Customers.Add(wrapper);

            NewCustomerName = string.Empty;
            NewCustomerEmail = string.Empty;
            NewCustomerPhone = string.Empty;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Błąd podczas dodawania kontrahenta: {ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void EditCustomer()
    {
        if (ActiveItem == null) return;

        try
        {
            var editViewModel = new CustomerEditViewModel(_customerService, ActiveItem);
            var editWindow = new CustomerEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                _ = LoadCustomersAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Błąd podczas otwierania okna edycji: {ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task DeleteCustomerAsync()
    {
        if (ActiveItem == null) return;

        var result = MessageBox.Show(
            $"Czy na pewno chcesz usunąć kontrahenta '{ActiveItem.Name}'?",
            "Potwierdzenie usunięcia",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            if (!_userContext.CompanyId.HasValue)
            {
                MessageBox.Show(
                    "Brak wybranej firmy.",
                    "Błąd",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            await _customerService.DeleteAsync(ActiveItem.Id, _userContext.CompanyId.Value);
            var wrapper = Customers.FirstOrDefault(w => w.Item.Id == ActiveItem.Id);
            if (wrapper != null)
            {
                Customers.Remove(wrapper);
                SelectedItems.Remove(ActiveItem);
            }

            SetActiveItemAfterRemove();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Błąd podczas usuwania kontrahenta: {ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (SelectedItems.Count == 0) return;

        var result = MessageBox.Show(
            $"Czy na pewno chcesz usunąć {SelectedItems.Count} kontrahentów?",
            "Potwierdzenie usunięcia",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            if (!_userContext.CompanyId.HasValue)
            {
                MessageBox.Show("Brak wybranej firmy.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var toRemove = SelectedItems.ToList();
            foreach (var customer in toRemove)
            {
                await _customerService.DeleteAsync(customer.Id, _userContext.CompanyId.Value);
                var wrapper = Customers.FirstOrDefault(w => w.Item.Id == customer.Id);
                if (wrapper != null)
                    Customers.Remove(wrapper);
            }

            SelectedItems.Clear();
            SetActiveItemAfterRemove();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Błąd podczas usuwania kontrahentów: {ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void SetActiveItemAfterRemove()
    {
        var first = FilteredCustomers.OfType<SelectableItem<CustomerDto>>().FirstOrDefault();
        ActiveRow = first ?? null;
    }
}
