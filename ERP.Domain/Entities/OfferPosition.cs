namespace ERP.Domain.Entities;

/// <summary>
/// Encja domenowa reprezentująca pozycję oferty (tabela apozycjeoferty, PK: id_pozycja_oferty)
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
    public decimal? Ilosc { get; private set; }
    public decimal? CenaNetto { get; private set; }
    public decimal? Discount { get; private set; }
    public decimal? PriceAfterDiscount { get; private set; }
    public decimal? NettoPoz { get; private set; }
    public string? VatRate { get; private set; }
    public decimal? VatPoz { get; private set; }
    public decimal? BruttoPoz { get; private set; }
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

    public void UpdatePricing(decimal? ilosc, decimal? cenaNetto, decimal? discount, 
        decimal? priceAfterDiscount, decimal? nettoPoz)
    {
        Ilosc = ilosc;
        CenaNetto = cenaNetto;
        Discount = discount;
        PriceAfterDiscount = priceAfterDiscount;
        NettoPoz = nettoPoz;
        UpdateTimestamp();
    }

    public void UpdateVatInfo(string? vatRate, decimal? vatPoz, decimal? bruttoPoz)
    {
        VatRate = vatRate;
        VatPoz = vatPoz;
        BruttoPoz = bruttoPoz;
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
