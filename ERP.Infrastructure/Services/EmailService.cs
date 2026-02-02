using System.Net;
using System.Net.Mail;
using ERP.Application.DTOs;
using ERP.Application.Services;
using Microsoft.Extensions.Configuration;

namespace ERP.Infrastructure.Services;

/// <summary>
/// Wysyłka e-mail przez SMTP (System.Net.Mail). Konfiguracja: Smtp (global) lub Smtp:Companies:{companyId} (per firma).
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task SendWithSmtpSettingsAsync(SmtpSettingsDto smtp, string to, string? cc, string subject, string body, byte[]? attachmentBytes = null, string? attachmentFileName = null, CancellationToken cancellationToken = default)
    {
        var host = (smtp.Host ?? "").Trim();
        var port = smtp.Port > 0 ? smtp.Port : 25;
        var enableSsl = smtp.Ssl;
        var fromEmail = (smtp.FromEmail ?? "").Trim();
        var fromName = (smtp.FromName ?? "").Trim();

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Credentials = !string.IsNullOrEmpty(smtp.User) ? new NetworkCredential(smtp.User, smtp.Pass ?? "") : null
        };

        var from = string.IsNullOrEmpty(fromName) ? new MailAddress(fromEmail) : new MailAddress(fromEmail, fromName);
        var mail = new MailMessage
        {
            From = from,
            Subject = subject ?? "",
            Body = body ?? "",
            IsBodyHtml = false
        };
        mail.To.Add(to);
        if (!string.IsNullOrWhiteSpace(cc))
            mail.CC.Add(cc);

        if (attachmentBytes != null && attachmentBytes.Length > 0 && !string.IsNullOrWhiteSpace(attachmentFileName))
        {
            var stream = new MemoryStream(attachmentBytes);
            try
            {
                mail.Attachments.Add(new Attachment(stream, attachmentFileName, "application/pdf"));
                await client.SendMailAsync(mail).ConfigureAwait(false);
            }
            finally
            {
                stream.Dispose();
            }
        }
        else
        {
            await client.SendMailAsync(mail).ConfigureAwait(false);
        }
    }

    public bool HasSmtpConfigForCompany(int companyId)
    {
        var companyPrefix = $"Smtp:Companies:{companyId}";
        var companyHost = _config[$"{companyPrefix}:Host"];
        if (!string.IsNullOrWhiteSpace(companyHost))
            return !string.IsNullOrWhiteSpace(_config[$"{companyPrefix}:FromAddress"]);
        var globalHost = _config["Smtp:Host"];
        var globalFrom = _config["Smtp:FromAddress"];
        return !string.IsNullOrWhiteSpace(globalHost) && !string.IsNullOrWhiteSpace(globalFrom);
    }

    public async Task SendAsync(string to, string? cc, string subject, string body, byte[]? attachmentBytes = null, string? attachmentFileName = null, int? companyId = null, CancellationToken cancellationToken = default)
    {
        string prefix;
        if (companyId.HasValue)
        {
            var companyPrefix = $"Smtp:Companies:{companyId.Value}";
            var companyHost = _config[$"{companyPrefix}:Host"];
            if (!string.IsNullOrWhiteSpace(companyHost))
                prefix = companyPrefix;
            else
                prefix = "Smtp";
        }
        else
        {
            prefix = "Smtp";
        }

        var host = _config[$"{prefix}:Host"];
        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException(companyId.HasValue
                ? $"Brak konfiguracji SMTP dla firmy (firmy.id={companyId}). Skonfiguruj Smtp:Companies:{companyId} lub sekcję Smtp w appsettings.json."
                : "Smtp:Host brak w konfiguracji.");

        var port = int.Parse(_config[$"{prefix}:Port"] ?? "25");
        var user = _config[$"{prefix}:User"];
        var pass = _config[$"{prefix}:Pass"];
        var enableSsl = string.Equals(_config[$"{prefix}:EnableSsl"], "true", StringComparison.OrdinalIgnoreCase);
        var fromAddress = _config[$"{prefix}:FromAddress"];
        if (string.IsNullOrWhiteSpace(fromAddress))
            throw new InvalidOperationException(companyId.HasValue
                ? $"Brak FromAddress w konfiguracji SMTP dla firmy (firmy.id={companyId})."
                : "Smtp:FromAddress brak w konfiguracji.");

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Credentials = !string.IsNullOrEmpty(user) ? new NetworkCredential(user, pass ?? "") : null
        };

        var mail = new MailMessage
        {
            From = new MailAddress(fromAddress),
            Subject = subject ?? "",
            Body = body ?? "",
            IsBodyHtml = false
        };
        mail.To.Add(to);
        if (!string.IsNullOrWhiteSpace(cc))
            mail.CC.Add(cc);

        if (attachmentBytes != null && attachmentBytes.Length > 0 && !string.IsNullOrWhiteSpace(attachmentFileName))
        {
            var stream = new MemoryStream(attachmentBytes);
            try
            {
                mail.Attachments.Add(new Attachment(stream, attachmentFileName, "application/pdf"));
                await client.SendMailAsync(mail).ConfigureAwait(false);
            }
            finally
            {
                stream.Dispose();
            }
        }
        else
        {
            await client.SendMailAsync(mail).ConfigureAwait(false);
        }
    }
}
