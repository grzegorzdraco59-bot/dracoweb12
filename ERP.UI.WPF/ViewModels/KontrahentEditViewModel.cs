using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Repositories;

namespace ERP.UI.WPF.ViewModels;

public class KontrahentEditViewModel : ViewModelBase
{
    private readonly IKontrahenciCommandRepository _repo;
    private readonly int _companyId;
    private readonly int _kontrahentId;
    private string? _typ;
    private string? _nazwa;
    private string? _email;
    private string? _telefon;
    private string? _miasto;
    private string? _waluta;

    public KontrahentEditViewModel(IKontrahenciCommandRepository repo, KontrahentLookupDto kontrahent)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        if (kontrahent.CompanyId == null || kontrahent.KontrahentId == null)
            throw new ArgumentException("Brak companyId lub kontrahentId.", nameof(kontrahent));

        _companyId = kontrahent.CompanyId.Value;
        _kontrahentId = kontrahent.KontrahentId.Value;
        _typ = kontrahent.Typ;
        _nazwa = kontrahent.Nazwa;
        _email = kontrahent.Email;
        _telefon = kontrahent.Telefon;
        _miasto = kontrahent.Miasto;
        _waluta = kontrahent.Waluta;

        SaveCommand = new RelayCommand(async () => await SaveAsync(), CanSave);
        CancelCommand = new RelayCommand(() => OnCancelled());
    }

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    public string? Typ
    {
        get => _typ;
        set { _typ = value; OnPropertyChanged(); }
    }

    public string? Nazwa
    {
        get => _nazwa;
        set
        {
            _nazwa = value;
            OnPropertyChanged();
            if (SaveCommand is RelayCommand cmd)
                cmd.RaiseCanExecuteChanged();
        }
    }

    public string? Email
    {
        get => _email;
        set { _email = value; OnPropertyChanged(); }
    }

    public string? Telefon
    {
        get => _telefon;
        set { _telefon = value; OnPropertyChanged(); }
    }

    public string? Miasto
    {
        get => _miasto;
        set { _miasto = value; OnPropertyChanged(); }
    }

    public string? Waluta
    {
        get => _waluta;
        set { _waluta = value; OnPropertyChanged(); }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(_nazwa);
    }

    private async Task SaveAsync()
    {
        try
        {
            var effectiveTyp = string.IsNullOrWhiteSpace(_typ) ? "O" : _typ;
            await _repo.UpdateAsync(_companyId, _kontrahentId, effectiveTyp, _nazwa, _email, _telefon, _miasto, _waluta);
            OnSaved();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas zapisywania kontrahenta: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    protected virtual void OnSaved() => Saved?.Invoke(this, EventArgs.Empty);
    protected virtual void OnCancelled() => Cancelled?.Invoke(this, EventArgs.Empty);
}
