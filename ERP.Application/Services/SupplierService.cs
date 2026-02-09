using ERP.Application.DTOs;
using ERP.Domain.Entities;
using ERP.Domain.Repositories;

namespace ERP.Application.Services;

/// <summary>
/// Implementacja serwisu aplikacyjnego dla kontrahenta (Supplier)
/// </summary>
public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _repository;

    public SupplierService(ISupplierRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<SupplierDto?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("Id musi być większe od zera.", nameof(id));
        if (companyId <= 0)
            throw new ArgumentException("CompanyId musi być większe od zera.", nameof(companyId));

        var supplier = await _repository.GetByIdAsync(id, cancellationToken);
        return supplier != null ? MapToDto(supplier) : null;
    }

    public async Task<SupplierDto?> GetByKontrahentIdAsync(int kontrahentId, int companyId, CancellationToken cancellationToken = default)
    {
        if (kontrahentId <= 0)
            throw new ArgumentException("KontrahentId musi być większe od zera.", nameof(kontrahentId));
        if (companyId <= 0)
            throw new ArgumentException("CompanyId musi być większe od zera.", nameof(companyId));

        var supplier = await _repository.GetByKontrahentIdAsync(kontrahentId, companyId, cancellationToken);
        return supplier != null ? MapToDto(supplier) : null;
    }

    public async Task<IEnumerable<SupplierDto>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        if (companyId <= 0)
            throw new ArgumentException("CompanyId musi być większe od zera.", nameof(companyId));

        var suppliers = await _repository.GetAllAsync(cancellationToken);
        return suppliers.Select(MapToDto);
    }

    public async Task<SupplierDto> CreateAsync(SupplierDto supplierDto, CancellationToken cancellationToken = default)
    {
        if (supplierDto.CompanyId <= 0)
            throw new ArgumentException("CompanyId musi być większe od zera.", nameof(supplierDto));
        if (string.IsNullOrWhiteSpace(supplierDto.Name))
            throw new ArgumentException("Nazwa kontrahenta nie może być pusta.", nameof(supplierDto));

        var existing = await _repository.GetByNameAsync(supplierDto.Name, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"Kontrahent o nazwie '{supplierDto.Name}' już istnieje w tej firmie.");

        var supplier = new Supplier(
            supplierDto.CompanyId,
            supplierDto.Name,
            supplierDto.Phone ?? string.Empty,
            supplierDto.Currency ?? "PLN");

        if (!string.IsNullOrWhiteSpace(supplierDto.Email))
            supplier.UpdateContactInfo(supplierDto.Email, supplierDto.Phone ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(supplierDto.Notes))
            supplier.UpdateNotes(supplierDto.Notes);

        var newId = await _repository.AddAsync(supplier, cancellationToken);
        var created = await _repository.GetByIdAsync(newId, cancellationToken);
        if (created == null)
            throw new InvalidOperationException("Nie udało się utworzyć kontrahenta.");
        return MapToDto(created);
    }

    public async Task<SupplierDto> UpdateAsync(SupplierDto supplierDto, CancellationToken cancellationToken = default)
    {
        if (supplierDto.Id <= 0)
            throw new ArgumentException("Id musi być większe od zera.", nameof(supplierDto));
        if (supplierDto.CompanyId <= 0)
            throw new ArgumentException("CompanyId musi być większe od zera.", nameof(supplierDto));
        if (string.IsNullOrWhiteSpace(supplierDto.Name))
            throw new ArgumentException("Nazwa kontrahenta nie może być pusta.", nameof(supplierDto));

        var supplier = await _repository.GetByIdAsync(supplierDto.Id, cancellationToken);
        if (supplier == null)
            throw new ArgumentException($"Kontrahent o ID {supplierDto.Id} nie został znaleziony.", nameof(supplierDto));

        supplier.UpdateName(supplierDto.Name);
        supplier.UpdateContactInfo(supplierDto.Email, supplierDto.Phone ?? string.Empty);
        supplier.UpdateCurrency(supplierDto.Currency ?? "PLN");
        supplier.UpdateNotes(supplierDto.Notes);

        await _repository.UpdateAsync(supplier, cancellationToken);
        var updated = await _repository.GetByIdAsync(supplierDto.Id, cancellationToken);
        if (updated == null)
            throw new InvalidOperationException("Nie udało się zaktualizować kontrahenta.");
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id, int companyId, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("Id musi być większe od zera.", nameof(id));
        if (companyId <= 0)
            throw new ArgumentException("CompanyId musi być większe od zera.", nameof(companyId));

        if (!await _repository.ExistsAsync(id, cancellationToken))
            throw new ArgumentException($"Kontrahent o ID {id} nie został znaleziony.", nameof(id));

        await _repository.DeleteAsync(id, cancellationToken);
    }

    private static SupplierDto MapToDto(Supplier supplier)
    {
        return new SupplierDto
        {
            Id = supplier.Id,
            CompanyId = supplier.CompanyId,
            Name = supplier.Name,
            Currency = supplier.Currency ?? "PLN",
            Email = supplier.Email,
            Phone = supplier.Phone,
            Notes = supplier.Notes,
            CreatedAt = supplier.CreatedAt,
            UpdatedAt = supplier.UpdatedAt
        };
    }
}
