namespace ERP.Application.DTOs;

/// <summary>
/// DTO reprezentujące dane użytkownika (operator)
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public int DefaultCompanyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int Permissions { get; set; }
}
