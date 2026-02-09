using ERP.Application.Services;
using System.IO;
using ERP.Application.Repositories;
using ERP.Infrastructure.Data;
using ERP.Domain.Repositories;
using MySqlConnector;

namespace ERP.Infrastructure.Services;

/// <summary>
/// Serwis kopiowania faktury do nowego dokumentu (FVZ, FV).
/// Kopiuje nagłówek i pozycje z istniejącej faktury.
/// </summary>
public class InvoiceCopyService : IInvoiceCopyService
{
    private const string FvzSkrot = "FVZ";
    private const string FvSkrot = "FV";

    private readonly DatabaseContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdGenerator _idGenerator;
    private readonly IDocumentNumberService _documentNumberService;
    private readonly IInvoiceTotalsService _invoiceTotalsService;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IInvoicePositionRepository _invoicePositionRepository;

    public InvoiceCopyService(
        DatabaseContext context,
        IUnitOfWork unitOfWork,
        IIdGenerator idGenerator,
        IDocumentNumberService documentNumberService,
        IInvoiceTotalsService invoiceTotalsService,
        IInvoiceRepository invoiceRepository,
        IInvoicePositionRepository invoicePositionRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        _documentNumberService = documentNumberService ?? throw new ArgumentNullException(nameof(documentNumberService));
        _invoiceTotalsService = invoiceTotalsService ?? throw new ArgumentNullException(nameof(invoiceTotalsService));
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _invoicePositionRepository = invoicePositionRepository ?? throw new ArgumentNullException(nameof(invoicePositionRepository));
    }

