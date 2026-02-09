using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

/// <summary>
/// Encja domenowa reprezentująca ofertę (tabela aoferty, PK: id_oferta)
/// </summary>
public class Offer : BaseEntity
{
    public int CompanyId { get; private set; }
    public OfferStatus Status { get; private set; } = OfferStatus.Draft;
    public bool? ForProforma { get; private set; }
    public bool? ForOrder { get; private set; }
    public int? OfferDate { get; private set; } // Unix timestamp
    public int? OfferNumber { get; private set; }
    public int? CustomerId { get; private set; }
    public string? CustomerName { get; private set; }
    public string? CustomerStreet { get; private set; }
    public string? CustomerPostalCode { get; private set; }
    public string? CustomerCity { get; private set; }
    public string? CustomerCountry { get; private set; }
    public string? CustomerNip { get; private set; }
    public string? CustomerEmail { get; private set; }
    public string? Currency { get; private set; }
    public decimal? TotalPrice { get; private set; }
    public decimal? VatRate { get; private set; }
    public decimal? TotalVat { get; private set; }
    public decimal? TotalBrutto { get; private set; }
    /// <summary>Suma brutto z pozycji (SUM(brutto_poz)). Tylko odczyt w UI.</summary>
    public decimal? SumBrutto { get; private set; }
    public string? OfferNotes { get; private set; }
    public string? AdditionalData { get; private set; }
    public string Operator { get; private set; }
    public string TradeNotes { get; private set; }
    public bool ForInvoice { get; private set; }
    public string History { get; private set; }

    private Offer()
    {
        Operator = string.Empty;
        TradeNotes = string.Empty;
        History = string.Empty;
    }

    public Offer(int companyId, string @operator)
    {
        CompanyId = companyId;
        Operator = @operator ?? string.Empty;
        TradeNotes = string.Empty;
        History = string.Empty;
        ForInvoice = false;
    }

    public void UpdateOfferInfo(int? offerDate, int? offerNumber, string? currency)
    {
        OfferDate = offerDate;
        OfferNumber = offerNumber;
        Currency = currency;
        UpdateTimestamp();
    }

    public void UpdateCustomerInfo(int? customerId, string? customerName, string? customerStreet,
        string? customerPostalCode, string? customerCity, string? customerCountry, string? customerNip, string? customerEmail)
    {
        CustomerId = customerId;
        CustomerName = customerName;
        CustomerStreet = customerStreet;
        CustomerPostalCode = customerPostalCode;
        CustomerCity = customerCity;
        CustomerCountry = customerCountry;
        CustomerNip = customerNip;
        CustomerEmail = customerEmail;
        UpdateTimestamp();
    }

    public void UpdatePricing(decimal? totalPrice, decimal? vatRate, decimal? totalVat, decimal? totalBrutto)
    {
        TotalPrice = totalPrice;
        VatRate = vatRate;
        TotalVat = totalVat;
        TotalBrutto = totalBrutto;
        UpdateTimestamp();
    }

    public void UpdateSumBrutto(decimal? sumBrutto)
    {
        SumBrutto = sumBrutto;
        UpdateTimestamp();
    }

    public void UpdateFlags(bool? forProforma, bool? forOrder, bool forInvoice)
    {
        ForProforma = forProforma;
        ForOrder = forOrder;
        ForInvoice = forInvoice;
        UpdateTimestamp();
    }

    public void UpdateNotes(string? offerNotes, string? additionalData, string? tradeNotes)
    {
        OfferNotes = offerNotes;
        AdditionalData = additionalData;
        TradeNotes = tradeNotes ?? string.Empty;
        UpdateTimestamp();
    }

    public void UpdateHistory(string history)
    {
        History = history ?? string.Empty;
        UpdateTimestamp();
    }

    public void UpdateStatus(OfferStatus status)
    {
        Status = status;
        UpdateTimestamp();
    }
}
