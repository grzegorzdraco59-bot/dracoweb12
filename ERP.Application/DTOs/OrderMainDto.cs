namespace ERP.Application.DTOs;

/// <summary>
/// DTO dla zamówienia z tabeli zamowienia
/// </summary>
public class OrderMainDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int? OrderNumber { get; set; }
    public DateTime? OrderDate { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? Notes { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Pola dla kolumn z zamowienia (lista)
    public int? IdZamowienia { get; set; }
    public DateTime? DataZamowienia { get; set; }
    public int? Nr { get; set; }
    public string? StatusSkrot { get; set; }
    public string? Skrot { get; set; }
    public string? DostawcaNazwa { get; set; }
    public string? Waluta { get; set; }
    public DateTime? DataDostawy { get; set; }
    public string? StatusZamowienia { get; set; }
    public string? StatusPlatnosci { get; set; }
    public DateTime? DataPlatnosci { get; set; }
    public string? NrFaktury { get; set; }
    public DateTime? DataFaktury { get; set; }
    public decimal? Wartosc { get; set; }
    public string? Uwagi { get; set; }
    public string? DlaKogo { get; set; }
    public string? TabelaNbp { get; set; }
    public decimal? Kurs { get; set; }
    public string? SupplierEmail { get; set; }
    public string? DataTabeliNbp { get; set; }
    public bool? SkopiowanoNiedostarczone { get; set; }
    public bool? SkopiowanoDoMagazynu { get; set; }

    /// <summary>Data zamówienia – alias dla bindingu.</summary>
    public DateTime? OrderDateDisplay => DataZamowienia ?? OrderDate;
}
