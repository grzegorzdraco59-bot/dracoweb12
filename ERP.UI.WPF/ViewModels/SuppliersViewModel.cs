using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Services;
using ERP.UI.WPF.Views;
using ERP.UI.WPF.Services;
using IUserContext = ERP.UI.WPF.Services.IUserContext;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla widoku kontrahentów
/// </summary>
public class SuppliersViewModel : ViewModelBase
{
    private readonly ISupplierService _supplierService;
    private readonly IUserContext _userContext;
    private CollectionViewSource _suppliersViewSource;
    private SupplierDto? _selectedSupplier;
    private string _searchText = string.Empty;
    private string _newSupplierName = string.Empty;
    private string _newSupplierEmail = string.Empty;
    private string _newSupplierPhone = string.Empty;

    public SuppliersViewModel(ISupplierService supplierService, IUserContext userContext)
    {
        _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        Suppliers = new ObservableCollection<SupplierDto>();
        _suppliersViewSource = new CollectionViewSource { Source = Suppliers };
        _suppliersViewSource.View.Filter = FilterSuppliers;

        LoadSuppliersCommand = new RelayCommand(async () => await LoadSuppliersAsync());
        AddSupplierCommand = new RelayCommand(async () => await AddSupplierAsync(), () => !string.IsNullOrWhiteSpace(NewSupplierName));
        EditSupplierCommand = new RelayCommand(() => EditSupplier(), () => SelectedSupplier != null);
        DeleteSupplierCommand = new RelayCommand(async () => await DeleteSupplierAsync(), () => SelectedSupplier != null);

        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                await LoadSuppliersAsync();
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

    public ObservableCollection<SupplierDto> Suppliers { get; }
    public ICollectionView FilteredSuppliers => _suppliersViewSource.View;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged();
                FilteredSuppliers.Refresh();
            }
        }
    }

    public SupplierDto? SelectedSupplier
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

    private bool FilterSuppliers(object obj)
    {
        if (obj is not SupplierDto supplier)
            return false;

        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        var searchTextLower = SearchText.ToLowerInvariant();
        return (supplier.Id.ToString().Contains(searchTextLower)) ||
               (supplier.Name?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (supplier.Email?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (supplier.Phone?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (supplier.Currency?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (supplier.Notes?.ToLowerInvariant().Contains(searchTextLower) ?? false);
    }

    private async Task LoadSuppliersAsync()
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

            var suppliers = await _supplierService.GetByCompanyIdAsync(_userContext.CompanyId.Value);
            Suppliers.Clear();
            foreach (var supplier in suppliers)
            {
                Suppliers.Add(supplier);
            }
            FilteredSuppliers.Refresh();

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var first = FilteredSuppliers.OfType<SupplierDto>().FirstOrDefault();
                if (first != null)
                    SelectedSupplier = first;
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

    private async Task AddSupplierAsync()
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

            var supplierDto = new SupplierDto
            {
                CompanyId = _userContext.CompanyId.Value,
                Name = NewSupplierName,
                Email = NewSupplierEmail,
                Phone = NewSupplierPhone ?? string.Empty,
                Currency = "PLN"
            };

            var supplier = await _supplierService.CreateAsync(supplierDto);
            Suppliers.Add(supplier);
            NewSupplierName = string.Empty;
            NewSupplierEmail = string.Empty;
            NewSupplierPhone = string.Empty;
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

    private void EditSupplier()
    {
        if (SelectedSupplier == null) return;

        try
        {
            var editViewModel = new SupplierEditViewModel(_supplierService, SelectedSupplier);
            var editWindow = new SupplierEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
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
            $"Czy na pewno chcesz usunąć kontrahenta '{SelectedSupplier.Name}'?",
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

            await _supplierService.DeleteAsync(SelectedSupplier.Id, _userContext.CompanyId.Value);
            Suppliers.Remove(SelectedSupplier);
            SelectedSupplier = null;
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
}
