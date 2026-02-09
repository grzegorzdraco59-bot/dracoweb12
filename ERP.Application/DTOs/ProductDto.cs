using System.ComponentModel.DataAnnotations;

namespace ERP.Application.DTOs;

/// <summary>
/// DTO dla towaru (tabela towary, widok towary_V).
/// Mapowanie 1:1 na kolumny tabeli towary (poza obliczonymi).
/// </summary>
public class ProductDto
{
    [Key]
    public int Id { get; set; }

    public int? CompanyId { get; set; }
    public string? Group { get; set; }
    public string? GrupaRemanentu { get; set; }
    public string? StatusTowaru { get; set; }
    public string? NazwaPLdraco { get; set; }
    public string? NazwaPL { get; set; }
    public string? NazwaENG { get; set; }
    public decimal? Cena_PLN { get; set; }
    public decimal? Cena_EUR { get; set; }
    public decimal? Cena_USD { get; set; }
    public decimal? Waga_Kg { get; set; }
    public decimal? Roboczogodziny { get; set; }
    public string? Uwagi { get; set; }
    public string? Dostawca { get; set; }
    public decimal? IloscMagazyn { get; set; }
    public string? JednostkiZakupu { get; set; }
    public string? JednostkiSprzedazy { get; set; }
    public string? Jednostka { get; set; }
    public decimal? PrzelicznikMKg { get; set; }
    public decimal? CenaZakupu { get; set; }
    public string? WalutaZakupu { get; set; }
    public decimal? KursWaluty { get; set; }
    public decimal? CenaZakupuPLN { get; set; }
    public decimal? CenaZakupuPLNNoweJednostki { get; set; }
    public decimal? KosztyMaterialow { get; set; }
    public string? GrupaGtu { get; set; }
    public string? StawkaVat { get; set; }
    public string? JednostkiEn { get; set; }
    public int? DataZakupu { get; set; }
    public decimal? IloscWOpakowaniu { get; set; }
    public int? LiniaProdukcyjna { get; set; }
    public int? IdDostawcy { get; set; }
    public bool? DoMagazynu { get; set; }
    public int? CenaData { get; set; }
    public string? EtykietaNazwa { get; set; }
    public string? EtykietaWielkosc { get; set; }
    public decimal? IloscJednostkowa { get; set; }

    // Alias dla kompatybilności z listą (np. OffersViewModel)
    public string? NamePl { get => NazwaPL; set => NazwaPL = value; }
    public string? NameEng { get => NazwaENG; set => NazwaENG = value; }
    public decimal? PricePln { get => Cena_PLN; set => Cena_PLN = value; }
    public decimal? PriceEur { get => Cena_EUR; set => Cena_EUR = value; }
    public decimal? PriceUsd { get => Cena_USD; set => Cena_USD = value; }
    public string? Unit { get => JednostkiSprzedazy; set => JednostkiSprzedazy = value; }
    public string? Status { get => StatusTowaru; set => StatusTowaru = value; }
    public int? SupplierId { get => IdDostawcy; set => IdDostawcy = value; }
    public string? SupplierName { get => Dostawca; set => Dostawca = value; }
}
