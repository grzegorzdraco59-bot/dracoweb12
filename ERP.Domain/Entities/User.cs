namespace ERP.Domain.Entities;

/// <summary>
/// Encja domenowa reprezentująca użytkownika systemu (operator)
/// Mapuje do tabeli: operator
/// </summary>
public class User : BaseEntity
{
    public int DefaultCompanyId { get; private set; }
    public string FullName { get; private set; }
    public int Permissions { get; private set; }
    public string SenderEmail { get; private set; }
    public string SenderUserName { get; private set; }
    public string SenderEmailServer { get; private set; }
    public string SenderEmailPassword { get; private set; }
    public string MessageText { get; private set; }
    public string CcAddress { get; private set; }

    // Konstruktor prywatny dla EF Core
    private User()
    {
        FullName = string.Empty;
        SenderEmail = string.Empty;
        SenderUserName = string.Empty;
        SenderEmailServer = string.Empty;
        SenderEmailPassword = string.Empty;
        MessageText = string.Empty;
        CcAddress = string.Empty;
    }

    // Główny konstruktor
    public User(int defaultCompanyId, string fullName, int permissions = 0)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Imię i nazwisko nie może być puste.", nameof(fullName));

        DefaultCompanyId = defaultCompanyId;
        FullName = fullName;
        Permissions = permissions;
        SenderEmail = string.Empty;
        SenderUserName = string.Empty;
        SenderEmailServer = string.Empty;
        SenderEmailPassword = string.Empty;
        MessageText = string.Empty;
        CcAddress = string.Empty;
    }

    public void UpdateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Imię i nazwisko nie może być puste.", nameof(fullName));

        FullName = fullName;
        UpdateTimestamp();
    }

    public void UpdatePermissions(int permissions)
    {
        Permissions = permissions;
        UpdateTimestamp();
    }

    public void UpdateEmailSettings(string senderEmail, string senderUserName, string senderEmailServer, string senderEmailPassword)
    {
        SenderEmail = senderEmail ?? string.Empty;
        SenderUserName = senderUserName ?? string.Empty;
        SenderEmailServer = senderEmailServer ?? string.Empty;
        SenderEmailPassword = senderEmailPassword ?? string.Empty;
        UpdateTimestamp();
    }
}
