using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Services;
using ERP.UI.WPF.Views;
using ERP.UI.WPF.Services;
using IUserContext = ERP.UI.WPF.Services.IUserContext;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla widoku odbiorców (klientów)
/// </summary>
public class CustomersViewModel : ViewModelBase
{
    private readonly ICustomerService _customerService;
    private readonly IUserContext _userContext;
    private CustomerDto? _selectedCustomer;
    private string _newCustomerName = string.Empty;
    private string _newCustomerEmail = string.Empty;
    private string _newCustomerPhone = string.Empty;

    public CustomersViewModel(ICustomerService customerService, IUserContext userContext)
    {
        _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        Customers = new ObservableCollection<CustomerDto>();
        
        LoadCustomersCommand = new RelayCommand(async () => await LoadCustomersAsync());
        AddCustomerCommand = new RelayCommand(async () => await AddCustomerAsync(), () => !string.IsNullOrWhiteSpace(NewCustomerName));
        EditCustomerCommand = new RelayCommand(() => EditCustomer(), () => SelectedCustomer != null);
        DeleteCustomerCommand = new RelayCommand(async () => await DeleteCustomerAsync(), () => SelectedCustomer != null);
        
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
                    $"Błąd podczas ładowania odbiorców przy starcie: {ex.Message}\n\n{ex.StackTrace}",
                    "Błąd",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        });
    }

    public ObservableCollection<CustomerDto> Customers { get; }

    public CustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            _selectedCustomer = value;
            OnPropertyChanged();
            if (DeleteCustomerCommand is RelayCommand deleteCmd)
                deleteCmd.RaiseCanExecuteChanged();
            if (EditCustomerCommand is RelayCommand editCmd)
                editCmd.RaiseCanExecuteChanged();
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

    private async Task LoadCustomersAsync()
    {
        try
        {
            if (!_userContext.CompanyId.HasValue)
            {
                MessageBox.Show(
                    "Brak wybranej firmy. Wybierz firmę przed załadowaniem odbiorców.",
                    "Brak firmy",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var customers = await _customerService.GetByCompanyIdAsync(_userContext.CompanyId.Value);
            Customers.Clear();
            foreach (var customer in customers)
            {
                Customers.Add(customer);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Błąd podczas ładowania odbiorców: {ex.Message}",
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
                    "Brak wybranej firmy. Wybierz firmę przed dodaniem odbiorcy.",
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
            
            Customers.Add(customer);
            NewCustomerName = string.Empty;
            NewCustomerEmail = string.Empty;
            NewCustomerPhone = string.Empty;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Błąd podczas dodawania odbiorcy: {ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void EditCustomer()
    {
        if (SelectedCustomer == null) return;

        try
        {
            var editViewModel = new CustomerEditViewModel(_customerService, SelectedCustomer);
            var editWindow = new CustomerEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                // Odświeżamy listę, aby pokazać zaktualizowane dane
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
        if (SelectedCustomer == null) return;

        var result = MessageBox.Show(
            $"Czy na pewno chcesz usunąć odbiorcę '{SelectedCustomer.Name}'?",
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

            await _customerService.DeleteAsync(SelectedCustomer.Id, _userContext.CompanyId.Value);
            Customers.Remove(SelectedCustomer);
            SelectedCustomer = null;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Błąd podczas usuwania odbiorcy: {ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
