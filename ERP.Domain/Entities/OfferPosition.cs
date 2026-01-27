namespace ERP.Domain.Entities;

/// <summary>
/// Encja domenowa reprezentująca pozycję oferty
/// </summary>
public class OfferPosition : BaseEntity
{
    public int CompanyId { get; private set; }
    public int OfferId { get; private set; }
    public int? ProductId { get; private set; }
    public int? SupplierId { get; private set; }
    public string? ProductCode { get; private set; }
    public string? Name { get; private set; }
    public string? NameEng { get; private set; }
    public string Unit { get; private set; }
    public string? UnitEng { get; private set; }
    public decimal? Quantity { get; private set; }
    public decimal? Price { get; private set; }
    public decimal? Discount { get; private set; }
    public decimal? PriceAfterDiscount { get; private set; }
    public decimal? PriceAfterDiscountAndQuantity { get; private set; }
    public string? VatRate { get; private set; }
    public decimal? Vat { get; private set; }
    public decimal? PriceBrutto { get; private set; }
    public string? OfferNotes { get; private set; }
    public string? InvoiceNotes { get; private set; }
    public string? Other1 { get; private set; }
    public decimal? GroupNumber { get; private set; }

    private OfferPosition()
    {
        Unit = "szt";
    }

    public OfferPosition(int companyId, int offerId, string unit = "szt")
    {
        CompanyId = companyId;
        OfferId = offerId;
        Unit = unit ?? "szt";
    }

    public void UpdateProductInfo(int? productId, int? supplierId, string? productCode, string? name, string? nameEng)
    {
        ProductId = productId;
        SupplierId = supplierId;
        ProductCode = productCode;
        Name = name;
        NameEng = nameEng;
        UpdateTimestamp();
    }

    public void UpdateUnits(string unit, string? unitEng)
    {
        Unit = unit ?? "szt";
        UnitEng = unitEng;
        UpdateTimestamp();
    }

    public void UpdatePricing(decimal? quantity, decimal? price, decimal? discount, 
        decimal? priceAfterDiscount, decimal? priceAfterDiscountAndQuantity)
    {
        Quantity = quantity;
        Price = price;
        Discount = discount;
        PriceAfterDiscount = priceAfterDiscount;
        PriceAfterDiscountAndQuantity = priceAfterDiscountAndQuantity;
        UpdateTimestamp();
    }

    public void UpdateVatInfo(string? vatRate, decimal? vat, decimal? priceBrutto)
    {
        VatRate = vatRate;
        Vat = vat;
        PriceBrutto = priceBrutto;
        UpdateTimestamp();
    }

    public void UpdateNotes(string? offerNotes, string? invoiceNotes, string? other1)
    {
        OfferNotes = offerNotes;
        InvoiceNotes = invoiceNotes;
        Other1 = other1;
        UpdateTimestamp();
    }

    public void UpdateGroupNumber(decimal? groupNumber)
    {
        GroupNumber = groupNumber;
        UpdateTimestamp();
    }
}
