using ERP.Application.DTOs;

namespace ERP.Application.Repositories;

/// <summary>
/// Operacje zapisu dla kontrahentów (tabele bazowe), bez modyfikacji widoków.
/// </summary>
public interface IKontrahenciCommandRepository
{
    Task<KontrahentLookupDto?> GetByIdAsync(
        int companyId,
        int kontrahentId,
        CancellationToken cancellationToken = default);

    Task<int> AddAsync(
        int companyId,
        string? typ,
        string? name,
        string? email,
        string? phone,
        string? city,
        string? currency,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        int companyId,
        int kontrahentId,
        string? typ,
        string? name,
        string? email,
        string? phone,
        string? city,
        string? currency,
        CancellationToken cancellationToken = default);

    Task<bool> IsUsedInDocumentsAsync(
        int companyId,
        int kontrahentId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int companyId,
        int kontrahentId,
        CancellationToken cancellationToken = default);
}
