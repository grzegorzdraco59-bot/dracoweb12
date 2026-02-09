using ERP.Application.Services;
using System.IO;
using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Services;

/// <summary>
/// Konwersja aoferty (aoferty + apozycjeoferty) na proformę FPF (faktury + pozycjefaktury).
/// Idempotencja: SELECT Id_faktury FROM faktury WHERE id_oferty=@offerId AND doc_type='FPF' LIMIT 1.
/// W pozycjefaktury FK do nagłówka: id_faktury.
/// </summary>
public class OfferToFpfConversionService : IOfferToFpfConversionService
{
    private const string FpfSkrot = "FPF";
    private const string FvzSkrot = "FVZ";
    private const string FvSkrot = "FV";

    private readonly DatabaseContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IDocumentNumberService _documentNumberService;
    private readonly IInvoiceTotalsService _invoiceTotalsService;
    private readonly IOfferRepository _offerRepository;
    private readonly IOfferPositionRepository _offerPositionRepository;

    public OfferToFpfConversionService(
        DatabaseContext context,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IDocumentNumberService documentNumberService,
        IInvoiceTotalsService invoiceTotalsService,
        IOfferRepository offerRepository,
        IOfferPositionRepository offerPositionRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        _documentNumberService = documentNumberService ?? throw new ArgumentNullException(nameof(documentNumberService));
        _invoiceTotalsService = invoiceTotalsService ?? throw new ArgumentNullException(nameof(invoiceTotalsService));
        _offerRepository = offerRepository ?? throw new ArgumentNullException(nameof(offerRepository));
        _offerPositionRepository = offerPositionRepository ?? throw new ArgumentNullException(nameof(offerPositionRepository));
    }

    public async Task<int> CopyOfferToProformaAsync(int offerId, int companyId, int userId, CancellationToken cancellationToken = default)
    {
        var existingId = await GetExistingInvoiceIdAsync(offerId, FpfSkrot, cancellationToken).ConfigureAwait(false);
        if (existingId.HasValue)
            return existingId.Value;

        return await CopyOfferToInvoiceAsync(offerId, companyId, FpfSkrot, cancellationToken).ConfigureAwait(false);
    }

