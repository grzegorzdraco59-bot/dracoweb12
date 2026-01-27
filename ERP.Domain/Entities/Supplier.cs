namespace ERP.Domain.Entities;

/// <summary>
/// Encja domenowa reprezentująca dostawcę z tabeli dostawcy
/// </summary>
public class Supplier : BaseEntity
{
    public int CompanyId { get; private set; }
    public string Name { get; private set; }
    public string Currency { get; private set; }
    public string? Email { get; private set; }
    public string Phone { get; private set; }
    public string? Notes { get; private set; }

    // Konstruktor prywatny dla EF Core
    private Supplier()
    {
        Name = string.Empty;
        Currency = "PLN";
        Phone = string.Empty;
    }

    // Główny konstruktor
    public Supplier(int companyId, string name, string phone, string currency = "PLN")
    {
        CompanyId = companyId;
        Name = name ?? string.Empty;
        Phone = phone ?? string.Empty;
        Currency = currency;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Nazwa dostawcy nie może być pusta.", nameof(newName));

        Name = newName;
        UpdateTimestamp();
    }

    public void UpdateContactInfo(string? email, string phone)
    {
        Email = email;
        Phone = phone ?? string.Empty;
        UpdateTimestamp();
    }

    public void UpdateCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Waluta nie może być pusta.", nameof(currency));

        Currency = currency;
        UpdateTimestamp();
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdateTimestamp();
    }
}
