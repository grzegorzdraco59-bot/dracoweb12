namespace ERP.Application.DTOs;

/// <summary>
/// Lekki DTO do lookupów kontrahentów (widok kontrahenci_v).
/// </summary>
public class KontrahentLookupDto
{
    public int Id { get; set; }
    public int? KontrahentId { get; set; }
    public int? CompanyId { get; set; }
    public string? Typ { get; set; } // 'O' lub 'D'
    public string? Nazwa { get; set; }
    public string? UlicaINr { get; set; }
    public string? KodPocztowy { get; set; }
    public string? Panstwo { get; set; }
    public string? Nip { get; set; }
    public string? Email { get; set; }
    public string? Telefon { get; set; }
    public string? Miasto { get; set; }
    public string? Waluta { get; set; }
}
