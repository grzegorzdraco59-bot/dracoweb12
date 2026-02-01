namespace ERP.Application.DTOs;

/// <summary>
/// Nagłówek dokumentu (faktury) powiązanego z ofertą – do drzewka. Kwoty z sum_brutto (nie SUM z pozycji).
/// </summary>
public class OfferDocumentDto
{
    public int InvoiceId { get; set; }
    public string DocType { get; set; } = string.Empty;
    public string? DocFullNo { get; set; }
    public int? DataFaktury { get; set; }
    public decimal? SumBrutto { get; set; }
    /// <summary>Do zapłaty (FV: sum_brutto - sum_zaliczek_brutto, z tabeli faktury).</summary>
    public decimal? DoZaplatyBrutto { get; set; }

    /// <summary>Data do wyświetlenia (Clarion int → yyyy-MM-dd).</summary>
    public string FormattedDataFaktury =>
        DataFaktury.HasValue && DataFaktury.Value > 0
            ? new DateTime(1800, 12, 28).AddDays(DataFaktury.Value).ToString("yyyy-MM-dd")
            : "";
}
