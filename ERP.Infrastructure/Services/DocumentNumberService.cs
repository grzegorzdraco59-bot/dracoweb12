using ERP.Application.Services;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Services;

/// <summary>
/// Atomowe pobieranie kolejnego numeru dokumentu (FV) z doc_counters w ramach przekazanej transakcji.
/// SQL: INSERT ... ON DUPLICATE KEY UPDATE last_no = LAST_INSERT_ID(last_no + 1); SELECT LAST_INSERT_ID() AS next_no;
/// </summary>
public class DocumentNumberService : IDocumentNumberService
{
    private const string DocTypeFv = "FV";

    public async Task<int> GetNextInvoiceNumberAsync(int companyId, DateTime invoiceDate, System.Data.IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        return await GetNextDocumentNumberAsync(companyId, DocTypeFv, invoiceDate.Year, invoiceDate.Month, transaction, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> GetNextDocumentNumberAsync(int companyId, string docType, int year, int month, System.Data.IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (transaction == null)
            throw new ArgumentNullException(nameof(transaction));
        if (string.IsNullOrWhiteSpace(docType))
            throw new ArgumentException("docType nie może być puste.", nameof(docType));

        var mysqlTransaction = transaction as MySqlTransaction
            ?? throw new ArgumentException("Transakcja musi być MySqlTransaction.", nameof(transaction));

        var conn = mysqlTransaction.Connection ?? throw new InvalidOperationException("Brak połączenia w transakcji.");

        // KROK 3: atomowe pobranie next_no (year + month)
        var insertCmd = new MySqlCommand(
            "INSERT INTO doc_counters(company_id, doc_type, year, month, last_no) " +
            "VALUES (@companyId, @docType, @year, @month, 1) " +
            "ON DUPLICATE KEY UPDATE last_no = LAST_INSERT_ID(last_no + 1);",
            conn, mysqlTransaction);
        insertCmd.Parameters.AddWithValue("@companyId", companyId);
        insertCmd.Parameters.AddWithValue("@docType", docType);
        insertCmd.Parameters.AddWithValue("@year", year);
        insertCmd.Parameters.AddWithValue("@month", month);
        await insertCmd.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken).ConfigureAwait(false);

        var selectCmd = new MySqlCommand("SELECT LAST_INSERT_ID() AS next_no;", conn, mysqlTransaction);
        var result = await selectCmd.ExecuteScalarWithDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
        var nextNo = result == null || result == DBNull.Value ? 1 : Convert.ToInt32(result);
        return nextNo;
    }

    /// <inheritdoc />
    public async Task<(int Year, int Month, int NextNo, string FullNo)> GetNextNumberAsync(int companyId, string docType, DateTime docDate, System.Data.IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var year = docDate.Year;
        var month = docDate.Month;
        var nextNo = await GetNextDocumentNumberAsync(companyId, docType, year, month, transaction, cancellationToken).ConfigureAwait(false);
        var fullNo = $"{docType}/{year}/{month:D2}/{nextNo:D6}";
        return (year, month, nextNo, fullNo);
    }
}
