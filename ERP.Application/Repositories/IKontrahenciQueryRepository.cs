using ERP.Application.DTOs;

namespace ERP.Application.Repositories;

/// <summary>
/// Repozytorium lookupu kontrahentów – tylko SELECT z kontrahenci_v.
/// </summary>
public interface IKontrahenciQueryRepository
{
    Task<IEnumerable<KontrahentLookupDto>> GetAllForCompanyAsync(int companyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<KontrahentLookupDto>> SearchAsync(int companyId, string? queryText, CancellationToken cancellationToken = default);
}
