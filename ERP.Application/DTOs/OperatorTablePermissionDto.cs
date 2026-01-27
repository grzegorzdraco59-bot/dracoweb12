namespace ERP.Application.DTOs;

/// <summary>
/// DTO dla uprawnień operatora do tabel
/// </summary>
public class OperatorTablePermissionDto
{
    public int Id { get; set; }
    public int OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public bool CanSelect { get; set; }
    public bool CanInsert { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
