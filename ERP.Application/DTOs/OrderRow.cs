namespace ERP.Application.DTOs;

/// <summary>
/// DTO wiersza zamówienia z widoku zamowienia_V.
/// Nazwy pól zgodne z kolumnami SQL dla bezpośredniego bindingu w XAML.
/// </summary>
public class OrderRow
{
    public int id { get; set; }
    public DateTime? data_zamowienia { get; set; }
    public string data_zamowienia_txt { get; set; } = "";
    public int? nr_zamowienia { get; set; }
    public string? dostawca { get; set; }
    public string? waluta { get; set; }
    public DateTime? data_dostawy { get; set; }
    public string data_dostawy_txt { get; set; } = "";
    public string? status_zamowienia { get; set; }
    public string? status_platnosci { get; set; }
    public DateTime? data_platnosci { get; set; }
    public string data_platnosci_txt { get; set; } = "";
    public string? nr_faktury { get; set; }
    public DateTime? data_faktury { get; set; }
    public string data_faktury_txt { get; set; } = "";
    public decimal? wartosc { get; set; }
    public string? uwagi { get; set; }
    public string? dla_kogo { get; set; }
    public string? tabela_nbp { get; set; }
    public decimal? kurs_waluty { get; set; }
}
