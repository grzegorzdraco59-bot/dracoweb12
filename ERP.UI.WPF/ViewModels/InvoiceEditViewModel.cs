using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.UI.WPF.Views;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla okna edycji faktury – zmiana kontrahenta.
/// </summary>
public class InvoiceEditViewModel : ViewModelBase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly InvoiceDto _invoice;

    public InvoiceEditViewModel(IInvoiceRepository invoiceRepository, InvoiceDto invoice)
    {
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _invoice = invoice ?? throw new ArgumentNullException(nameof(invoice));

        SaveCommand = new RelayCommand(async () => await SaveAsync());
        CancelCommand = new RelayCommand(() => OnCancelled());
        PickKontrahentCommand = new RelayCommand(PickKontrahent);
    }

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    public long Id => _invoice.Id;
    public int CompanyId => _invoice.CompanyId;

    public int? SelectedKontrahentId
    {
        get => _invoice.OdbiorcaId;
        set
        {
            _invoice.OdbiorcaId = value;
            OnPropertyChanged();
        }
    }

    public string? SelectedKontrahentNazwa
    {
        get => _invoice.OdbiorcaNazwa;
        set
        {
            _invoice.OdbiorcaNazwa = value;
            OnPropertyChanged();
        }
    }

    public string? SelectedKontrahentEmail
    {
        get => _invoice.OdbiorcaEmail;
        set
        {
            _invoice.OdbiorcaEmail = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(OdbiorcaEmail));
        }
    }

    public string? SelectedKontrahentWaluta
    {
        get => _invoice.Waluta;
        set
        {
            _invoice.Waluta = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Waluta));
        }
    }

    public string? OdbiorcaEmail
    {
        get => _invoice.OdbiorcaEmail;
        set
        {
            _invoice.OdbiorcaEmail = value;
            OnPropertyChanged();
        }
    }

    public string? Waluta
    {
        get => _invoice.Waluta;
        set
        {
            _invoice.Waluta = value;
            OnPropertyChanged();
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand PickKontrahentCommand { get; }

    private async Task SaveAsync()
    {
        try
        {
            await _invoiceRepository.UpdateRecipientAsync(
                _invoice.Id,
                _invoice.CompanyId,
                _invoice.OdbiorcaId,
                _invoice.OdbiorcaNazwa,
                _invoice.OdbiorcaEmail,
                _invoice.Waluta);
            OnSaved();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas zapisywania faktury: {ex.Message}",
                "Faktury",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void PickKontrahent()
    {
        if (System.Windows.Application.Current is not App app)
            return;
        var viewModel = app.GetService<KontrahenciViewModel>();
        var window = new KontrahenciPickerWindow(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (window.ShowDialog() == true && window.SelectedKontrahent != null)
        {
            var selected = window.SelectedKontrahent;
            SelectedKontrahentId = selected.Id;
            SelectedKontrahentNazwa = selected.Nazwa;
            SelectedKontrahentEmail = selected.Email;
            SelectedKontrahentWaluta = selected.Waluta;
        }
    }

    protected virtual void OnSaved() => Saved?.Invoke(this, EventArgs.Empty);
    protected virtual void OnCancelled() => Cancelled?.Invoke(this, EventArgs.Empty);
}
