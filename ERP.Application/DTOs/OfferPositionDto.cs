namespace ERP.Application.DTOs;

/// <summary>
/// DTO (Data Transfer Object) dla OfferPosition - używane w warstwie aplikacji i UI
/// </summary>
public class OfferPositionDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int OfferId { get; set; }
    public int? ProductId { get; set; }
    public int? SupplierId { get; set; }
    public string? ProductCode { get; set; }
    public string? Name { get; set; }
    public string? NameEng { get; set; }
    public string Unit { get; set; } = "szt";
    public string? UnitEng { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? Price { get; set; }
    public decimal? Discount { get; set; }
    public decimal? PriceAfterDiscount { get; set; }
    public decimal? PriceAfterDiscountAndQuantity { get; set; }
    public string? VatRate { get; set; }
    public decimal? Vat { get; set; }
    public decimal? PriceBrutto { get; set; }
    public string? OfferNotes { get; set; }
    public string? InvoiceNotes { get; set; }
    public string? Other1 { get; set; }
    public decimal? GroupNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
