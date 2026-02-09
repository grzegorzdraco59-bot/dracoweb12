using ERP.Application.DTOs;

namespace ERP.Application.Services;

/// <summary>
/// Interfejs serwisu aplikacyjnego dla kontrahenta (Supplier)
/// </summary>
public interface ISupplierService
{
    Task<SupplierDto?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken = default);
    Task<SupplierDto?> GetByKontrahentIdAsync(int kontrahentId, int companyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupplierDto>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);
    Task<SupplierDto> CreateAsync(SupplierDto supplierDto, CancellationToken cancellationToken = default);
    Task<SupplierDto> UpdateAsync(SupplierDto supplierDto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, int companyId, CancellationToken cancellationToken = default);
}
