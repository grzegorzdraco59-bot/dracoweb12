namespace ERP.Application.DTOs;

/// <summary>
/// DTO dla pozycji zamówienia z tabeli pozyjezamowienia
/// </summary>
public class OrderPositionMainDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int OrderId { get; set; }
    public int? ProductId { get; set; }
    public int? DeliveryDateInt { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? ProductNameDraco { get; set; }
    public string? Product { get; set; }
    public string? ProductNameEng { get; set; }
    public string? OrderUnit { get; set; }
    public decimal? OrderQuantity { get; set; }
    public decimal? DeliveredQuantity { get; set; }
    public decimal? OrderPrice { get; set; }
    public string? ProductStatus { get; set; }
    public string? PurchaseUnit { get; set; }
    public decimal? PurchaseQuantity { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? PurchaseValue { get; set; }
    public decimal? PurchasePricePln { get; set; }
    public decimal? ConversionFactor { get; set; }
    public decimal? PurchasePricePlnNewUnit { get; set; }
    public string? Notes { get; set; }
    public string? Supplier { get; set; }
    public string? VatRate { get; set; }
    public decimal? UnitWeight { get; set; }
    public decimal? QuantityInPackage { get; set; }
    public int? OrderHalaId { get; set; }
    public int? OfferPositionId { get; set; }
    public int? MarkForCopying { get; set; }
    public bool? CopiedToWarehouse { get; set; }
    public decimal? Length { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
