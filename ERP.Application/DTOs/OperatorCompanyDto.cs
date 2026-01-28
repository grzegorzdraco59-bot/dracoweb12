namespace ERP.Application.DTOs;

/// <summary>
/// DTO powiązania operator–firma (tabela operatorfirma: id, id_operatora, id_firmy, rola).
/// </summary>
public class OperatorCompanyDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CompanyId { get; set; }
    public int? RoleId { get; set; }
}