    private async Task<int> CopyOfferToInvoiceAsync(int offerId, int companyId, string docType, CancellationToken cancellationToken)
    {
        var offer = await _offerRepository.GetByIdAsync(offerId, companyId, cancellationToken).ConfigureAwait(false);
        if (offer == null)
            throw new InvalidOperationException($"Oferta o ID {offerId} nie została znaleziona.");

        var positions = (await _offerPositionRepository.GetByOfferIdAsync(offerId, cancellationToken).ConfigureAwait(false)).ToList();
        var (sumNetto, sumVat, sumBrutto) = ComputePositionTotals(positions);
        var docDate = DateTime.Today;
        var dataFakturyClarion = (int)(docDate - new DateTime(1800, 12, 28)).TotalDays;

        return await _unitOfWork.ExecuteInTransactionAsync(async transaction =>
        {
            var conn = (transaction.Connection as MySqlConnection) ?? throw new InvalidOperationException("Brak połączenia MySQL w transakcji.");
            var (_, _, nextNo, _) = await _documentNumberService.GetNextNumberAsync(companyId, docType, docDate, transaction, cancellationToken).ConfigureAwait(false);
            var invoiceId = (int)await _idGenerator.GetNextIdAsync("faktury", conn, (MySqlTransaction)transaction, cancellationToken).ConfigureAwait(false);
            await InsertFakturaAsync(conn, transaction, invoiceId, offerId, companyId, offer, docType, nextNo, dataFakturyClarion, sumNetto, sumVat, sumBrutto, cancellationToken).ConfigureAwait(false);
            await VerifyRootDocIdAsync(conn, transaction, invoiceId, cancellationToken).ConfigureAwait(false);
            foreach (var pos in positions)
                await InsertPozycjaFakturyAsync(conn, transaction, invoiceId, offerId, companyId, pos, cancellationToken).ConfigureAwait(false);
            await _invoiceTotalsService.RecalculateTotalsAsync(invoiceId, transaction, cancellationToken).ConfigureAwait(false);
            await UpdateOfferSumBruttoFromInvoiceAsync(conn, transaction, offerId, companyId, invoiceId, cancellationToken).ConfigureAwait(false);
            return invoiceId;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<(int InvoiceId, bool CreatedNew)> CopyOfferToFpfAsync(int offerId, int companyId, CancellationToken cancellationToken = default)
    {
        if (offerId <= 0)
            throw new InvalidOperationException("Brak ID aoferty. Oferta musi być zapisana w bazie przed kopiowaniem do proformy.");

        var existingId = await GetExistingInvoiceIdAsync(offerId, FpfSkrot, cancellationToken).ConfigureAwait(false);
        if (existingId.HasValue)
            return (existingId.Value, false);

        var invoiceId = await CopyOfferToInvoiceAsync(offerId, companyId, FpfSkrot, cancellationToken).ConfigureAwait(false);
        return (invoiceId, true);
    }

    public async Task<(int InvoiceId, bool CreatedNew)> CopyOfferToFvzAsync(int offerId, int companyId, CancellationToken cancellationToken = default)
    {
        if (offerId <= 0)
            throw new InvalidOperationException("Brak ID aoferty. Oferta musi być zapisana w bazie przed kopiowaniem do faktury zaliczkowej.");

        var existingId = await GetExistingInvoiceIdAsync(offerId, FvzSkrot, cancellationToken).ConfigureAwait(false);
        if (existingId.HasValue)
            return (existingId.Value, false);

        var invoiceId = await CopyOfferToInvoiceAsync(offerId, companyId, FvzSkrot, cancellationToken).ConfigureAwait(false);
        return (invoiceId, true);
    }

    public async Task<(int InvoiceId, bool CreatedNew)> CopyOfferToFvAsync(int offerId, int companyId, CancellationToken cancellationToken = default)
    {
        if (offerId <= 0)
            throw new InvalidOperationException("Brak ID aoferty. Oferta musi być zapisana w bazie przed kopiowaniem do faktury VAT.");

        var existingId = await GetExistingInvoiceIdAsync(offerId, FvSkrot, cancellationToken).ConfigureAwait(false);
        if (existingId.HasValue)
            return (existingId.Value, false);

        var invoiceId = await CopyOfferToInvoiceAsync(offerId, companyId, FvSkrot, cancellationToken).ConfigureAwait(false);
        return (invoiceId, true);
    }

    private async Task<int?> GetExistingInvoiceIdAsync(int offerId, string docType, CancellationToken cancellationToken)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var cmd = new MySqlCommand(
            "SELECT id_faktury FROM faktury WHERE id_oferty = @OfferId AND doc_type = @Skrot LIMIT 1",
            connection);
        cmd.Parameters.AddWithValue("@OfferId", offerId);
        cmd.Parameters.AddWithValue("@Skrot", docType);
        var result = await cmd.ExecuteScalarWithDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
        if (result == null || result == DBNull.Value) return null;
        return Convert.ToInt32(result);
    }

    private static DateTime OfferDateToDateTime(int? offerDateClarion)
    {
        if (!offerDateClarion.HasValue || offerDateClarion.Value <= 0)
            return DateTime.Today;
        return new DateTime(1800, 12, 28).AddDays(offerDateClarion.Value);
    }

    /// <summary>Rabat w %, zaokrąglenia do 2 miejsc. Zwraca (sumNetto, sumVat, sumBrutto).</summary>
    private static (decimal sumNetto, decimal sumVat, decimal sumBrutto) ComputePositionTotals(List<OfferPosition> positions)
    {
        decimal sNetto = 0m, sVat = 0m, sBrutto = 0m;
        foreach (var pos in positions)
        {
            var (netto, vat, brutto) = ComputePositionAmounts(
                pos.Ilosc ?? 0m,
                pos.CenaNetto ?? 0m,
                pos.Discount ?? 0m,
                pos.VatRate);
            sNetto += netto;
            sVat += vat;
            sBrutto += brutto;
        }
        return (Math.Round(sNetto, 2), Math.Round(sVat, 2), Math.Round(sBrutto, 2));
    }

    /// <summary>
    /// Księgowo poprawny algorytm liczenia pozycji. Rabat w %. Nie zaokrąglać wcześniej niż w kroku 3.
    /// 1) netto0 = ilosc * cena_netto
    /// 2) netto_po_rabacie = netto0 * (1 - rabat/100)
    /// 3) netto_poz = ROUND(netto_po_rabacie, 2)
    /// 4) vat_poz = ROUND(netto_poz * stawka_vat/100, 2)
    /// 5) brutto_poz = netto_poz + vat_poz
    /// </summary>
    private static (decimal nettoPoz, decimal vatPoz, decimal bruttoPoz) ComputePositionAmounts(decimal ilosc, decimal cenaNetto, decimal rabatPercent, string? stawkaVat)
    {
        var netto0 = ilosc * cenaNetto;
        var nettoPoRabacie = netto0 * (1m - rabatPercent / 100m);
        var nettoPoz = Math.Round(nettoPoRabacie, 2);
        var vatRate = ParseVatRate(stawkaVat);
        var vatPoz = Math.Round(nettoPoz * vatRate / 100m, 2);
        var bruttoPoz = nettoPoz + vatPoz;
        return (nettoPoz, vatPoz, bruttoPoz);
    }

    private static decimal ParseVatRate(string? stawkaVat)
    {
        if (string.IsNullOrWhiteSpace(stawkaVat)) return 0m;
        var s = stawkaVat.Trim().TrimEnd('%');
        return decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var rate) ? rate : 0m;
    }

    private static async Task InsertFakturaAsync(MySqlConnection conn, MySqlTransaction transaction, int invoiceId, int offerId, int companyId, Offer offer, string docType, int docNo, int dataFakturyClarion, decimal sumNetto, decimal sumVat, decimal sumBrutto, CancellationToken cancellationToken)
    {
        var empty = "";
        var cmd = new MySqlCommand(
            "INSERT INTO faktury (id_faktury, id_oferty, parent_doc_id, root_doc_id, id_firmy, doc_type, skrot_nazwa_faktury, id_faktur_powiazanych, numer_faktury_korygowanej_text, data_sprzedazy, data_faktury_korygowanej, " +
            "skrot_kolejne_faktury, firma_nazwa, naglowek1, naglowek2, firma_adres, firma_kod_pocztowy, firma_miejscowosc, firma_panstwo, firma_nip, firma_bank_nazwa, firma_nr_konta, firma_iban, firma_swift_bic, " +
            "id_odbiorca, odbiorca_nazwa, odbiorca_adres, odbiorca_kod_pocztowy, odbiorca_miejscowosc, odbiorca_panstwo, odbiorca_nip, odbiorca_mail, " +
            "waluta, data_faktury, nr_faktury, kwota_netto, stawka_vat, total_vat, kwota_brutto, sum_netto, sum_vat, sum_brutto, uwagi_do_faktury, operator) " +
            "VALUES (@IdFaktury, @IdOferty, @ParentDocId, @RootDocId, @IdFirmy, @DocType, @SkrotNazwaFaktury, @IdFakturPowiazanych, '', @DataSprzedazy, @DataFakturyKorygowanej, '', " +
            "@FirmaNazwa, '', '', '', '', NULL, '', '', '', '', '', '', " +
            "@IdOdbiorca, @OdbiorcaNazwa, @OdbiorcaAdres, @OdbiorcaKodPocztowy, @OdbiorcaMiejscowosc, @OdbiorcaPanstwo, @OdbiorcaNip, @OdbiorcaMail, " +
            "@Waluta, @DataFaktury, @NrFaktury, @KwotaNetto, @StawkaVat, @TotalVat, @KwotaBrutto, @SumNetto, @SumVat, @SumBrutto, @Uwagi, @Operator)",
            conn, transaction);
        cmd.Parameters.AddWithValue("@IdFaktury", invoiceId);
        cmd.Parameters.AddWithValue("@IdOferty", offerId);
        cmd.Parameters.AddWithValue("@ParentDocId", DBNull.Value);
        cmd.Parameters.AddWithValue("@RootDocId", invoiceId);
        cmd.Parameters.AddWithValue("@IdFirmy", companyId);
        cmd.Parameters.AddWithValue("@DocType", docType);
        cmd.Parameters.AddWithValue("@SkrotNazwaFaktury", docType);
        cmd.Parameters.AddWithValue("@IdFakturPowiazanych", 0); // opcjonalne: FK do powiązanych faktur (korekty); nowa FPF = brak powiązań
        cmd.Parameters.AddWithValue("@NrFaktury", docNo);
        cmd.Parameters.AddWithValue("@DataSprzedazy", dataFakturyClarion);
        cmd.Parameters.AddWithValue("@DataFakturyKorygowanej", dataFakturyClarion);
        cmd.Parameters.AddWithValue("@FirmaNazwa", empty);
        cmd.Parameters.AddWithValue("@IdOdbiorca", offer.CustomerId ?? 0);
        cmd.Parameters.AddWithValue("@OdbiorcaNazwa", offer.CustomerName ?? empty);
        cmd.Parameters.AddWithValue("@OdbiorcaAdres", offer.CustomerStreet ?? empty);
        cmd.Parameters.AddWithValue("@OdbiorcaKodPocztowy", offer.CustomerPostalCode ?? empty);
        cmd.Parameters.AddWithValue("@OdbiorcaMiejscowosc", offer.CustomerCity ?? empty);
        cmd.Parameters.AddWithValue("@OdbiorcaPanstwo", offer.CustomerCountry ?? empty);
        cmd.Parameters.AddWithValue("@OdbiorcaNip", offer.CustomerNip ?? empty);
        cmd.Parameters.AddWithValue("@OdbiorcaMail", offer.CustomerEmail ?? empty);
        cmd.Parameters.AddWithValue("@Waluta", offer.Currency ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@DataFaktury", dataFakturyClarion);
        cmd.Parameters.AddWithValue("@KwotaNetto", sumNetto);
        cmd.Parameters.AddWithValue("@StawkaVat", offer.VatRate?.ToString() ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@TotalVat", sumVat);
        cmd.Parameters.AddWithValue("@KwotaBrutto", sumBrutto);
        cmd.Parameters.AddWithValue("@SumNetto", sumNetto);
        cmd.Parameters.AddWithValue("@SumVat", sumVat);
        cmd.Parameters.AddWithValue("@SumBrutto", sumBrutto);
        cmd.Parameters.AddWithValue("@Uwagi", offer.OfferNotes ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Operator", offer.Operator ?? empty);

        await cmd.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task VerifyRootDocIdAsync(MySqlConnection conn, MySqlTransaction transaction, int invoiceId, CancellationToken cancellationToken)
    {
        var cmd = new MySqlCommand(
            "SELECT root_doc_id FROM faktury WHERE id_faktury = @InvoiceId",
            conn, transaction);
        cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
        var result = await cmd.ExecuteScalarWithDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
        if (result == null || result == DBNull.Value)
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "faktury_root_doc_id_missing.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] root_doc_id NULL for faktury.id_faktury={invoiceId}\r\n");
        }
    }

    private async Task InsertPozycjaFakturyAsync(MySqlConnection conn, MySqlTransaction transaction, int invoiceId, int offerId, int companyId, OfferPosition pos, CancellationToken cancellationToken)
    {
        var ilosc = pos.Ilosc ?? 0m;
        var cenaNetto = pos.CenaNetto ?? 0m;
        var rabat = pos.Discount ?? 0m;
        var (nettoPoz, vatPoz, bruttoPoz) = ComputePositionAmounts(ilosc, cenaNetto, rabat, pos.VatRate);

        var posId = (int)await _idGenerator.GetNextIdAsync("pozycjefaktury", conn, transaction, cancellationToken).ConfigureAwait(false);

        var cmd = new MySqlCommand(
            "INSERT INTO pozycjefaktury (id, id_pozycji_faktury, id_firmy, faktura_id, id_faktury, id_oferty, po_korekcie, Nazwa_towaru, Nazwa_towaru_eng, jednostki, ilosc, cena_netto, rabat, stawka_vat, netto_poz, vat_poz, brutto_poz, id_towaru, nr_zespolu, id_pozycji_oferty) " +
            "VALUES (@Id, @IdPozycjiFaktury, @IdFirmy, @IdFaktury, @IdFaktury, @IdOferty, 0, @NazwaTowaru, @NazwaTowaruEng, @Jednostki, @Ilosc, @CenaNetto, @Rabat, @StawkaVat, @NettoPoz, @VatPoz, @BruttoPoz, @IdTowaru, @NrZespolu, @IdPozycjiOferty);",
            conn, transaction);
        cmd.Parameters.AddWithValue("@Id", posId);
        cmd.Parameters.AddWithValue("@IdPozycjiFaktury", posId);
        cmd.Parameters.AddWithValue("@IdFirmy", companyId);
        cmd.Parameters.AddWithValue("@IdFaktury", invoiceId);
        cmd.Parameters.AddWithValue("@IdOferty", offerId);
        cmd.Parameters.AddWithValue("@NazwaTowaru", pos.Name ?? "");
        cmd.Parameters.AddWithValue("@NazwaTowaruEng", pos.NameEng ?? "");
        cmd.Parameters.AddWithValue("@Jednostki", pos.Unit ?? "szt");
        cmd.Parameters.AddWithValue("@Ilosc", ilosc);
        cmd.Parameters.AddWithValue("@CenaNetto", cenaNetto);
        cmd.Parameters.AddWithValue("@Rabat", rabat);
        cmd.Parameters.AddWithValue("@StawkaVat", pos.VatRate ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@NettoPoz", nettoPoz);
        cmd.Parameters.AddWithValue("@VatPoz", vatPoz);
        cmd.Parameters.AddWithValue("@BruttoPoz", bruttoPoz);
        cmd.Parameters.AddWithValue("@IdTowaru", pos.ProductId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@NrZespolu", pos.GroupNumber ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@IdPozycjiOferty", pos.Id);

        await cmd.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Po utworzeniu FPF/FV: ustaw aoferty.sum_brutto = faktury.sum_brutto (w tej samej transakcji).</summary>
    private static async Task UpdateOfferSumBruttoFromInvoiceAsync(MySqlConnection conn, MySqlTransaction transaction, int offerId, int companyId, int invoiceId, CancellationToken cancellationToken)
    {
        var cmd = new MySqlCommand(
            "UPDATE aoferty o " +
            "INNER JOIN faktury f ON f.id_faktury = @InvoiceId AND f.id_firmy = @CompanyId " +
            "SET o.sum_brutto = f.sum_brutto " +
            "WHERE o.id_oferta = @OfferId AND o.id_firmy = @CompanyId",
            conn, transaction);
        cmd.Parameters.AddWithValue("@OfferId", offerId);
        cmd.Parameters.AddWithValue("@CompanyId", companyId);
        cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
        await cmd.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
    }
}
