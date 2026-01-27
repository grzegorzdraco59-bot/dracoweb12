using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Repositories;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla okna edycji pozycji zamówienia
/// </summary>
public class OrderPositionEditViewModel : ViewModelBase
{
    private readonly IOrderPositionMainRepository _repository;
    private readonly OrderPositionMainDto _originalPosition;
    private OrderPositionMainDto _position;

    public OrderPositionEditViewModel(IOrderPositionMainRepository repository, OrderPositionMainDto position)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _originalPosition = position ?? throw new ArgumentNullException(nameof(position));
        
        // Tworzymy kopię do edycji
        _position = new OrderPositionMainDto
        {
            Id = position.Id,
            CompanyId = position.CompanyId,
            OrderId = position.OrderId,
            ProductId = position.ProductId,
            DeliveryDateInt = position.DeliveryDateInt,
            DeliveryDate = position.DeliveryDate,
            ProductNameDraco = position.ProductNameDraco,
            Product = position.Product,
            ProductNameEng = position.ProductNameEng,
            OrderUnit = position.OrderUnit,
            OrderQuantity = position.OrderQuantity,
            DeliveredQuantity = position.DeliveredQuantity,
            OrderPrice = position.OrderPrice,
            ProductStatus = position.ProductStatus,
            PurchaseUnit = position.PurchaseUnit,
            PurchaseQuantity = position.PurchaseQuantity,
            PurchasePrice = position.PurchasePrice,
            PurchaseValue = position.PurchaseValue,
            PurchasePricePln = position.PurchasePricePln,
            ConversionFactor = position.ConversionFactor,
            PurchasePricePlnNewUnit = position.PurchasePricePlnNewUnit,
            Notes = position.Notes,
            Supplier = position.Supplier,
            VatRate = position.VatRate,
            UnitWeight = position.UnitWeight,
            QuantityInPackage = position.QuantityInPackage,
            OrderHalaId = position.OrderHalaId,
            OfferPositionId = position.OfferPositionId,
            MarkForCopying = position.MarkForCopying,
            CopiedToWarehouse = position.CopiedToWarehouse,
            Length = position.Length,
            CreatedAt = position.CreatedAt,
            UpdatedAt = position.UpdatedAt
        };

        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => CanSave());
        CancelCommand = new RelayCommand(() => OnCancelled());
    }

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    // Właściwości do edycji
    public int Id => _position.Id;
    public int CompanyId => _position.CompanyId;

    public int OrderId
    {
        get => _position.OrderId;
        set
        {
            _position.OrderId = value;
            OnPropertyChanged();
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        }
    }

    public int? ProductId
    {
        get => _position.ProductId;
        set
        {
            _position.ProductId = value;
            OnPropertyChanged();
        }
    }

    public string? ProductNameDraco
    {
        get => _position.ProductNameDraco;
        set
        {
            _position.ProductNameDraco = value;
            OnPropertyChanged();
        }
    }

    public string? Product
    {
        get => _position.Product;
        set
        {
            _position.Product = value;
            OnPropertyChanged();
        }
    }

    public string? ProductNameEng
    {
        get => _position.ProductNameEng;
        set
        {
            _position.ProductNameEng = value;
            OnPropertyChanged();
        }
    }

    public string? OrderUnit
    {
        get => _position.OrderUnit;
        set
        {
            _position.OrderUnit = value;
            OnPropertyChanged();
        }
    }

    public decimal? OrderQuantity
    {
        get => _position.OrderQuantity;
        set
        {
            _position.OrderQuantity = value;
            OnPropertyChanged();
        }
    }

    public decimal? DeliveredQuantity
    {
        get => _position.DeliveredQuantity;
        set
        {
            _position.DeliveredQuantity = value;
            OnPropertyChanged();
        }
    }

    public decimal? OrderPrice
    {
        get => _position.OrderPrice;
        set
        {
            _position.OrderPrice = value;
            OnPropertyChanged();
        }
    }

    public string? ProductStatus
    {
        get => _position.ProductStatus;
        set
        {
            _position.ProductStatus = value;
            OnPropertyChanged();
        }
    }

    public string? PurchaseUnit
    {
        get => _position.PurchaseUnit;
        set
        {
            _position.PurchaseUnit = value;
            OnPropertyChanged();
        }
    }

    public decimal? PurchaseQuantity
    {
        get => _position.PurchaseQuantity;
        set
        {
            _position.PurchaseQuantity = value;
            OnPropertyChanged();
        }
    }

    public decimal? PurchasePrice
    {
        get => _position.PurchasePrice;
        set
        {
            _position.PurchasePrice = value;
            OnPropertyChanged();
        }
    }

    public decimal? PurchaseValue
    {
        get => _position.PurchaseValue;
        set
        {
            _position.PurchaseValue = value;
            OnPropertyChanged();
        }
    }

    public decimal? PurchasePricePln
    {
        get => _position.PurchasePricePln;
        set
        {
            _position.PurchasePricePln = value;
            OnPropertyChanged();
        }
    }

    public decimal? ConversionFactor
    {
        get => _position.ConversionFactor;
        set
        {
            _position.ConversionFactor = value;
            OnPropertyChanged();
        }
    }

    public string? Notes
    {
        get => _position.Notes;
        set
        {
            _position.Notes = value;
            OnPropertyChanged();
        }
    }

    public string? Supplier
    {
        get => _position.Supplier;
        set
        {
            _position.Supplier = value;
            OnPropertyChanged();
        }
    }

    public string? VatRate
    {
        get => _position.VatRate;
        set
        {
            _position.VatRate = value;
            OnPropertyChanged();
        }
    }

    public decimal? UnitWeight
    {
        get => _position.UnitWeight;
        set
        {
            _position.UnitWeight = value;
            OnPropertyChanged();
        }
    }

    public decimal? QuantityInPackage
    {
        get => _position.QuantityInPackage;
        set
        {
            _position.QuantityInPackage = value;
            OnPropertyChanged();
        }
    }

    public decimal? Length
    {
        get => _position.Length;
        set
        {
            _position.Length = value;
            OnPropertyChanged();
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private bool CanSave()
    {
        return OrderId > 0;
    }

    private async Task SaveAsync()
    {
        try
        {
            if (_position.Id == 0)
            {
                // Nowa pozycja
                await _repository.AddAsync(_position);
            }
            else
            {
                // Aktualizacja istniejącej pozycji
                await _repository.UpdateAsync(_position);
            }

            OnSaved();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas zapisywania pozycji zamówienia: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    protected virtual void OnSaved()
    {
        Saved?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnCancelled()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }
}
