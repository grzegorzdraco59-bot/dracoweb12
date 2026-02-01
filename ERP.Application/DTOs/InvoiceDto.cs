namespace ERP.Application.DTOs;

/// <summary>
/// DTO dla nagłówka faktury z tabeli faktury (odczyt – lista/edycja).
/// ZASADA: sum_netto, sum_vat, sum_brutto są TYLKO wyliczane (RecalculateTotals) – NIE umożliwiaj edycji w UI.
/// </summary>
public class InvoiceDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int? IdOferty { get; set; }
    public int? DataFaktury { get; set; }
    public int? NrFaktury { get; set; }
    public string? NrFakturyText { get; set; }
    public string? SkrotNazwaFaktury { get; set; }
    public string? OdbiorcaNazwa { get; set; }
    public string? Waluta { get; set; }
    public decimal? KwotaNetto { get; set; }
    public decimal? TotalVat { get; set; }
    public decimal? KwotaBrutto { get; set; }
    /// <summary>Suma netto – WYLICZANA z pozycji (RecalculateTotals). Tylko odczyt w UI.</summary>
    public decimal? SumNetto { get; set; }
    /// <summary>Suma VAT – WYLICZANA. Tylko odczyt w UI.</summary>
    public decimal? SumVat { get; set; }
    /// <summary>Suma brutto – WYLICZANA. Tylko odczyt w UI.</summary>
    public decimal? SumBrutto { get; set; }
    public string? Operator { get; set; }

    /// <summary>Data do wyświetlenia (Clarion int → DateTime lub tekst).</summary>
    public string FormattedDataFaktury =>
        DataFaktury.HasValue && DataFaktury.Value > 0
            ? new DateTime(1800, 12, 28).AddDays(DataFaktury.Value).ToString("yyyy-MM-dd")
            : "";
}
