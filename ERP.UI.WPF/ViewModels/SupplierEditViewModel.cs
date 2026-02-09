using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Services;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla okna edycji kontrahenta
/// </summary>
public class SupplierEditViewModel : ViewModelBase
{
    private readonly ISupplierService _supplierService;
    private readonly SupplierDto _originalSupplier;
    private SupplierDto _supplier;

    public SupplierEditViewModel(ISupplierService supplierService, SupplierDto supplier)
    {
        _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
        _originalSupplier = supplier ?? throw new ArgumentNullException(nameof(supplier));

        _supplier = new SupplierDto
        {
            Id = supplier.Id,
            CompanyId = supplier.CompanyId,
            Name = supplier.Name,
            Currency = supplier.Currency ?? "PLN",
            Email = supplier.Email,
            Phone = supplier.Phone ?? string.Empty,
            Notes = supplier.Notes
        };

        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => CanSave());
        CancelCommand = new RelayCommand(() => OnCancelled());
    }

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    public int Id => _supplier.Id;
    public int CompanyId => _supplier.CompanyId;

    public string Name
    {
        get => _supplier.Name;
        set
        {
            _supplier.Name = value;
            OnPropertyChanged();
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        }
    }

    public string Currency
    {
        get => _supplier.Currency ?? "PLN";
        set
        {
            _supplier.Currency = value;
            OnPropertyChanged();
        }
    }

    public string? Email
    {
        get => _supplier.Email;
        set
        {
            _supplier.Email = value;
            OnPropertyChanged();
        }
    }

    public string Phone
    {
        get => _supplier.Phone ?? string.Empty;
        set
        {
            _supplier.Phone = value;
            OnPropertyChanged();
        }
    }

    public string? Notes
    {
        get => _supplier.Notes;
        set
        {
            _supplier.Notes = value;
            OnPropertyChanged();
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private bool CanSave() => !string.IsNullOrWhiteSpace(_supplier.Name);

    private async Task SaveAsync()
    {
        if (!CanSave()) return;

        try
        {
            await _supplierService.UpdateAsync(_supplier);
            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas zapisywania: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void OnCancelled()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }
}
