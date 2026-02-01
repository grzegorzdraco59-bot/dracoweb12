namespace ERP.Application.Services;

/// <summary>
/// Serwis do atomowego pobierania kolejnych numerów dokumentów (np. FV) z tabeli doc_counters.
/// Nie otwiera transakcji – używa przekazanej. Numer nadawany TYLKO przy zapisie (Create).
/// </summary>
public interface IDocumentNumberService
{
    /// <summary>
    /// Pobiera kolejny numer faktury dla danej firmy i roku (z invoiceDate).
    /// doc_type = 'FV'. Wykonuje INSERT ... ON DUPLICATE KEY UPDATE w ramach przekazanej transakcji.
    /// </summary>
    /// <param name="companyId">id_firmy</param>
    /// <param name="invoiceDate">data faktury – rok określa numerację</param>
    /// <param name="transaction">aktywna transakcja (ta sama, w której będzie INSERT do faktury)</param>
    /// <param name="cancellationToken">token anulowania</param>
    /// <returns>Kolejny numer (next_no); doc_full_no = "FV/{year}/{month:D2}/{nextNo:D6}"</returns>
    Task<int> GetNextInvoiceNumberAsync(int companyId, DateTime invoiceDate, System.Data.IDbTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera kolejny numer dokumentu dla (company_id, doc_type, year, month).
    /// Atomowo: INSERT INTO doc_counters(company_id, doc_type, year, month, last_no) ... ON DUPLICATE KEY UPDATE last_no = LAST_INSERT_ID(last_no + 1); SELECT LAST_INSERT_ID() AS next_no.
    /// </summary>
    /// <param name="companyId">id_firmy (company_id)</param>
    /// <param name="docType">typ: FPF, FV, FVZ, FVK</param>
    /// <param name="year">YEAR(datydokumentu)</param>
    /// <param name="month">MONTH(datydokumentu)</param>
    /// <param name="transaction">aktywna transakcja</param>
    /// <param name="cancellationToken">token anulowania</param>
    /// <returns>next_no; doc_full_no = "{docType}/{year}/{month:D2}/{nextNo:D6}"</returns>
    Task<int> GetNextDocumentNumberAsync(int companyId, string docType, int year, int month, System.Data.IDbTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Jedno źródło prawdy: pobiera następny numer dla (companyId, docType, docDate).
    /// year = docDate.Year, month = docDate.Month, fullNo = "{docType}/{year}/{month:D2}/{nextNo:D6}".
    /// </summary>
    /// <param name="companyId">id_firmy (company_id)</param>
    /// <param name="docType">typ: FPF, FV, FVZ, FVK</param>
    /// <param name="docDate">data dokumentu (nie DateTime.Now jeśli w systemie jest inne pole daty)</param>
    /// <param name="transaction">aktywna transakcja</param>
    /// <param name="cancellationToken">token anulowania</param>
    /// <returns>(year, month, nextNo, fullNo) – format fullNo: FPF/2026/01/000123</returns>
    Task<(int Year, int Month, int NextNo, string FullNo)> GetNextNumberAsync(int companyId, string docType, DateTime docDate, System.Data.IDbTransaction transaction, CancellationToken cancellationToken = default);
}
