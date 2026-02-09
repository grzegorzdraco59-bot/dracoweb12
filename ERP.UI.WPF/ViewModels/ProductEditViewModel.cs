using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Infrastructure.Repositories;
using ERP.UI.WPF.Views;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla okna edycji towaru – wszystkie pola tabeli towary.
/// </summary>
public class ProductEditViewModel : ViewModelBase
{
    private readonly ProductRepository _repository;
    private readonly int _companyId;
    private ProductDto _product;

    public ProductEditViewModel(ProductRepository repository, ProductDto product, int companyId)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _companyId = companyId;
        _product = product ?? throw new ArgumentNullException(nameof(product));

        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => CanSave());
        CancelCommand = new RelayCommand(() => OnCancelled());
        PickKontrahentCommand = new RelayCommand(PickKontrahent);
    }

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    public int Id => _product.Id;
    public int? CompanyId { get => _product.CompanyId; set { _product.CompanyId = value; OnPropertyChanged(); } }

    public string? Group { get => _product.Group; set { _product.Group = value; OnPropertyChanged(); RaiseCanSave(); } }
    public string? GrupaRemanentu { get => _product.GrupaRemanentu; set { _product.GrupaRemanentu = value; OnPropertyChanged(); } }
    public string? StatusTowaru { get => _product.StatusTowaru; set { _product.StatusTowaru = value; OnPropertyChanged(); } }
    public string? NazwaPLdraco { get => _product.NazwaPLdraco; set { _product.NazwaPLdraco = value; OnPropertyChanged(); } }
    public string? NazwaPL { get => _product.NazwaPL; set { _product.NazwaPL = value; OnPropertyChanged(); RaiseCanSave(); } }
    public string? NazwaENG { get => _product.NazwaENG; set { _product.NazwaENG = value; OnPropertyChanged(); } }
    public decimal? Cena_PLN { get => _product.Cena_PLN; set { _product.Cena_PLN = value; OnPropertyChanged(); } }
    public decimal? Cena_EUR { get => _product.Cena_EUR; set { _product.Cena_EUR = value; OnPropertyChanged(); } }
    public decimal? Cena_USD { get => _product.Cena_USD; set { _product.Cena_USD = value; OnPropertyChanged(); } }
    public decimal? Waga_Kg { get => _product.Waga_Kg; set { _product.Waga_Kg = value; OnPropertyChanged(); } }
    public decimal? Roboczogodziny { get => _product.Roboczogodziny; set { _product.Roboczogodziny = value; OnPropertyChanged(); } }
    public string? Uwagi { get => _product.Uwagi; set { _product.Uwagi = value; OnPropertyChanged(); } }
    public string? Dostawca { get => _product.Dostawca; set { _product.Dostawca = value; OnPropertyChanged(); } }
    public decimal? IloscMagazyn { get => _product.IloscMagazyn; set { _product.IloscMagazyn = value; OnPropertyChanged(); } }
    public string? JednostkiZakupu { get => _product.JednostkiZakupu; set { _product.JednostkiZakupu = value; OnPropertyChanged(); } }
    public string? JednostkiSprzedazy { get => _product.JednostkiSprzedazy; set { _product.JednostkiSprzedazy = value; OnPropertyChanged(); } }
    public string? Jednostka { get => _product.Jednostka; set { _product.Jednostka = value; OnPropertyChanged(); } }
    public decimal? PrzelicznikMKg { get => _product.PrzelicznikMKg; set { _product.PrzelicznikMKg = value; OnPropertyChanged(); } }
    public decimal? CenaZakupu { get => _product.CenaZakupu; set { _product.CenaZakupu = value; OnPropertyChanged(); } }
    public string? WalutaZakupu { get => _product.WalutaZakupu; set { _product.WalutaZakupu = value; OnPropertyChanged(); } }
    public decimal? KursWaluty { get => _product.KursWaluty; set { _product.KursWaluty = value; OnPropertyChanged(); } }
    public decimal? CenaZakupuPLN { get => _product.CenaZakupuPLN; set { _product.CenaZakupuPLN = value; OnPropertyChanged(); } }
    public decimal? CenaZakupuPLNNoweJednostki { get => _product.CenaZakupuPLNNoweJednostki; set { _product.CenaZakupuPLNNoweJednostki = value; OnPropertyChanged(); } }
    public decimal? KosztyMaterialow { get => _product.KosztyMaterialow; set { _product.KosztyMaterialow = value; OnPropertyChanged(); } }
    public string? GrupaGtu { get => _product.GrupaGtu; set { _product.GrupaGtu = value; OnPropertyChanged(); } }
    public string? StawkaVat { get => _product.StawkaVat; set { _product.StawkaVat = value; OnPropertyChanged(); } }
    public string? JednostkiEn { get => _product.JednostkiEn; set { _product.JednostkiEn = value; OnPropertyChanged(); } }
    public int? DataZakupu { get => _product.DataZakupu; set { _product.DataZakupu = value; OnPropertyChanged(); } }
    public decimal? IloscWOpakowaniu { get => _product.IloscWOpakowaniu; set { _product.IloscWOpakowaniu = value; OnPropertyChanged(); } }
    public int? LiniaProdukcyjna { get => _product.LiniaProdukcyjna; set { _product.LiniaProdukcyjna = value; OnPropertyChanged(); } }
    public int? IdDostawcy { get => _product.IdDostawcy; set { _product.IdDostawcy = value; OnPropertyChanged(); } }
    public bool? DoMagazynu { get => _product.DoMagazynu; set { _product.DoMagazynu = value; OnPropertyChanged(); } }
    public int? CenaData { get => _product.CenaData; set { _product.CenaData = value; OnPropertyChanged(); } }
    public string? EtykietaNazwa { get => _product.EtykietaNazwa; set { _product.EtykietaNazwa = value; OnPropertyChanged(); } }
    public string? EtykietaWielkosc { get => _product.EtykietaWielkosc; set { _product.EtykietaWielkosc = value; OnPropertyChanged(); } }
    public decimal? IloscJednostkowa { get => _product.IloscJednostkowa; set { _product.IloscJednostkowa = value; OnPropertyChanged(); } }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand PickKontrahentCommand { get; }

    private void RaiseCanSave()
    {
        if (SaveCommand is RelayCommand cmd)
            cmd.RaiseCanExecuteChanged();
    }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(NazwaPL);
    }

    private (bool ok, string? error) Validate()
    {
        if (string.IsNullOrWhiteSpace(NazwaPL))
            return (false, "Nazwa PL jest wymagana.");

        if (Cena_PLN.HasValue && Cena_PLN < 0)
            return (false, "Cena PLN nie może być ujemna.");
        if (Cena_EUR.HasValue && Cena_EUR < 0)
            return (false, "Cena EUR nie może być ujemna.");
        if (Cena_USD.HasValue && Cena_USD < 0)
            return (false, "Cena USD nie może być ujemna.");
        if (Waga_Kg.HasValue && Waga_Kg < 0)
            return (false, "Waga nie może być ujemna.");
        if (IloscMagazyn.HasValue && IloscMagazyn < 0)
            return (false, "Ilość magazynowa nie może być ujemna.");
        if (CenaZakupu.HasValue && CenaZakupu < 0)
            return (false, "Cena zakupu nie może być ujemna.");
        if (CenaZakupuPLN.HasValue && CenaZakupuPLN < 0)
            return (false, "Cena zakupu PLN nie może być ujemna.");
        if (PrzelicznikMKg.HasValue && PrzelicznikMKg < 0)
            return (false, "Przelicznik m/kg nie może być ujemny.");
        if (KosztyMaterialow.HasValue && KosztyMaterialow < 0)
            return (false, "Koszty materiałów nie mogą być ujemne.");

        return (true, null);
    }

    private async Task SaveAsync()
    {
        var (ok, error) = Validate();
        if (!ok)
        {
            System.Windows.MessageBox.Show(error, "Błąd walidacji", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Ustaw company_id dla nowego towaru
            if (_product.Id == 0)
            {
                _product.CompanyId = _companyId;
            }

            await _repository.SaveAsync(_product);
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
            Dostawca = selected.Nazwa;
            IdDostawcy = selected.Id;
        }
    }
}
