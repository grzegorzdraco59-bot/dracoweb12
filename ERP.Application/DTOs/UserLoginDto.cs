namespace ERP.Application.DTOs;

/// <summary>
/// DTO reprezentujące dane logowania operatora
/// </summary>
public class UserLoginDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}
