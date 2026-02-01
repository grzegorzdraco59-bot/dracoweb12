namespace ERP.Application.DTOs;

/// <summary>
/// DTO dla pozycji faktury z tabeli pozycjefaktury (odczyt).
/// ZASADA: netto_poz, vat_poz, brutto_poz są TYLKO wyliczane – NIE umożliwiaj edycji w UI.
/// </summary>
public class InvoicePositionDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string? NazwaTowaru { get; set; }
    public string? NazwaTowaruEng { get; set; }
    public string? Jednostki { get; set; }
    public decimal Ilosc { get; set; }
    public decimal CenaNetto { get; set; }
    public decimal Rabat { get; set; }
    public string? StawkaVat { get; set; }

    /// <summary>Netto pozycji – WYLICZANE (algorytm ETAP 2). Tylko odczyt w UI.</summary>
    public decimal NettoPoz { get; set; }
    /// <summary>VAT pozycji – WYLICZANE. Tylko odczyt w UI.</summary>
    public decimal VatPoz { get; set; }
    /// <summary>Brutto pozycji – WYLICZANE. Tylko odczyt w UI.</summary>
    public decimal BruttoPoz { get; set; }
}
