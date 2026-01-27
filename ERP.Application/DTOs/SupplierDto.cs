namespace ERP.Application.DTOs;

/// <summary>
/// DTO dla dostawcy z tabeli dostawcy
/// </summary>
public class SupplierDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = "PLN";
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
