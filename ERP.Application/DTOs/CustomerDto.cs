namespace ERP.Application.DTOs;

/// <summary>
/// DTO (Data Transfer Object) dla Customer - używane w warstwie aplikacji i UI
/// </summary>
public class CustomerDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Surname { get; set; }
    public string? FirstName { get; set; }
    public string? Notes { get; set; }
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public string? Nip { get; set; }
    public string? Street { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? ShippingStreet { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingCountry { get; set; }
    public string? Email1 { get; set; }
    public string? Email2 { get; set; }
    public string? Code { get; set; }
    public string? Status { get; set; }
    public string Currency { get; set; } = "PLN";
    public int? CustomerType { get; set; }
    public bool? OfferEnabled { get; set; }
    public string? VatStatus { get; set; }
    public string? Regon { get; set; }
    public string? FullAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
