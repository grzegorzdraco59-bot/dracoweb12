namespace ERP.Application.DTOs;

/// <summary>
/// DTO dla towaru
/// </summary>
public class ProductDto
{
    public int Id { get; set; }
    public int? CompanyId { get; set; }
    public string? Group { get; set; }
    public string? NamePl { get; set; }
    public string? NameEng { get; set; }
    public decimal? PricePln { get; set; }
    public string? Unit { get; set; }
    public string? Status { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
}
