namespace ERP.Domain.Entities;

/// <summary>
/// Encja domenowa reprezentująca zamówienie z tabeli zamowieniahala
/// </summary>
public class Order : BaseEntity
{
    public int CompanyId { get; set; }
    public int? OrderNumberInt { get; set; } // id_zamowienia
    public int? OrderDateInt { get; set; } // data_zamowienia (format Clarion)
    public DateTime? OrderDate { get; set; } // konwersja z OrderDateInt
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierEmail { get; set; } // dostawca_mail
    public string? SupplierCurrency { get; set; } // dostawca_waluta
    public int? ProductId { get; set; } // id_towaru
    public string? ProductNameDraco { get; set; }
    public string? ProductName { get; set; } // nazwa_towaru
    public string? ProductStatus { get; set; } // status_towaru
    public string? PurchaseUnit { get; set; } // jednostki_zakupu
    public string? SalesUnit { get; set; }
    public decimal? PurchasePrice { get; set; } // cena_zakupu
    public decimal? ConversionFactor { get; set; } // przelicznik_m_kg
    public decimal? Quantity { get; set; }
    public string? Notes { get; set; }
    public int? Status { get; set; } // zaznacz_do_zamowienia (1-wybrane, 2-edytowane, 3-oczekiwanie, 4-dostarczone)
    public bool? SentToOrder { get; set; } // wyslano_do_zamowienia
    public bool? Delivered { get; set; } // dostarczono
    public decimal? QuantityInPackage { get; set; } // ilosc_w_opakowaniu
    public string? VatRate { get; set; } // stawka_vat
    public string? Operator { get; set; } // operator
    public int? ScannerOrderNumber { get; set; } // nr_zam_skaner

    // Konstruktor prywatny dla EF Core
    private Order()
    {
    }

    // Główny konstruktor
    public Order(int companyId)
    {
        CompanyId = companyId;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(int? status)
    {
        Status = status;
        UpdateTimestamp();
    }

    public void UpdateOrderInfo(DateTime? orderDate, string? orderNumber, string? notes, decimal? totalAmount)
    {
        OrderDate = orderDate;
        OrderNumberInt = int.TryParse(orderNumber, out var num) ? num : null;
        Notes = notes;
        UpdateTimestamp();
    }

    public void UpdateSupplier(int? supplierId, string? supplierName, string? supplierEmail = null, string? supplierCurrency = null)
    {
        SupplierId = supplierId;
        SupplierName = supplierName;
        SupplierEmail = supplierEmail;
        SupplierCurrency = supplierCurrency;
        UpdateTimestamp();
    }

    public void UpdateProductInfo(int? productId, string? productNameDraco, string? productName, string? productStatus)
    {
        ProductId = productId;
        ProductNameDraco = productNameDraco;
        ProductName = productName;
        ProductStatus = productStatus;
        UpdateTimestamp();
    }

    public void UpdatePricing(decimal? purchasePrice, decimal? quantity, decimal? conversionFactor, decimal? quantityInPackage, string? purchaseUnit, string? salesUnit)
    {
        PurchasePrice = purchasePrice;
        Quantity = quantity;
        ConversionFactor = conversionFactor;
        QuantityInPackage = quantityInPackage;
        PurchaseUnit = purchaseUnit;
        SalesUnit = salesUnit;
        UpdateTimestamp();
    }

    public void UpdateDeliveryInfo(bool? sentToOrder, bool? delivered)
    {
        SentToOrder = sentToOrder;
        Delivered = delivered;
        UpdateTimestamp();
    }

    public void UpdateVatAndOperator(string? vatRate, string? operatorName, int? scannerOrderNumber)
    {
        VatRate = vatRate;
        Operator = operatorName;
        ScannerOrderNumber = scannerOrderNumber;
        UpdateTimestamp();
    }
}
