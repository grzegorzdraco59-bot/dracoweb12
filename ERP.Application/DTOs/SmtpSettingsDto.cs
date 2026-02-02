namespace ERP.Application.DTOs;

/// <summary>
/// Ustawienia SMTP (np. z tabeli firmy) do wysyłki e-mail.
/// </summary>
public class SmtpSettingsDto
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 25;
    public string? User { get; set; }
    public string? Pass { get; set; }
    public bool Ssl { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string? FromName { get; set; }
}
