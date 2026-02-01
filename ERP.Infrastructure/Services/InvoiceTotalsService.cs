using ERP.Application.Services;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Services;

/// <summary>
/// Przeliczanie sum nagłówka faktury z pozycji. Dane wyłącznie z pozycjefaktury.
/// sum_netto = SUM(netto_poz), sum_vat = SUM(vat_poz), sum_brutto = SUM(brutto_poz).
/// </summary>
public class InvoiceTotalsService : IInvoiceTotalsService
{
    private readonly DatabaseContext _context;

    public InvoiceTotalsService(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task RecalculateTotalsAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await RecalculateTotalsCoreAsync(connection, transaction, invoiceId, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RecalculateTotalsAsync(int invoiceId, System.Data.IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var mysqlTransaction = transaction as MySqlTransaction
            ?? throw new ArgumentException("Transakcja musi być MySqlTransaction.", nameof(transaction));
        var conn = mysqlTransaction.Connection ?? throw new InvalidOperationException("Brak połączenia w transakcji.");
        await RecalculateTotalsCoreAsync(conn, mysqlTransaction, invoiceId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RecalculateFinalInvoicePaymentsAsync(int fvId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await RecalculateFinalInvoicePaymentsCoreAsync(connection, transaction, fvId, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RecalculateFinalInvoicePaymentsAsync(int fvId, System.Data.IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var mysqlTransaction = transaction as MySqlTransaction
            ?? throw new ArgumentException("Transakcja musi być MySqlTransaction.", nameof(transaction));
        var conn = mysqlTransaction.Connection ?? throw new InvalidOperationException("Brak połączenia w transakcji.");
        await RecalculateFinalInvoicePaymentsCoreAsync(conn, mysqlTransaction, fvId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 1) sum_netto = SUM(netto_poz), sum_vat = SUM(vat_poz), sum_brutto = SUM(brutto_poz) z pozycjefaktury.
    /// 2) UPDATE faktury SET sum_netto, sum_vat, sum_brutto WHERE Id_faktury = @invoiceId.
    /// </summary>
    private static async Task RecalculateTotalsCoreAsync(MySqlConnection conn, MySqlTransaction transaction, int invoiceId, CancellationToken cancellationToken)
    {
        var selectCmd = new MySqlCommand(
            "SELECT COALESCE(SUM(netto_poz), 0), COALESCE(SUM(vat_poz), 0), COALESCE(SUM(brutto_poz), 0) " +
            "FROM pozycjefaktury WHERE id_faktury = @InvoiceId",
            conn, transaction);
        selectCmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
        await using var reader = await selectCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return;
        var sumNetto = reader.GetDecimal(0);
        var sumVat = reader.GetDecimal(1);
        var sumBrutto = reader.GetDecimal(2);
        await reader.CloseAsync().ConfigureAwait(false);

        var updateCmd = new MySqlCommand(
            "UPDATE faktury SET sum_netto = @SumNetto, sum_vat = @SumVat, sum_brutto = @SumBrutto WHERE Id_faktury = @InvoiceId",
            conn, transaction);
        updateCmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
        updateCmd.Parameters.AddWithValue("@SumNetto", sumNetto);
        updateCmd.Parameters.AddWithValue("@SumVat", sumVat);
        updateCmd.Parameters.AddWithValue("@SumBrutto", sumBrutto);
        await updateCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Dla FV: 1) Pobierz root_doc_id, id_firmy, sum_brutto. 2) Policz SUM(sum_brutto) FVZ w tej samej sprawie. 3) UPDATE sum_zaliczek_brutto, do_zaplaty_brutto (min 0).
    /// </summary>
    private static async Task RecalculateFinalInvoicePaymentsCoreAsync(MySqlConnection conn, MySqlTransaction transaction, int fvId, CancellationToken cancellationToken)
    {
        // 1) Pobierz root_doc_id, id_firmy, sum_brutto tej FV
        long? rootDocId = null;
        int companyId = 0;
        decimal sumBrutto = 0;
        var headCmd = new MySqlCommand(
            "SELECT root_doc_id, id_firmy, COALESCE(sum_brutto, 0) AS s_brutto FROM faktury WHERE Id_faktury = @InvoiceId",
            conn, transaction);
        headCmd.Parameters.AddWithValue("@InvoiceId", fvId);
        await using (var headReader = await headCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!await headReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                return;
            var rootDocIdOrdinal = headReader.GetOrdinal("root_doc_id");
            rootDocId = headReader.IsDBNull(rootDocIdOrdinal) ? null : headReader.GetInt64(rootDocIdOrdinal);
            companyId = headReader.GetInt32(headReader.GetOrdinal("id_firmy"));
            sumBrutto = headReader.GetDecimal(headReader.GetOrdinal("s_brutto"));
        }

        decimal sumZaliczekBrutto;
        if (!rootDocId.HasValue)
        {
            sumZaliczekBrutto = 0;
        }
        else
        {
            // 2) Suma brutto zaliczek FVZ w tej samej sprawie (bez tej faktury)
            var sumCmd = new MySqlCommand(
                "SELECT COALESCE(SUM(sum_brutto), 0) FROM faktury " +
                "WHERE id_firmy = @CompanyId AND root_doc_id = @RootId AND doc_type = 'FVZ' AND Id_faktury <> @InvoiceId",
                conn, transaction);
            sumCmd.Parameters.AddWithValue("@CompanyId", companyId);
            sumCmd.Parameters.AddWithValue("@RootId", rootDocId.Value);
            sumCmd.Parameters.AddWithValue("@InvoiceId", fvId);
            var sumObj = await sumCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            sumZaliczekBrutto = sumObj is null || sumObj == DBNull.Value ? 0 : Convert.ToDecimal(sumObj);
        }

        // 3) do_zaplaty_brutto = sum_brutto - sum_zaliczek_brutto, min 0
        var doZaplatyBrutto = Math.Max(0, Math.Round(sumBrutto - sumZaliczekBrutto, 2));

        var updateCmd = new MySqlCommand(
            "UPDATE faktury SET sum_zaliczek_brutto = @SumZaliczek, do_zaplaty_brutto = @DoZaplaty WHERE Id_faktury = @InvoiceId",
            conn, transaction);
        updateCmd.Parameters.AddWithValue("@InvoiceId", fvId);
        updateCmd.Parameters.AddWithValue("@SumZaliczek", sumZaliczekBrutto);
        updateCmd.Parameters.AddWithValue("@DoZaplaty", doZaplatyBrutto);
        await updateCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
