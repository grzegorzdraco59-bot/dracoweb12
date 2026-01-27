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
}
