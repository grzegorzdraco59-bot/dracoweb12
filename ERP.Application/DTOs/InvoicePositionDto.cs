namespace ERP.Application.DTOs;

/// <summary>
/// DTO dla pozycji faktury z tabeli pozycjefaktury (odczyt).
/// IdPozycjiFaktury = pozycjefaktury.id_pozycji_faktury (PK). FakturaId = pozycjefaktury.faktura_id (FK do faktury.id).
/// ZASADA: netto_poz, vat_poz, brutto_poz są TYLKO wyliczane – NIE umożliwiaj edycji w UI.
/// </summary>
public class InvoicePositionDto
{
    /// <summary>PK pozycji – mapuje do pozycjefaktury.id_pozycji_faktury. Używane w UPDATE (WHERE id_pozycji_faktury = @IdPozycjiFaktury).</summary>
    public int IdPozycjiFaktury { get; set; }
    /// <summary>FK do nagłówka – mapuje do pozycjefaktury.faktura_id (faktury.id).</summary>
    public long FakturaId { get; set; }
    /// <summary>Alias dla IdPozycjiFaktury (bindingi / wyświetlanie).</summary>
    public int Id { get => IdPozycjiFaktury; set => IdPozycjiFaktury = value; }
    /// <summary>Alias dla FakturaId (kompatybilność).</summary>
    public long InvoiceId { get => FakturaId; set => FakturaId = value; }
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
