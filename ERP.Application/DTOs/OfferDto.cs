namespace ERP.Application.DTOs;

/// <summary>
/// DTO (Data Transfer Object) dla Offer - używane w warstwie aplikacji i UI
/// </summary>
public class OfferDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public bool? ForProforma { get; set; }
    public bool? ForOrder { get; set; }
    public int? OfferDate { get; set; } // Clarion date (liczba dni od 28.12.1800)
    public string? FormattedOfferDate { get; set; } // Formatowana data dd/MM/yyyy
    public int? OfferNumber { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerStreet { get; set; }
    public string? CustomerPostalCode { get; set; }
    public string? CustomerCity { get; set; }
    public string? CustomerCountry { get; set; }
    public string? CustomerNip { get; set; }
    public string? CustomerEmail { get; set; }
    public string? RecipientName { get; set; }
    public string? Currency { get; set; }
    public decimal? TotalPrice { get; set; }
    public decimal? VatRate { get; set; }
    public decimal? TotalVat { get; set; }
    public decimal? TotalBrutto { get; set; }
    public string? OfferNotes { get; set; }
    public string? AdditionalData { get; set; }
    public string Operator { get; set; } = string.Empty;
    public string TradeNotes { get; set; } = string.Empty;
    public bool ForInvoice { get; set; }
    public string History { get; set; } = string.Empty;
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
