namespace ERP.Domain.Entities;

/// <summary>
/// Encja domenowa reprezentująca firmę
/// Mapuje do tabeli: firmy
/// </summary>
public class Company : BaseEntity
{
    public string Name { get; private set; }
    public string? Header1 { get; private set; }
    public string? Header2 { get; private set; }
    public string? Street { get; private set; }
    public string? PostalCode { get; private set; }
    public string? City { get; private set; }
    public string? Country { get; private set; }
    public string? Nip { get; private set; }
    public string? Regon { get; private set; }
    public string? Krs { get; private set; }
    public string? Phone1 { get; private set; }
    public string? Email { get; private set; }
    public bool InvoiceNumberingPerMonth { get; private set; }
    public bool ProformaInvoiceNumberingPerMonth { get; private set; }

    // Konstruktor prywatny dla EF Core
    private Company()
    {
        Name = string.Empty;
    }

    // Główny konstruktor
    public Company(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nazwa firmy nie może być pusta.", nameof(name));

        Name = name;
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Nazwa firmy nie może być pusta.", nameof(newName));

        Name = newName;
        UpdateTimestamp();
    }

    public void UpdateAddress(string? street, string? postalCode, string? city, string? country)
    {
        Street = street;
        PostalCode = postalCode;
        City = city;
        Country = country;
        UpdateTimestamp();
    }

    public void UpdateHeaders(string? header1, string? header2)
    {
        Header1 = header1;
        Header2 = header2;
        UpdateTimestamp();
    }

    public void UpdateCompanyData(string? nip, string? regon, string? krs, string? phone1, string? email)
    {
        Nip = nip;
        Regon = regon;
        Krs = krs;
        Phone1 = phone1;
        Email = email;
        UpdateTimestamp();
    }

    public void SetInvoiceNumberingPerMonth(bool value)
    {
        InvoiceNumberingPerMonth = value;
        UpdateTimestamp();
    }

    public void SetProformaInvoiceNumberingPerMonth(bool value)
    {
        ProformaInvoiceNumberingPerMonth = value;
        UpdateTimestamp();
    }
}
