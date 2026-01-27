namespace ERP.Application.DTOs;

/// <summary>
/// DTO dla pozycji magazynu
/// </summary>
public class WarehouseItemDto
{
    public int Id { get; set; }
    public int? CompanyId { get; set; }
    public string? OperationDate { get; set; }
    public string? DocumentNumber { get; set; }
    public string? SupplierCustomerName { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? Unit { get; set; }
    public decimal? QuantityIn { get; set; }
    public decimal? QuantityOut { get; set; }
    public decimal? Price { get; set; }
}
