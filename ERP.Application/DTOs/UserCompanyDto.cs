namespace ERP.Application.DTOs;

/// <summary>
/// DTO reprezentujące powiązanie operatora z firmą
/// </summary>
public class UserCompanyDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CompanyId { get; set; }
    public int? RoleId { get; set; }
}
