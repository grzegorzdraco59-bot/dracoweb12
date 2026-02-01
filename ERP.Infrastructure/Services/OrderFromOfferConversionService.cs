using ERP.Application.Repositories;
using ERP.Application.Services;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using ERP.Domain.Exceptions;
using ERP.Domain.Repositories;
using MySqlConnector;

namespace ERP.Infrastructure.Services;

/// <summary>
/// Atomowa konwersja oferty (Accepted) na zamówienie – nagłówek + pozycje w jednej transakcji.
/// Zgodne z docs/FAZA4_STANY_DOKUMENTOW.md. Oferta musi być w statusie Accepted; nowe zamówienie ma Status = Draft.
/// Idempotencja: jeśli oferta była już konwertowana (istnieje zamówienie powiązane przez pozycje), zwracane jest ID istniejącego zamówienia.
/// </summary>
public class OrderFromOfferConversionService : IOrderFromOfferConversionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOfferRepository _offerRepository;
    private readonly IOfferPositionRepository _offerPositionRepository;
    private readonly IOrderPositionMainRepository _orderPositionRepository;

    public OrderFromOfferConversionService(
        IUnitOfWork unitOfWork,
        IOfferRepository offerRepository,
        IOfferPositionRepository offerPositionRepository,
        IOrderPositionMainRepository orderPositionRepository)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _offerRepository = offerRepository ?? throw new ArgumentNullException(nameof(offerRepository));
        _offerPositionRepository = offerPositionRepository ?? throw new ArgumentNullException(nameof(offerPositionRepository));
        _orderPositionRepository = orderPositionRepository ?? throw new ArgumentNullException(nameof(orderPositionRepository));
    }

    public async Task<int> CreateFromOfferAsync(int offerId, int companyId, CancellationToken cancellationToken = default)
    {
        var offer = await _offerRepository.GetByIdAsync(offerId, companyId, cancellationToken).ConfigureAwait(false);
        if (offer == null)
            throw new InvalidOperationException($"Oferta o ID {offerId} nie została znaleziona.");
        if (offer.Status != OfferStatus.Accepted)
            throw new BusinessRuleException(
                "Konwersja oferty do zamówienia jest dozwolona tylko dla oferty w statusie Zaakceptowana (Accepted). " +
                $"Aktualny status oferty: {offer.Status}.");

        var existingOrderId = await _orderPositionRepository.GetOrderIdLinkedToOfferAsync(offerId, companyId, cancellationToken).ConfigureAwait(false);
        if (existingOrderId.HasValue)
            return existingOrderId.Value;

        var positions = (await _offerPositionRepository.GetByOfferIdAsync(offerId, cancellationToken).ConfigureAwait(false)).ToList();

        return await _unitOfWork.ExecuteInTransactionAsync(async transaction =>
        {
            var conn = transaction.Connection ?? throw new InvalidOperationException("Brak połączenia w transakcji.");
            var newOrderId = await InsertOrderAsync(conn, transaction, offer, cancellationToken).ConfigureAwait(false);
            foreach (var pos in positions)
                await InsertOrderPositionAsync(conn, transaction, newOrderId, companyId, pos, cancellationToken).ConfigureAwait(false);
            return newOrderId;
        }, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> InsertOrderAsync(MySqlConnection conn, MySqlTransaction transaction, Offer offer, CancellationToken cancellationToken)
    {
        int? orderDateInt = offer.OfferDate; // Clarion format (już int w encji)
        var cmd = new MySqlCommand(
            "INSERT INTO zamowienia (id_firmy, nr_zamowienia, data_zamowienia, id_dostawcy, dostawca, uwagi, status) " +
            "VALUES (@CompanyId, @OrderNumber, @OrderDateInt, @SupplierId, @SupplierName, @Notes, @Status); " +
            "SELECT LAST_INSERT_ID();",
            conn, transaction);
        cmd.Parameters.AddWithValue("@CompanyId", offer.CompanyId);
        cmd.Parameters.AddWithValue("@OrderNumber", offer.OfferNumber ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@OrderDateInt", orderDateInt ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@SupplierId", offer.CustomerId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@SupplierName", offer.CustomerName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Notes", offer.OfferNotes ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", OrderStatusMapping.ToDb(OrderStatus.Draft));

        var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(result);
    }

    private static async Task InsertOrderPositionAsync(MySqlConnection conn, MySqlTransaction transaction, int orderId, int companyId, OfferPosition pos, CancellationToken cancellationToken)
    {
        var cmd = new MySqlCommand(
            "INSERT INTO pozycjezamowienia (id_firmy, id_zamowienia, id_towaru, data_dostawy_pozycji, " +
            "towar_nazwa_draco, towar, towar_nazwa_ENG, jednostki_zamawiane, ilosc_zamawiana, ilosc_dostarczona, " +
            "cena_zamawiana, status_towaru, jednostki_zakupu, ilosc_zakupu, cena_zakupu, wartsc_zakupu, " +
            "cena_zakupu_pln, przelicznik_m_kg, cena_zakupu_PLN_nowe_jednostki, uwagi, dostawca_pozycji, " +
            "stawka_vat, ciezar_jednostkowy, ilosc_w_opakowaniu, id_zamowienia_hala, id_pozycji_pozycji_oferty, " +
            "zaznacz_do_kopiowania, skopiowano_do_magazynu, dlugosc) " +
            "VALUES (@CompanyId, @OrderId, @ProductId, @DeliveryDateInt, @ProductNameDraco, @Product, " +
            "@ProductNameEng, @OrderUnit, @OrderQuantity, @DeliveredQuantity, @OrderPrice, @ProductStatus, " +
            "@PurchaseUnit, @PurchaseQuantity, @PurchasePrice, @PurchaseValue, @PurchasePricePln, " +
            "@ConversionFactor, @PurchasePricePlnNewUnit, @Notes, @Supplier, @VatRate, @UnitWeight, " +
            "@QuantityInPackage, @OrderHalaId, @OfferPositionId, @MarkForCopying, @CopiedToWarehouse, @Length);",
            conn, transaction);

        cmd.Parameters.AddWithValue("@CompanyId", companyId);
        cmd.Parameters.AddWithValue("@OrderId", orderId);
        cmd.Parameters.AddWithValue("@ProductId", pos.ProductId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@DeliveryDateInt", (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ProductNameDraco", pos.Name ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Product", pos.ProductCode ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ProductNameEng", pos.NameEng ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@OrderUnit", pos.Unit ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@OrderQuantity", pos.Ilosc ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@DeliveredQuantity", (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@OrderPrice", (pos.PriceAfterDiscount ?? pos.CenaNetto) ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ProductStatus", (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@PurchaseUnit", (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@PurchaseQuantity", (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@PurchasePrice", (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@PurchaseValue", (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@PurchasePricePln", (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ConversionFactor", (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@PurchasePricePlnNewUnit", (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Notes", pos.OfferNotes ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Supplier", (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@VatRate", pos.VatRate ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@UnitWeight", (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@QuantityInPackage", (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@OrderHalaId", (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@OfferPositionId", pos.Id);
        cmd.Parameters.AddWithValue("@MarkForCopying", (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@CopiedToWarehouse", (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Length", (object)DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