    public async Task<int> CopyInvoiceToFvzAsync(long sourceInvoiceId, int companyId, CancellationToken cancellationToken = default)
    {
        return await CopyInvoiceToDocTypeAsync(sourceInvoiceId, companyId, FvzSkrot, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CopyInvoiceToFvAsync(long sourceInvoiceId, int companyId, CancellationToken cancellationToken = default)
    {
        return await CopyInvoiceToDocTypeAsync(sourceInvoiceId, companyId, FvSkrot, cancellationToken).ConfigureAwait(false);
    }

    private async Task<int> CopyInvoiceToDocTypeAsync(long sourceInvoiceId, int companyId, string docType, CancellationToken cancellationToken)
    {
        if (sourceInvoiceId <= 0)
            throw new InvalidOperationException("Brak ID źródłowej faktury.");

        var sourceInvoice = await GetSourceInvoiceAsync(sourceInvoiceId, companyId, cancellationToken).ConfigureAwait(false);
        if (sourceInvoice == null)
            throw new InvalidOperationException($"Faktura o ID {sourceInvoiceId} nie została znaleziona.");

        var positions = (await _invoicePositionRepository.GetByInvoiceIdAsync(sourceInvoiceId, cancellationToken).ConfigureAwait(false)).ToList();
        var docDate = DateTime.Today;
        var dataFakturyClarion = sourceInvoice.DataFaktury ?? (int)(docDate - new DateTime(1800, 12, 28)).TotalDays;
        var sumNetto = sourceInvoice.SumNetto ?? 0m;
        var sumVat = sourceInvoice.SumVat ?? 0m;
        var sumBrutto = sourceInvoice.SumBrutto ?? 0m;

        return await _unitOfWork.ExecuteInTransactionAsync(async transaction =>
        {
            var conn = (transaction.Connection as MySqlConnection) ?? throw new InvalidOperationException("Brak połączenia MySQL w transakcji.");
            var mysqlTransaction = (MySqlTransaction)transaction;

            var nextNo = await _invoiceRepository.GetNextInvoiceNumberAsync(companyId, docType, dataFakturyClarion, cancellationToken).ConfigureAwait(false);
            var docDateConverted = new DateTime(1800, 12, 28).AddDays(dataFakturyClarion);
            var docFullNo = $"{docType}/{docDateConverted.Month:D2}/{docDateConverted.Year}/{nextNo}";

            var newInvoiceId = (int)await _idGenerator.GetNextIdAsync("faktury", conn, mysqlTransaction, cancellationToken).ConfigureAwait(false);
            var sourceRootDocId = await GetRootDocIdAsync(conn, mysqlTransaction, sourceInvoiceId, cancellationToken).ConfigureAwait(false);
            var rootDocId = sourceRootDocId ?? sourceInvoiceId;
            await InsertFakturaFromSourceAsync(conn, mysqlTransaction, newInvoiceId, companyId, sourceInvoice, docType, docDateConverted.Year, docDateConverted.Month, nextNo, docFullNo, dataFakturyClarion, sumNetto, sumVat, sumBrutto, sourceInvoiceId, rootDocId, cancellationToken).ConfigureAwait(false);
            await VerifyRootDocIdAsync(conn, mysqlTransaction, newInvoiceId, cancellationToken).ConfigureAwait(false);

            foreach (var pos in positions)
                await InsertPozycjaFromDtoAsync(conn, mysqlTransaction, newInvoiceId, sourceInvoice.IdOferty ?? 0, companyId, pos, cancellationToken).ConfigureAwait(false);

            await _invoiceTotalsService.RecalculateTotalsAsync(newInvoiceId, transaction, cancellationToken).ConfigureAwait(false);
            return newInvoiceId;
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ERP.Application.DTOs.InvoiceDto?> GetSourceInvoiceAsync(long invoiceId, int companyId, CancellationToken cancellationToken)
    {
        return await _invoiceRepository.GetByIdAsync(invoiceId, companyId, cancellationToken).ConfigureAwait(false);
    }

    private static async Task InsertFakturaFromSourceAsync(MySqlConnection conn, MySqlTransaction transaction, int invoiceId, int companyId, ERP.Application.DTOs.InvoiceDto source, string docType, int docYear, int docMonth, int docNo, string docFullNo, int dataFakturyClarion, decimal sumNetto, decimal sumVat, decimal sumBrutto, long parentDocId, long rootDocId, CancellationToken cancellationToken)
    {
        var empty = "";
        var cmd = new MySqlCommand(
            "INSERT INTO faktury (id_faktury, id_oferty, source_offer_id, parent_doc_id, root_doc_id, id_firmy, doc_type, doc_year, doc_month, doc_no, doc_full_no, skrot_nazwa_faktury, id_faktur_powiazanych, numer_faktury_korygowanej_text, data_sprzedazy, data_faktury_korygowanej, " +
            "skrot_kolejne_faktury, firma_nazwa, naglowek1, naglowek2, firma_adres, firma_kod_pocztowy, firma_miejscowosc, firma_panstwo, firma_nip, firma_bank_nazwa, firma_nr_konta, firma_iban, firma_swift_bic, " +
            "id_odbiorca, odbiorca_nazwa, odbiorca_adres, odbiorca_kod_pocztowy, odbiorca_miejscowosc, odbiorca_panstwo, odbiorca_nip, odbiorca_mail, " +
            "waluta, data_faktury, nr_faktury, kwota_netto, stawka_vat, total_vat, kwota_brutto, sum_netto, sum_vat, sum_brutto, uwagi_do_faktury, operator) " +
            "VALUES (@IdFaktury, @IdOferty, @SourceOfferId, @ParentDocId, @RootDocId, @IdFirmy, @DocType, @DocYear, @DocMonth, @DocNo, @DocFullNo, @SkrotNazwaFaktury, 0, '', @DataSprzedazy, @DataFakturyKorygowanej, '', " +
            "'', '', '', '', NULL, '', '', '', '', '', '', " +
            "0, @OdbiorcaNazwa, '', '', '', '', '', '', " +
            "@Waluta, @DataFaktury, @NrFaktury, @KwotaNetto, NULL, @TotalVat, @KwotaBrutto, @SumNetto, @SumVat, @SumBrutto, '', @Operator)",
            conn, transaction);
        cmd.Parameters.AddWithValue("@IdFaktury", invoiceId);
        cmd.Parameters.AddWithValue("@IdOferty", source.IdOferty ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@SourceOfferId", source.IdOferty ?? 0);
        cmd.Parameters.AddWithValue("@ParentDocId", parentDocId);
        cmd.Parameters.AddWithValue("@RootDocId", rootDocId);
        cmd.Parameters.AddWithValue("@IdFirmy", companyId);
        cmd.Parameters.AddWithValue("@DocType", docType);
        cmd.Parameters.AddWithValue("@DocYear", docYear);
        cmd.Parameters.AddWithValue("@DocMonth", docMonth);
        cmd.Parameters.AddWithValue("@DocNo", docNo);
        cmd.Parameters.AddWithValue("@DocFullNo", docFullNo);
        cmd.Parameters.AddWithValue("@SkrotNazwaFaktury", docType);
        cmd.Parameters.AddWithValue("@DataSprzedazy", dataFakturyClarion);
        cmd.Parameters.AddWithValue("@DataFakturyKorygowanej", dataFakturyClarion);
        cmd.Parameters.AddWithValue("@OdbiorcaNazwa", source.OdbiorcaNazwa ?? empty);
        cmd.Parameters.AddWithValue("@Waluta", source.Waluta ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@DataFaktury", dataFakturyClarion);
        cmd.Parameters.AddWithValue("@NrFaktury", docNo);
        cmd.Parameters.AddWithValue("@KwotaNetto", sumNetto);
        cmd.Parameters.AddWithValue("@TotalVat", sumVat);
        cmd.Parameters.AddWithValue("@KwotaBrutto", sumBrutto);
        cmd.Parameters.AddWithValue("@SumNetto", sumNetto);
        cmd.Parameters.AddWithValue("@SumVat", sumVat);
        cmd.Parameters.AddWithValue("@SumBrutto", sumBrutto);
        cmd.Parameters.AddWithValue("@Operator", source.Operator ?? empty);

        await cmd.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<long?> GetRootDocIdAsync(MySqlConnection conn, MySqlTransaction transaction, long invoiceId, CancellationToken cancellationToken)
    {
        var cmd = new MySqlCommand(
            "SELECT root_doc_id FROM faktury WHERE id_faktury = @InvoiceId",
            conn, transaction);
        cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
        var result = await cmd.ExecuteScalarWithDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
        if (result == null || result == DBNull.Value)
            return null;
        return Convert.ToInt64(result);
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

    private async Task InsertPozycjaFromDtoAsync(MySqlConnection conn, MySqlTransaction transaction, int invoiceId, int offerId, int companyId, ERP.Application.DTOs.InvoicePositionDto pos, CancellationToken cancellationToken)
    {
        var posId = (int)await _idGenerator.GetNextIdAsync("pozycjefaktury", conn, transaction, cancellationToken).ConfigureAwait(false);

        var cmd = new MySqlCommand(
            "INSERT INTO pozycjefaktury (id, id_pozycji_faktury, id_firmy, faktura_id, id_faktury, id_oferty, po_korekcie, Nazwa_towaru, Nazwa_towaru_eng, jednostki, ilosc, cena_netto, rabat, stawka_vat, netto_poz, vat_poz, brutto_poz, id_towaru, nr_zespolu, id_pozycji_oferty) " +
            "VALUES (@Id, @IdPozycjiFaktury, @IdFirmy, @IdFaktury, @IdFaktury, @IdOferty, 0, @NazwaTowaru, @NazwaTowaruEng, @Jednostki, @Ilosc, @CenaNetto, @Rabat, @StawkaVat, @NettoPoz, @VatPoz, @BruttoPoz, NULL, NULL, NULL);",
            conn, transaction);
        cmd.Parameters.AddWithValue("@Id", posId);
        cmd.Parameters.AddWithValue("@IdPozycjiFaktury", posId);
        cmd.Parameters.AddWithValue("@IdFirmy", companyId);
        cmd.Parameters.AddWithValue("@IdFaktury", invoiceId);
        cmd.Parameters.AddWithValue("@IdOferty", offerId);
        cmd.Parameters.AddWithValue("@NazwaTowaru", pos.NazwaTowaru ?? "");
        cmd.Parameters.AddWithValue("@NazwaTowaruEng", pos.NazwaTowaruEng ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Jednostki", pos.Jednostki ?? "szt");
        cmd.Parameters.AddWithValue("@Ilosc", pos.Ilosc);
        cmd.Parameters.AddWithValue("@CenaNetto", pos.CenaNetto);
        cmd.Parameters.AddWithValue("@Rabat", pos.Rabat);
        cmd.Parameters.AddWithValue("@StawkaVat", pos.StawkaVat ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@NettoPoz", pos.NettoPoz);
        cmd.Parameters.AddWithValue("@VatPoz", pos.VatPoz);
        cmd.Parameters.AddWithValue("@BruttoPoz", pos.BruttoPoz);

        await cmd.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
    }
}
