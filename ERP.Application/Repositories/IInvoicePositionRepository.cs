using ERP.Application.DTOs;

namespace ERP.Application.Repositories;

/// <summary>
/// Repozytorium odczytu pozycji faktur (tabela pozycjefaktury).
/// </summary>
public interface IInvoicePositionRepository
{
    Task<IEnumerable<InvoicePositionDto>> GetByInvoiceIdAsync(int invoiceId, CancellationToken cancellationToken = default);
}
