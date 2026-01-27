using System.Reflection;

namespace ERP.Domain.Entities;

/// <summary>
/// Fabryka do tworzenia Customer z danych z bazy danych
/// </summary>
public static class CustomerFactory
{
    public static Customer FromDatabase(
        int id,
        int companyId,
        string name,
        string? surname,
        string? firstName,
        string? notes,
        string? phone1,
        string? phone2,
        string? nip,
        string? street,
        string? postalCode,
        string? city,
        string? country,
        string? shippingStreet,
        string? shippingPostalCode,
        string? shippingCity,
        string? shippingCountry,
        string? email1,
        string? email2,
        string? code,
        string? status,
        string currency,
        int? customerType,
        bool? offerEnabled,
        string? vatStatus,
        string? regon,
        string? fullAddress)
    {
        var customer = new Customer(companyId, name, currency);
        
        // Używamy refleksji do ustawienia Id i innych właściwości
        SetProperty(customer, "Id", id);
        SetProperty(customer, "Surname", surname);
        SetProperty(customer, "FirstName", firstName);
        SetProperty(customer, "Notes", notes);
        SetProperty(customer, "Phone1", phone1);
        SetProperty(customer, "Phone2", phone2);
        SetProperty(customer, "Nip", nip);
        SetProperty(customer, "Street", street);
        SetProperty(customer, "PostalCode", postalCode);
        SetProperty(customer, "City", city);
        SetProperty(customer, "Country", country);
        SetProperty(customer, "ShippingStreet", shippingStreet);
        SetProperty(customer, "ShippingPostalCode", shippingPostalCode);
        SetProperty(customer, "ShippingCity", shippingCity);
        SetProperty(customer, "ShippingCountry", shippingCountry);
        SetProperty(customer, "Email1", email1);
        SetProperty(customer, "Email2", email2);
        SetProperty(customer, "Code", code);
        SetProperty(customer, "Status", status);
        SetProperty(customer, "CustomerType", customerType);
        SetProperty(customer, "OfferEnabled", offerEnabled);
        SetProperty(customer, "VatStatus", vatStatus);
        SetProperty(customer, "Regon", regon);
        SetProperty(customer, "FullAddress", fullAddress);
        
        // Ustawiamy CreatedAt i UpdatedAt na domyślne wartości, ponieważ nie ma ich w bazie danych
        SetProperty(customer, "CreatedAt", DateTime.Now);
        SetProperty(customer, "UpdatedAt", null);
        
        return customer;
    }

    private static void SetProperty(object obj, string propertyName, object? value)
    {
        var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property != null && property.CanWrite)
        {
            property.SetValue(obj, value);
        }
    }
}
