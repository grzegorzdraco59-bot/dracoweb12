using ERP.Application.DTOs;

namespace ERP.Application.Services;

/// <summary>
/// Wysyłka e-mail (SMTP) – np. oferta z załącznikiem PDF. SMTP z tabeli firmy lub z appsettings.
/// </summary>
public interface IEmailService
{
    /// <summary>Zwraca true, gdy w appsettings jest konfiguracja SMTP dla danej firmy (Smtp:Companies:{companyId} lub Smtp).</summary>
    bool HasSmtpConfigForCompany(int companyId);

    /// <summary>Wysyła e-mail używając ustawień SMTP z firmy (tabela firmy). Host/port/ssl z smtp; From = FromEmail + FromName.</summary>
    Task SendWithSmtpSettingsAsync(SmtpSettingsDto smtp, string to, string? cc, string subject, string body, byte[]? attachmentBytes = null, string? attachmentFileName = null, CancellationToken cancellationToken = default);

    /// <summary>Wysyła e-mail z załącznikiem (konfiguracja z appsettings). Gdy companyId podane – szuka Smtp:Companies:{companyId}; inaczej Smtp.</summary>
    Task SendAsync(string to, string? cc, string subject, string body, byte[]? attachmentBytes = null, string? attachmentFileName = null, int? companyId = null, CancellationToken cancellationToken = default);
}
