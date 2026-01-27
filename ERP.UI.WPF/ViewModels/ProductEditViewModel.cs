using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Infrastructure.Repositories;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla okna edycji towaru
/// </summary>
public class ProductEditViewModel : ViewModelBase
{
    private readonly ProductRepository _repository;
    private readonly ProductDto _originalProduct;
    private ProductDto _product;

    public ProductEditViewModel(ProductRepository repository, ProductDto product)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _originalProduct = product ?? throw new ArgumentNullException(nameof(product));
        
        // Tworzymy kopię do edycji
        _product = new ProductDto
        {
            Id = product.Id,
            CompanyId = product.CompanyId,
            Group = product.Group,
            NamePl = product.NamePl,
            NameEng = product.NameEng,
            PricePln = product.PricePln,
            Unit = product.Unit,
            Status = product.Status
        };

        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => CanSave());
        CancelCommand = new RelayCommand(() => OnCancelled());
    }

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    // Właściwości do edycji
    public int Id => _product.Id;
    public int? CompanyId => _product.CompanyId;

    public string? Group
    {
        get => _product.Group;
        set
        {
            _product.Group = value;
            OnPropertyChanged();
        }
    }

    public string? NamePl
    {
        get => _product.NamePl;
        set
        {
            _product.NamePl = value;
            OnPropertyChanged();
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        }
    }

    public string? NameEng
    {
        get => _product.NameEng;
        set
        {
            _product.NameEng = value;
            OnPropertyChanged();
        }
    }

    public decimal? PricePln
    {
        get => _product.PricePln;
        set
        {
            _product.PricePln = value;
            OnPropertyChanged();
        }
    }

    public string? Unit
    {
        get => _product.Unit;
        set
        {
            _product.Unit = value;
            OnPropertyChanged();
        }
    }

    public string? Status
    {
        get => _product.Status;
        set
        {
            _product.Status = value;
            OnPropertyChanged();
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(NamePl);
    }

    private async Task SaveAsync()
    {
        try
        {
            // TODO: Implementacja zapisu do bazy danych
            // Na razie tylko pokazujemy komunikat
            System.Windows.MessageBox.Show(
                "Funkcjonalność zapisu towaru - w przygotowaniu.\n\n" +
                $"Zapisywane dane:\n" +
                $"ID: {Id}\n" +
                $"Grupa: {Group}\n" +
                $"Nazwa PL: {NamePl}\n" +
                $"Nazwa ENG: {NameEng}\n" +
                $"Cena PLN: {PricePln}\n" +
                $"Jednostka: {Unit}\n" +
                $"Status: {Status}",
                "Info",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);

            OnSaved();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas zapisywania towaru: {ex.Message}",
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
