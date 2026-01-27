namespace ERP.Application.DTOs;

/// <summary>
/// DTO dla zamówienia - wszystkie pola z tabeli zamowieniahala
/// </summary>
public class OrderDto
{
    public int Id { get; set; } // id_zamowienia_hala
    public int CompanyId { get; set; } // id_firmy
    public int? OrderNumberInt { get; set; } // id_zamowienia
    public int? OrderDateInt { get; set; } // data_zamowienia (format Clarion)
    public DateTime? OrderDate { get; set; } // konwersja z OrderDateInt
    public int? SupplierId { get; set; } // id_dostawcy
    public string? SupplierName { get; set; } // dostawca
    public string? SupplierEmail { get; set; } // dostawca_mail
    public string? SupplierCurrency { get; set; } // dostawca_waluta
    public int? ProductId { get; set; } // id_towaru
    public string? ProductNameDraco { get; set; } // nazwa_towaru_draco
    public string? ProductName { get; set; } // nazwa_towaru
    public string? ProductStatus { get; set; } // status_towaru
    public string? PurchaseUnit { get; set; } // jednostki_zakupu
    public string? SalesUnit { get; set; } // jednostki_sprzedazy
    public decimal? PurchasePrice { get; set; } // cena_zakupu
    public decimal? ConversionFactor { get; set; } // przelicznik_m_kg
    public decimal? Quantity { get; set; } // ilosc
    public string? Notes { get; set; } // uwagi
    public int? Status { get; set; } // zaznacz_do_zamowienia (1-wybrane, 2-edytowane, 3-oczekiwanie, 4-dostarczone)
    public bool? SentToOrder { get; set; } // wyslano_do_zamowienia
    public bool? Delivered { get; set; } // dostarczono
    public decimal? QuantityInPackage { get; set; } // ilosc_w_opakowaniu
    public string? VatRate { get; set; } // stawka_vat
    public string? Operator { get; set; } // operator
    public int? ScannerOrderNumber { get; set; } // nr_zam_skaner
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
