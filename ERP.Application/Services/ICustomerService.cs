using ERP.Application.DTOs;

namespace ERP.Application.Services;

/// <summary>
/// Interfejs serwisu aplikacyjnego dla Odbiorcy (Customer)
/// Zawiera logikę biznesową i walidacje
/// </summary>
public interface ICustomerService
{
    Task<CustomerDto?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerDto>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerDto>> GetActiveByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateAsync(CustomerDto customerDto, CancellationToken cancellationToken = default);
    Task<CustomerDto> UpdateAsync(CustomerDto customerDto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, int companyId, CancellationToken cancellationToken = default);
}
