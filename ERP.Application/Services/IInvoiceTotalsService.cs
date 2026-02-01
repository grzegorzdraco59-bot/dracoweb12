namespace ERP.Application.Services;

/// <summary>
/// Przeliczanie sum nagłówka faktury z pozycji (sum_netto, sum_vat, sum_brutto).
/// Wywołać po: zapisie pozycji, kopiowaniu dokumentu, korekcie.
/// </summary>
public interface IInvoiceTotalsService
{
    /// <summary>
    /// Przelicza netto_poz, vat_poz, brutto_poz dla wszystkich pozycji faktury (w DB),
    /// następnie sum_netto, sum_vat, sum_brutto w nagłówku. Wywołać po dodaniu/edycji/usunięciu pozycji.
    /// </summary>
    Task RecalculateInvoicePositionsAndTotalsAsync(int invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera SUM(netto_poz), SUM(vat_poz), SUM(brutto_poz) z pozycjefaktury dla danej faktury
    /// i zapisuje do tabeli faktury (sum_netto, sum_vat, sum_brutto).
    /// Zapewnia spójność: suma pozycji = nagłówek.
    /// </summary>
    Task RecalculateTotalsAsync(int invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Jak wyżej, w ramach przekazanej transakcji (np. po insertach pozycji).
    /// </summary>
    Task RecalculateTotalsAsync(int invoiceId, System.Data.IDbTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dla faktury końcowej FV wylicza sumę zaliczek FVZ (tej samej sprawy: root_doc_id) i kwotę do zapłaty.
    /// Ustawia: sum_zaliczek_brutto = SUM(sum_brutto) FVZ w sprawie, do_zaplaty_brutto = sum_brutto - sum_zaliczek_brutto (min 0).
    /// Wywołać: po utworzeniu FV z FPF, po utworzeniu/edycji/usunięciu FVZ w sprawie, po przeliczeniu sum FV (RecalculateTotals).
    /// </summary>
    Task RecalculateFinalInvoicePaymentsAsync(int fvId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Jak wyżej, w ramach przekazanej transakcji (np. po utworzeniu FV).
    /// </summary>
    Task RecalculateFinalInvoicePaymentsAsync(int fvId, System.Data.IDbTransaction transaction, CancellationToken cancellationToken = default);
}
