namespace ERP.Application.DTOs;

/// <summary>
/// DTO reprezentujące dane firmy
/// </summary>
public class CompanyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Header1 { get; set; }
    public string? Header2 { get; set; }
    public string? Street { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Nip { get; set; }
    public string? Regon { get; set; }
    public string? Krs { get; set; }
    public string? Phone1 { get; set; }
    public string? Email { get; set; }
    public int? RoleId { get; set; } // Rola użytkownika w tej firmie
    public bool IsDefault { get; set; } // Czy to domyślna firma użytkownika

    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUser { get; set; }
    public string? SmtpPass { get; set; }
    public bool? SmtpSsl { get; set; }
    public string? SmtpFromEmail { get; set; }
    public string? SmtpFromName { get; set; }
}
