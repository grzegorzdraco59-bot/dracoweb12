namespace ERP.Domain.Entities;

/// <summary>
/// Encja domenowa reprezentująca kontrahenta
/// </summary>
public class Customer : BaseEntity
{
    public int CompanyId { get; private set; }
    public string Name { get; private set; }
    public string? Surname { get; private set; }
    public string? FirstName { get; private set; }
    public string? Notes { get; private set; }
    public string? Phone1 { get; private set; }
    public string? Phone2 { get; private set; }
    public string? Nip { get; private set; }
    public string? Street { get; private set; }
    public string? PostalCode { get; private set; }
    public string? City { get; private set; }
    public string? Country { get; private set; }
    public string? ShippingStreet { get; private set; }
    public string? ShippingPostalCode { get; private set; }
    public string? ShippingCity { get; private set; }
    public string? ShippingCountry { get; private set; }
    public string? Email1 { get; private set; }
    public string? Email2 { get; private set; }
    public string? Code { get; private set; }
    public string? Status { get; private set; }
    public string Currency { get; private set; }
    public int? CustomerType { get; private set; }
    public bool? OfferEnabled { get; private set; }
    public string? VatStatus { get; private set; }
    public string? Regon { get; private set; }
    public string? FullAddress { get; private set; }

    // Konstruktor prywatny dla EF Core (jeśli będziemy używać)
    private Customer()
    {
        Name = string.Empty;
        Currency = "PLN";
    }

    // Główny konstruktor
    public Customer(int companyId, string name, string currency = "PLN")
    {
        CompanyId = companyId;
        Name = name ?? string.Empty; // Pozwalamy na null w bazie, ale używamy pustego stringa w encji
        Currency = currency;
    }

    public void UpdateContactInfo(string? email1, string? email2, string? phone1, string? phone2)
    {
        Email1 = email1;
        Email2 = email2;
        Phone1 = phone1;
        Phone2 = phone2;
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

    public void UpdateShippingAddress(string? street, string? postalCode, string? city, string? country)
    {
        ShippingStreet = street;
        ShippingPostalCode = postalCode;
        ShippingCity = city;
        ShippingCountry = country;
        UpdateTimestamp();
    }

    public void UpdatePersonalInfo(string? firstName, string? surname)
    {
        FirstName = firstName;
        Surname = surname;
        UpdateTimestamp();
    }

    public void UpdateCompanyInfo(string? nip, string? regon, string? vatStatus)
    {
        Nip = nip;
        Regon = regon;
        VatStatus = vatStatus;
        UpdateTimestamp();
    }

    public void ChangeName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Nazwa odbiorcy nie może być pusta.", nameof(newName));

        Name = newName;
        UpdateTimestamp();
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdateTimestamp();
    }

    public void UpdateStatus(string? status)
    {
        Status = status;
        UpdateTimestamp();
    }

    public void UpdateCode(string? code)
    {
        Code = code;
        UpdateTimestamp();
    }

    public void SetOfferEnabled(bool enabled)
    {
        OfferEnabled = enabled;
        UpdateTimestamp();
    }

    public void UpdateCustomerType(int? customerType)
    {
        CustomerType = customerType;
        UpdateTimestamp();
    }

    public void UpdateCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Waluta nie może być pusta.", nameof(currency));

        Currency = currency;
        UpdateTimestamp();
    }

    public void UpdateFullAddress(string? fullAddress)
    {
        FullAddress = fullAddress;
        UpdateTimestamp();
    }
}
