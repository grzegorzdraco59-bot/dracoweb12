using ERP.Application.Helpers;

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
    public int? OfferDate { get; set; } // Clarion date (liczba dni od 28.12.1800) – data_oferty
    public string? FormattedOfferDate { get; set; } // Formatowana data dd/MM/yyyy
    /// <summary>Data oferty (mapowanie z data_oferty). Do wyświetlania w browse.</summary>
    public DateTime? DataOferty { get; set; }
    /// <summary>Numer oferty (mapowanie z nr_oferty). Do wyświetlania w browse.</summary>
    public int? NrOferty { get; set; }
    /// <summary>Data oferty w formacie yyyy-MM-dd (do listy ofert / drzewka).</summary>
    public string FormattedOfferDateYyyyMmDd =>
        DataOferty.HasValue ? DataOferty.Value.ToString("yyyy-MM-dd") : (OfferDate.HasValue && OfferDate.Value > 0 ? new DateTime(1800, 12, 28).AddDays(OfferDate.Value).ToString("yyyy-MM-dd") : "");
    public int? OfferNumber { get; set; }
    /// <summary>Pełny numer prezentacyjny OF/yyyy/MM/dd-NrOferty – budowany deterministycznie z data_oferty + nr_oferty (helper).</summary>
    public string FullNo => GetOfferNoFromDateAndNumber();

    private string GetOfferNoFromDateAndNumber()
    {
        var dt = DataOferty ?? (OfferDate.HasValue && OfferDate.Value > 0 ? new DateTime(1800, 12, 28).AddDays(OfferDate.Value) : (DateTime?)null);
        if (dt.HasValue && NrOferty.HasValue)
            return OfferNumberHelper.BuildOfferNo(dt.Value, NrOferty.Value);
        return NrOferty?.ToString() ?? "";
    }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerStreet { get; set; }
    public string? CustomerPostalCode { get; set; }
    public string? CustomerCity { get; set; }
    public string? CustomerCountry { get; set; }
    /// <summary>Adres łączony (ulica, kod, miasto) do wyświetlania w browse.</summary>
    public string? Adres => string.Join(", ", new[] { CustomerStreet, CustomerPostalCode, CustomerCity }.Where(s => !string.IsNullOrWhiteSpace(s)));
    public string? CustomerNip { get; set; }
    public string? CustomerEmail { get; set; }
    public string? RecipientName { get; set; }
    public string? Currency { get; set; }
    public decimal? TotalPrice { get; set; }
    public decimal? VatRate { get; set; }
    public decimal? TotalVat { get; set; }
    public decimal? TotalBrutto { get; set; }
    /// <summary>Suma brutto z pozycji (nagłówek). Preferowane w drzewku dokumentów.</summary>
    public decimal? SumBrutto { get; set; }
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
