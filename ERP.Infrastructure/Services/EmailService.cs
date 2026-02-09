using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using ERP.Application.DTOs;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
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
        var port = 465; // KROK 13 – wymuszony port 465 / SSL
        var smtpUser = (smtp.User ?? "").Trim();
        var fromEmail = smtpUser; // KROK 12 – test SPF: FROM = smtpUser (zgodna domena)
        var toEmail = (to ?? "").Trim();
        if (string.IsNullOrEmpty(toEmail) || !toEmail.Contains("@"))
        {
#if WINDOWS
            System.Windows.MessageBox.Show("Brak adresu email odbiorcy", "SMTP", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
#endif
            return;
        }
        var smtpPassword = smtp.Pass ?? "";

        // TEMP SMTP DEBUG – test TCP connectivity
        try
        {
            using var tcp = new TcpClient();
            var connectTask = tcp.ConnectAsync(host, port);
            var completed = await Task.WhenAny(connectTask, Task.Delay(5000));
            if (completed != connectTask)
            {
#if WINDOWS
                System.Windows.MessageBox.Show($"SMTP TCP FAIL: timeout 5s {host}:{port}", "SMTP DEBUG");
#endif
                return;
            }
#if WINDOWS
            System.Windows.MessageBox.Show($"SMTP TCP OK: {host}:{port}", "SMTP DEBUG");
#endif
        }
        catch (Exception ex)
        {
#if WINDOWS
            System.Windows.MessageBox.Show($"SMTP TCP ERROR: {host}:{port}\n{ex}", "SMTP DEBUG");
#endif
            return;
        }

        // TEMP MAILKIT TEST – zastępuje SmtpClient
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject ?? "";
        var builder = new BodyBuilder { TextBody = body ?? "" };
        if (attachmentBytes != null && attachmentBytes.Length > 0 && !string.IsNullOrWhiteSpace(attachmentFileName))
            builder.Attachments.Add(attachmentFileName, attachmentBytes);
        message.Body = builder.ToMessageBody();

        try
        {
#if WINDOWS
            System.Windows.MessageBox.Show("MAILKIT: BEFORE CONNECT", "SMTP DEBUG");
#endif

            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "smtp_log.txt"
            );
            using var logStream = File.Create(logPath);
            using var protocolLogger = new ProtocolLogger(logStream);
            using var client = new MailKit.Net.Smtp.SmtpClient(protocolLogger);
            client.Timeout = 15000;
            client.CheckCertificateRevocation = false;

            await client.ConnectAsync(host, port, SecureSocketOptions.SslOnConnect);
#if WINDOWS
            System.Windows.MessageBox.Show("MAILKIT: CONNECTED", "SMTP DEBUG");
#endif

            await client.AuthenticateAsync(smtpUser, smtpPassword);
#if WINDOWS
            System.Windows.MessageBox.Show("MAILKIT: AUTH OK", "SMTP DEBUG");
#endif

#if WINDOWS
            System.Windows.MessageBox.Show("TO=" + string.Join(";", message.To.Mailboxes.Select(x => x.Address)), "SMTP DEBUG");
#endif
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            logStream.Flush();
#if WINDOWS
            System.Windows.MessageBox.Show("MAILKIT SEND RESPONSE: OK\nLOG: " + logPath, "SMTP DEBUG");
#endif
        }
        catch (Exception ex)
        {
#if WINDOWS
            System.Windows.MessageBox.Show(ex.ToString(), "MAILKIT ERROR");
#endif
            throw;
        }

        /* ZAKOMENTOWANY SmtpClient – zastąpiony MailKit
        using var client = new SmtpClient(host, port) { ... };
        var mail = new MailMessage { ... };
        await client.SendMailAsync(mail);
        */
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

        var port = 465; // KROK 13 – wymuszony port 465 / SSL
        var smtpUser = (_config[$"{prefix}:User"] ?? "").Trim();
        var smtpPassword = _config[$"{prefix}:Pass"] ?? "";
        var fromAddress = smtpUser; // KROK 12 – test SPF: FROM = smtpUser (zgodna domena)
        if (string.IsNullOrWhiteSpace(smtpUser))
            throw new InvalidOperationException(companyId.HasValue
                ? $"Brak User w konfiguracji SMTP dla firmy (firmy.id={companyId})."
                : "Smtp:User brak w konfiguracji.");

        var toEmail = (to ?? "").Trim();
        if (string.IsNullOrEmpty(toEmail) || !toEmail.Contains("@"))
        {
#if WINDOWS
            System.Windows.MessageBox.Show("Brak adresu email odbiorcy", "SMTP", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
#endif
            return;
        }

        // TEMP SMTP DEBUG – test TCP connectivity
        try
        {
            using var tcp = new TcpClient();
            var connectTask = tcp.ConnectAsync(host, port);
            var completed = await Task.WhenAny(connectTask, Task.Delay(5000));
            if (completed != connectTask)
            {
#if WINDOWS
                System.Windows.MessageBox.Show($"SMTP TCP FAIL: timeout 5s {host}:{port}", "SMTP DEBUG");
#endif
                return;
            }
#if WINDOWS
            System.Windows.MessageBox.Show($"SMTP TCP OK: {host}:{port}", "SMTP DEBUG");
#endif
        }
        catch (Exception ex)
        {
#if WINDOWS
            System.Windows.MessageBox.Show($"SMTP TCP ERROR: {host}:{port}\n{ex}", "SMTP DEBUG");
#endif
            return;
        }

        // TEMP MAILKIT TEST – zastępuje SmtpClient
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(fromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject ?? "";
        var builder = new BodyBuilder { TextBody = body ?? "" };
        if (attachmentBytes != null && attachmentBytes.Length > 0 && !string.IsNullOrWhiteSpace(attachmentFileName))
            builder.Attachments.Add(attachmentFileName, attachmentBytes);
        message.Body = builder.ToMessageBody();

        try
        {
#if WINDOWS
            System.Windows.MessageBox.Show("MAILKIT: BEFORE CONNECT", "SMTP DEBUG");
#endif

            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "smtp_log.txt"
            );
            using var logStream = File.Create(logPath);
            using var protocolLogger = new ProtocolLogger(logStream);
            using var client = new MailKit.Net.Smtp.SmtpClient(protocolLogger);
            client.Timeout = 15000;
            client.CheckCertificateRevocation = false;

            await client.ConnectAsync(host, port, SecureSocketOptions.SslOnConnect);
#if WINDOWS
            System.Windows.MessageBox.Show("MAILKIT: CONNECTED", "SMTP DEBUG");
#endif

            await client.AuthenticateAsync(smtpUser, smtpPassword);
#if WINDOWS
            System.Windows.MessageBox.Show("MAILKIT: AUTH OK", "SMTP DEBUG");
#endif

#if WINDOWS
            System.Windows.MessageBox.Show("TO=" + string.Join(";", message.To.Mailboxes.Select(x => x.Address)), "SMTP DEBUG");
#endif
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            logStream.Flush();
#if WINDOWS
            System.Windows.MessageBox.Show("MAILKIT SEND RESPONSE: OK\nLOG: " + logPath, "SMTP DEBUG");
#endif
        }
        catch (Exception ex)
        {
#if WINDOWS
            System.Windows.MessageBox.Show(ex.ToString(), "MAILKIT ERROR");
#endif
            throw;
        }

        /* ZAKOMENTOWANY SmtpClient – zastąpiony MailKit */
    }
}
