namespace ERP.Application.DTOs;

/// <summary>
/// DTO wiersza pozycji zamówienia z widoku pozycjezamowienia_V.
/// Nazwy pól zgodne z kolumnami SQL dla AutoGenerateColumns.
/// </summary>
public class OrderPositionRow
{
    public int id_pozycji_zamowienia { get; set; }
    public int? company_id { get; set; }
    public int? id_zamowienia { get; set; }
    public int? id_towaru { get; set; }
    public int? data_dostawy_pozycji { get; set; }
    public string data_dostawy_pozycji_txt { get; set; } = "";
    public string? towar_nazwa_draco { get; set; }
    public string? towar { get; set; }
    public string? towar_nazwa_ENG { get; set; }
    public string? jednostki_zamawiane { get; set; }
    public decimal? ilosc_zamawiana { get; set; }
    public decimal? ilosc_dostarczona { get; set; }
    public decimal? cena_zamawiana { get; set; }
    public string? status_towaru { get; set; }
    public string? jednostki_zakupu { get; set; }
    public decimal? ilosc_zakupu { get; set; }
    public decimal? cena_zakupu { get; set; }
    public decimal? wartsc_zakupu { get; set; }
    public decimal? cena_zakupu_pln { get; set; }
    public decimal? przelicznik_m_kg { get; set; }
    public decimal? cena_zakupu_PLN_nowe_jednostki { get; set; }
    public string? uwagi { get; set; }
    public string? dostawca_pozycji { get; set; }
    public string? stawka_vat { get; set; }
    public decimal? ciezar_jednostkowy { get; set; }
    public decimal? ilosc_w_opakowaniu { get; set; }
    public int? id_zamowienia_hala { get; set; }
    public int? id_pozycji_pozycji_oferty { get; set; }
    public int? zaznacz_do_kopiowania { get; set; }
    public bool? skopiowano_do_magazynu { get; set; }
    public decimal? dlugosc { get; set; }
}
