namespace ERP.Application.DTOs;

/// <summary>
/// DTO (Data Transfer Object) dla OfferPosition - używane w warstwie aplikacji i UI.
/// Id i OfferId mapują na ofertypozycje.id i ofertypozycje.oferta_id.
/// </summary>
public class OfferPositionDto
{
    /// <summary>Mapuje na ofertypozycje.id (PK).</summary>
    public long Id { get; set; }
    public int CompanyId { get; set; }
    /// <summary>Mapuje na ofertypozycje.oferta_id (FK do oferty.id).</summary>
    public long OfferId { get; set; }
    public int? ProductId { get; set; }
    public int? SupplierId { get; set; }
    public string? ProductCode { get; set; }
    public string? Name { get; set; }
    public string? NameEng { get; set; }
    public string Unit { get; set; } = "szt";
    public string? UnitEng { get; set; }
    public decimal? Ilosc { get; set; }
    public decimal? CenaNetto { get; set; }
    public decimal? Discount { get; set; }
    public decimal? PriceAfterDiscount { get; set; }
    public decimal? NettoPoz { get; set; }
    public string? VatRate { get; set; }
    public decimal? VatPoz { get; set; }
    public decimal? BruttoPoz { get; set; }
    public string? OfferNotes { get; set; }
    public string? InvoiceNotes { get; set; }
    public string? Other1 { get; set; }
    public decimal? GroupNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
