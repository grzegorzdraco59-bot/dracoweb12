using ERP.Application.Services;
using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Services;

/// <summary>
/// Konwersja aoferty (aoferty + aofertypozycje) na zlecenie produkcyjne (zlecenia + pozycjezlecenia).
/// Wzorzec jak OfferToFpfConversionService: transakcja, insert nagłówka → id → insert pozycji → update flagi aoferty.
/// </summary>
public class OfferToZlecenieConversionService : IOfferToZlecenieConversionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IOfferRepository _offerRepository;
    private readonly IOfferPositionRepository _offerPositionRepository;

    public OfferToZlecenieConversionService(
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IOfferRepository offerRepository,
        IOfferPositionRepository offerPositionRepository)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        _offerRepository = offerRepository ?? throw new ArgumentNullException(nameof(offerRepository));
        _offerPositionRepository = offerPositionRepository ?? throw new ArgumentNullException(nameof(offerPositionRepository));
    }

    public async Task<int> CopyOfferToZlecenieAsync(int offerId, int companyId, CancellationToken cancellationToken = default)
    {
        if (offerId <= 0)
            throw new InvalidOperationException("Brak ID aoferty. Oferta musi być zapisana w bazie przed kopiowaniem do zlecenia.");

        var offer = await _offerRepository.GetByIdAsync(offerId, companyId, cancellationToken).ConfigureAwait(false);
        if (offer == null)
            throw new InvalidOperationException($"Oferta o ID {offerId} nie została znaleziona.");

        var positions = (await _offerPositionRepository.GetByOfferIdAsync(offerId, cancellationToken).ConfigureAwait(false)).ToList();
        if (positions.Count == 0)
            throw new InvalidOperationException("Oferta nie ma pozycji. Dodaj co najmniej jedną pozycję przed kopiowaniem do zlecenia.");

        return await _unitOfWork.ExecuteInTransactionAsync(async transaction =>
        {
            var conn = (transaction.Connection as MySqlConnection) ?? throw new InvalidOperationException("Brak połączenia MySQL w transakcji.");
            var mysqlTransaction = (MySqlTransaction)transaction;
            var zlecenieId = (int)await _idGenerator.GetNextIdAsync("zlecenia", conn, mysqlTransaction, cancellationToken).ConfigureAwait(false);
            await InsertZlecenieAsync(conn, mysqlTransaction, zlecenieId, offerId, companyId, offer, cancellationToken).ConfigureAwait(false);
            foreach (var pos in positions)
                await InsertPozycjaZleceniaAsync(conn, mysqlTransaction, zlecenieId, offerId, companyId, pos, cancellationToken).ConfigureAwait(false);
            await UpdateOfferDoZleceniaAsync(conn, mysqlTransaction, offerId, companyId, cancellationToken).ConfigureAwait(false);
            return zlecenieId;
        }, cancellationToken).ConfigureAwait(false);
    }

    private static async Task InsertZlecenieAsync(MySqlConnection conn, MySqlTransaction transaction, int zlecenieId, int offerId, int companyId, Offer offer, CancellationToken cancellationToken)
    {
        var empty = "";
        var dataZlec = offer.OfferDate ?? (int)(DateTime.Today - new DateTime(1800, 12, 28)).TotalDays;

        var sql = "INSERT INTO zlecenia (id_zlecenia, id_firmy, id_aoferta, Data_zlec, Nr_zlec, Data_wykonania, " +
            "odbiorca_ID_odbiorcy, odbiorca_nazwa, odbiorca_ulica, odbiorca_kod_poczt, odbiorca_miasto, odbiorca_panstwo, odbiorca_nip, odbiorca_mail, " +
            "Waluta, uwagi_do_zlecenia, operator, Data_oferty, Nr_oferty, xCena_calkowita, xstawka_vat, xtotal_vat, xtotal_brutto) " +
            "VALUES (@IdZlecenia, @IdFirmy, @IdAoferta, @DataZlec, @NrZlec, @DataWykonania, " +
            "@OdbiorcaId, @OdbiorcaNazwa, @OdbiorcaUlica, @OdbiorcaKodPoczt, @OdbiorcaMiasto, @OdbiorcaPanstwo, @OdbiorcaNip, @OdbiorcaMail, " +
            "@Waluta, @Uwagi, @Operator, @DataOferty, @NrOferty, @xCenaCalkowita, @xStawkaVat, @xTotalVat, @xTotalBrutto);";
        var cmd = new MySqlCommand(sql, conn, transaction);

        cmd.Parameters.AddWithValue("@IdZlecenia", zlecenieId);
        cmd.Parameters.AddWithValue("@IdFirmy", companyId);
        cmd.Parameters.AddWithValue("@IdAoferta", offerId);
        cmd.Parameters.AddWithValue("@DataZlec", dataZlec);
        cmd.Parameters.AddWithValue("@NrZlec", offer.OfferNumber ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@DataWykonania", (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@OdbiorcaId", offer.CustomerId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@OdbiorcaNazwa", offer.CustomerName ?? empty);
        cmd.Parameters.AddWithValue("@OdbiorcaUlica", offer.CustomerStreet ?? empty);
        cmd.Parameters.AddWithValue("@OdbiorcaKodPoczt", offer.CustomerPostalCode ?? empty);
        cmd.Parameters.AddWithValue("@OdbiorcaMiasto", offer.CustomerCity ?? empty);
        cmd.Parameters.AddWithValue("@OdbiorcaPanstwo", offer.CustomerCountry ?? empty);
        cmd.Parameters.AddWithValue("@OdbiorcaNip", offer.CustomerNip ?? empty);
        cmd.Parameters.AddWithValue("@OdbiorcaMail", offer.CustomerEmail ?? empty);
        cmd.Parameters.AddWithValue("@Waluta", offer.Currency ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Uwagi", offer.OfferNotes ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Operator", offer.Operator ?? empty);
        cmd.Parameters.AddWithValue("@DataOferty", offer.OfferDate ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@NrOferty", offer.OfferNumber ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@xCenaCalkowita", offer.TotalPrice ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@xStawkaVat", offer.VatRate ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@xTotalVat", offer.TotalVat ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@xTotalBrutto", offer.TotalBrutto ?? offer.SumBrutto ?? (object)DBNull.Value);

        await cmd.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task InsertPozycjaZleceniaAsync(MySqlConnection conn, MySqlTransaction transaction, int zlecenieId, int offerId, int companyId, OfferPosition pos, CancellationToken cancellationToken)
    {
        var posId = (int)await _idGenerator.GetNextIdAsync("pozycjezlecenia", conn, transaction, cancellationToken).ConfigureAwait(false);

        var cmd = new MySqlCommand(
            "INSERT INTO pozycjezlecenia (ID_pozycji_zlecenia, id_firmy, ID_zlecenia, id_towaru, Nazwa, Nazwa_ENG, jednostki, Sztuki, Cena, Rabat, stawka_vat, " +
            "id_pozycji_oferty, id_oferty, Uwagi_oferta, nr_skrzyni) " +
            "VALUES (@IdPozycji, @IdFirmy, @IdZlecenia, @IdTowaru, @Nazwa, @NazwaEng, @Jednostki, @Sztuki, @Cena, @Rabat, @StawkaVat, " +
            "@IdPozycjiOferty, @IdOferty, @UwagiOferta, @NrSkrzyni);",
            conn, transaction);

        cmd.Parameters.AddWithValue("@IdPozycji", posId);
        cmd.Parameters.AddWithValue("@IdFirmy", companyId);
        cmd.Parameters.AddWithValue("@IdZlecenia", zlecenieId);
        cmd.Parameters.AddWithValue("@IdTowaru", pos.ProductId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Nazwa", pos.Name ?? "");
        cmd.Parameters.AddWithValue("@NazwaEng", pos.NameEng ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Jednostki", pos.Unit ?? "szt");
        cmd.Parameters.AddWithValue("@Sztuki", pos.Ilosc ?? 0m);
        cmd.Parameters.AddWithValue("@Cena", pos.CenaNetto ?? 0m);
        cmd.Parameters.AddWithValue("@Rabat", pos.Discount ?? 0m);
        cmd.Parameters.AddWithValue("@StawkaVat", pos.VatRate ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@IdPozycjiOferty", pos.Id);
        cmd.Parameters.AddWithValue("@IdOferty", offerId);
        cmd.Parameters.AddWithValue("@UwagiOferta", pos.OfferNotes ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@NrSkrzyni", 0);

        await cmd.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task UpdateOfferDoZleceniaAsync(MySqlConnection conn, MySqlTransaction transaction, int offerId, int companyId, CancellationToken cancellationToken)
    {
        var cmd = new MySqlCommand(
            "UPDATE aoferty SET do_zlecenia = 1 WHERE id_oferta = @OfferId AND id_firmy = @CompanyId",
            conn, transaction);
        cmd.Parameters.AddWithValue("@OfferId", offerId);
        cmd.Parameters.AddWithValue("@CompanyId", companyId);
        await cmd.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
    }
}
