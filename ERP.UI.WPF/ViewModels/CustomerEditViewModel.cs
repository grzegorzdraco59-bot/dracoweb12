using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Services;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla okna edycji odbiorcy
/// </summary>
public class CustomerEditViewModel : ViewModelBase
{
    private readonly ICustomerService _customerService;
    private readonly CustomerDto _originalCustomer;
    private CustomerDto _customer;

    public CustomerEditViewModel(ICustomerService customerService, CustomerDto customer)
    {
        _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        _originalCustomer = customer ?? throw new ArgumentNullException(nameof(customer));
        
        // Tworzymy kopię do edycji
        _customer = new CustomerDto
        {
            Id = customer.Id,
            CompanyId = customer.CompanyId,
            Name = customer.Name,
            Surname = customer.Surname,
            FirstName = customer.FirstName,
            Notes = customer.Notes,
            Phone1 = customer.Phone1,
            Phone2 = customer.Phone2,
            Nip = customer.Nip,
            Street = customer.Street,
            PostalCode = customer.PostalCode,
            City = customer.City,
            Country = customer.Country,
            ShippingStreet = customer.ShippingStreet,
            ShippingPostalCode = customer.ShippingPostalCode,
            ShippingCity = customer.ShippingCity,
            ShippingCountry = customer.ShippingCountry,
            Email1 = customer.Email1,
            Email2 = customer.Email2,
            Code = customer.Code,
            Status = customer.Status,
            Currency = customer.Currency,
            CustomerType = customer.CustomerType,
            OfferEnabled = customer.OfferEnabled,
            VatStatus = customer.VatStatus,
            Regon = customer.Regon,
            FullAddress = customer.FullAddress
        };

        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => CanSave());
        CancelCommand = new RelayCommand(() => OnCancelled());
    }

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    // Właściwości do edycji
    public int Id => _customer.Id;
    public int CompanyId => _customer.CompanyId;

    public string Name
    {
        get => _customer.Name;
        set
        {
            _customer.Name = value;
            OnPropertyChanged();
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        }
    }

    public string? Surname
    {
        get => _customer.Surname;
        set
        {
            _customer.Surname = value;
            OnPropertyChanged();
        }
    }

    public string? FirstName
    {
        get => _customer.FirstName;
        set
        {
            _customer.FirstName = value;
            OnPropertyChanged();
        }
    }

    public string? Notes
    {
        get => _customer.Notes;
        set
        {
            _customer.Notes = value;
            OnPropertyChanged();
        }
    }

    public string? Phone1
    {
        get => _customer.Phone1;
        set
        {
            _customer.Phone1 = value;
            OnPropertyChanged();
        }
    }

    public string? Phone2
    {
        get => _customer.Phone2;
        set
        {
            _customer.Phone2 = value;
            OnPropertyChanged();
        }
    }

    public string? Nip
    {
        get => _customer.Nip;
        set
        {
            _customer.Nip = value;
            OnPropertyChanged();
        }
    }

    public string? Street
    {
        get => _customer.Street;
        set
        {
            _customer.Street = value;
            OnPropertyChanged();
        }
    }

    public string? PostalCode
    {
        get => _customer.PostalCode;
        set
        {
            _customer.PostalCode = value;
            OnPropertyChanged();
        }
    }

    public string? City
    {
        get => _customer.City;
        set
        {
            _customer.City = value;
            OnPropertyChanged();
        }
    }

    public string? Country
    {
        get => _customer.Country;
        set
        {
            _customer.Country = value;
            OnPropertyChanged();
        }
    }

    public string? Email1
    {
        get => _customer.Email1;
        set
        {
            _customer.Email1 = value;
            OnPropertyChanged();
        }
    }

    public string? Email2
    {
        get => _customer.Email2;
        set
        {
            _customer.Email2 = value;
            OnPropertyChanged();
        }
    }

    public string? Code
    {
        get => _customer.Code;
        set
        {
            _customer.Code = value;
            OnPropertyChanged();
        }
    }

    public string? Status
    {
        get => _customer.Status;
        set
        {
            _customer.Status = value;
            OnPropertyChanged();
        }
    }

    public string Currency
    {
        get => _customer.Currency;
        set
        {
            _customer.Currency = value;
            OnPropertyChanged();
        }
    }

    public bool? OfferEnabled
    {
        get => _customer.OfferEnabled;
        set
        {
            _customer.OfferEnabled = value;
            OnPropertyChanged();
        }
    }

    public string? VatStatus
    {
        get => _customer.VatStatus;
        set
        {
            _customer.VatStatus = value;
            OnPropertyChanged();
        }
    }

    public string? Regon
    {
        get => _customer.Regon;
        set
        {
            _customer.Regon = value;
            OnPropertyChanged();
        }
    }

    public string? ShippingStreet
    {
        get => _customer.ShippingStreet;
        set
        {
            _customer.ShippingStreet = value;
            OnPropertyChanged();
        }
    }

    public string? ShippingPostalCode
    {
        get => _customer.ShippingPostalCode;
        set
        {
            _customer.ShippingPostalCode = value;
            OnPropertyChanged();
        }
    }

    public string? ShippingCity
    {
        get => _customer.ShippingCity;
        set
        {
            _customer.ShippingCity = value;
            OnPropertyChanged();
        }
    }

    public string? ShippingCountry
    {
        get => _customer.ShippingCountry;
        set
        {
            _customer.ShippingCountry = value;
            OnPropertyChanged();
        }
    }

    public int? CustomerType
    {
        get => _customer.CustomerType;
        set
        {
            _customer.CustomerType = value;
            OnPropertyChanged();
        }
    }

    public string? FullAddress
    {
        get => _customer.FullAddress;
        set
        {
            _customer.FullAddress = value;
            OnPropertyChanged();
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Name);
    }

    private async Task SaveAsync()
    {
        try
        {
            // Aktualizujemy wszystkie dane przez serwis
            await _customerService.UpdateAsync(_customer);

            System.Windows.MessageBox.Show(
                "Dane odbiorcy zostały zaktualizowane.",
                "Sukces",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);

            OnSaved();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas zapisywania danych: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void OnSaved()
    {
        Saved?.Invoke(this, EventArgs.Empty);
    }

    private void OnCancelled()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }
}
