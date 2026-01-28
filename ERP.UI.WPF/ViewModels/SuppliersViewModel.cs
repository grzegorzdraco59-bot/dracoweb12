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
/// ViewModel dla widoku dostawców
/// </summary>
public class SuppliersViewModel : ViewModelBase
{
    private readonly ICustomerService _customerService;
    private readonly IUserContext _userContext;
    private CustomerDto? _selectedSupplier;
    private string _newSupplierName = string.Empty;
    private string _newSupplierEmail = string.Empty;
    private string _newSupplierPhone = string.Empty;

    public SuppliersViewModel(ICustomerService customerService, IUserContext userContext)
    {
        _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        Suppliers = new ObservableCollection<CustomerDto>();
        
        LoadSuppliersCommand = new RelayCommand(async () => await LoadSuppliersAsync());
        AddSupplierCommand = new RelayCommand(async () => await AddSupplierAsync(), () => !string.IsNullOrWhiteSpace(NewSupplierName));
        EditSupplierCommand = new RelayCommand(() => EditSupplier(), () => SelectedSupplier != null);
        DeleteSupplierCommand = new RelayCommand(async () => await DeleteSupplierAsync(), () => SelectedSupplier != null);
        
        // Automatyczne ładowanie przy starcie - asynchronicznie, aby nie blokować konstruktora
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                await LoadSuppliersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Błąd podczas ładowania dostawców przy starcie: {ex.Message}\n\n{ex.StackTrace}",
                    "Błąd",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        });
    }

    public ObservableCollection<CustomerDto> Suppliers { get; }

    public CustomerDto? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            _selectedSupplier = value;
            OnPropertyChanged();
            if (DeleteSupplierCommand is RelayCommand deleteCmd)
                deleteCmd.RaiseCanExecuteChanged();
            if (EditSupplierCommand is RelayCommand editCmd)
                editCmd.RaiseCanExecuteChanged();
        }
    }

    public string NewSupplierName
    {
        get => _newSupplierName;
        set
        {
            _newSupplierName = value;
            OnPropertyChanged();
            if (AddSupplierCommand is RelayCommand addCmd)
                addCmd.RaiseCanExecuteChanged();
        }
    }

    public string NewSupplierEmail
    {
        get => _newSupplierEmail;
        set
        {
            _newSupplierEmail = value;
            OnPropertyChanged();
        }
    }

    public string NewSupplierPhone
    {
        get => _newSupplierPhone;
        set
        {
            _newSupplierPhone = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoadSuppliersCommand { get; }
    public ICommand AddSupplierCommand { get; }
    public ICommand EditSupplierCommand { get; }
    public ICommand DeleteSupplierCommand { get; }

    private async Task LoadSuppliersAsync()
    {
        try
        {
            if (!_userContext.CompanyId.HasValue)
            {
                MessageBox.Show(
                    "Brak wybranej firmy. Wybierz firmę przed załadowaniem dostawców.",
                    "Brak firmy",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var suppliers = await _customerService.GetByCompanyIdAsync(_userContext.CompanyId.Value);
            Suppliers.Clear();
            foreach (var supplier in suppliers)
            {
                Suppliers.Add(supplier);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Błąd podczas ładowania dostawców: {ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task AddSupplierAsync()
    {
        try
        {
            if (!_userContext.CompanyId.HasValue)
            {
                MessageBox.Show(
                    "Brak wybranej firmy. Wybierz firmę przed dodaniem dostawcy.",
                    "Brak firmy",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var supplierDto = new CustomerDto
            {
                CompanyId = _userContext.CompanyId.Value,
                Name = NewSupplierName,
                Email1 = NewSupplierEmail,
                Phone1 = NewSupplierPhone
            };

            var supplier = await _customerService.CreateAsync(supplierDto);
            
            Suppliers.Add(supplier);
            NewSupplierName = string.Empty;
            NewSupplierEmail = string.Empty;
            NewSupplierPhone = string.Empty;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Błąd podczas dodawania dostawcy: {ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void EditSupplier()
    {
        if (SelectedSupplier == null) return;

        try
        {
            var editViewModel = new CustomerEditViewModel(_customerService, SelectedSupplier);
            var editWindow = new CustomerEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                // Odświeżamy listę, aby pokazać zaktualizowane dane
                _ = LoadSuppliersAsync();
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

    private async Task DeleteSupplierAsync()
    {
        if (SelectedSupplier == null) return;

        var result = MessageBox.Show(
            $"Czy na pewno chcesz usunąć dostawcę '{SelectedSupplier.Name}'?",
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

            await _customerService.DeleteAsync(SelectedSupplier.Id, _userContext.CompanyId.Value);
            Suppliers.Remove(SelectedSupplier);
            SelectedSupplier = null;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Błąd podczas usuwania dostawcy: {ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
