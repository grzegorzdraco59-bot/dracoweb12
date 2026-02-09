using ERP.Application.DTOs;
using ERP.Application.Validation;
using ERP.Application.Services;
using ERP.Domain.Entities;
using ERP.Domain.Repositories;

namespace ERP.Application.Services;

/// <summary>
/// Implementacja serwisu aplikacyjnego dla kontrahenta (Customer)
/// Zawiera logikę biznesową, walidacje i zarządzanie transakcjami
/// </summary>
public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly CustomerValidator _validator;
    private readonly CustomerDtoValidator _dtoValidator;

    public CustomerService(
        ICustomerRepository repository,
        CustomerValidator validator,
        CustomerDtoValidator dtoValidator)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _dtoValidator = dtoValidator ?? throw new ArgumentNullException(nameof(dtoValidator));
    }

    public async Task<CustomerDto?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("Id musi być większe od zera.", nameof(id));
        if (companyId <= 0)
            throw new ArgumentException("CompanyId musi być większe od zera.", nameof(companyId));

        var customer = await _repository.GetByIdAsync(id, companyId, cancellationToken);
        return customer != null ? MapToDto(customer) : null;
    }

    public async Task<CustomerDto?> GetByKontrahentIdAsync(int kontrahentId, int companyId, CancellationToken cancellationToken = default)
    {
        if (kontrahentId <= 0)
            throw new ArgumentException("KontrahentId musi być większe od zera.", nameof(kontrahentId));
        if (companyId <= 0)
            throw new ArgumentException("CompanyId musi być większe od zera.", nameof(companyId));

        var customer = await _repository.GetByKontrahentIdAsync(kontrahentId, companyId, cancellationToken);
        return customer != null ? MapToDto(customer) : null;
    }

    public async Task<IEnumerable<CustomerDto>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        if (companyId <= 0)
            throw new ArgumentException("CompanyId musi być większe od zera.", nameof(companyId));

        var customers = await _repository.GetByCompanyIdAsync(companyId, cancellationToken);
        return customers.Select(MapToDto);
    }

    public async Task<IEnumerable<CustomerDto>> GetActiveByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        if (companyId <= 0)
            throw new ArgumentException("CompanyId musi być większe od zera.", nameof(companyId));

        var customers = await _repository.GetActiveByCompanyIdAsync(companyId, cancellationToken);
        return customers.Select(MapToDto);
    }

    public async Task<CustomerDto> CreateAsync(CustomerDto customerDto, CancellationToken cancellationToken = default)
    {
        // Walidacja DTO
        var validationResult = _dtoValidator.Validate(customerDto);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Błędy walidacji: {string.Join(", ", validationResult.Errors)}");
        }

        // Walidacja CompanyId
        if (customerDto.CompanyId <= 0)
            throw new ArgumentException("CompanyId musi być większe od zera.", nameof(customerDto));

        // Sprawdzenie czy klient o tej nazwie już istnieje
        var existingCustomer = await _repository.GetByNameAsync(customerDto.Name, customerDto.CompanyId, cancellationToken);
        if (existingCustomer != null)
        {
            throw new InvalidOperationException($"Klient o nazwie '{customerDto.Name}' już istnieje w tej firmie.");
        }

        // Tworzenie encji
        var customer = new Customer(
            customerDto.CompanyId,
            customerDto.Name
        );

        // Aktualizacja dodatkowych pól
        if (!string.IsNullOrWhiteSpace(customerDto.FirstName) || !string.IsNullOrWhiteSpace(customerDto.Surname))
        {
            customer.UpdatePersonalInfo(customerDto.FirstName, customerDto.Surname);
        }

        if (!string.IsNullOrWhiteSpace(customerDto.Email1) || !string.IsNullOrWhiteSpace(customerDto.Phone1))
        {
            customer.UpdateContactInfo(customerDto.Email1, customerDto.Email2, customerDto.Phone1, customerDto.Phone2);
        }

        if (!string.IsNullOrWhiteSpace(customerDto.Street))
        {
            customer.UpdateAddress(customerDto.Street, customerDto.PostalCode, customerDto.City, customerDto.Country);
        }

        if (!string.IsNullOrWhiteSpace(customerDto.ShippingStreet))
        {
            customer.UpdateShippingAddress(customerDto.ShippingStreet, customerDto.ShippingPostalCode, customerDto.ShippingCity, customerDto.ShippingCountry);
        }

        if (!string.IsNullOrWhiteSpace(customerDto.Nip))
        {
            customer.UpdateCompanyInfo(customerDto.Nip, customerDto.Regon, customerDto.VatStatus);
        }

        if (!string.IsNullOrWhiteSpace(customerDto.Notes))
        {
            customer.UpdateNotes(customerDto.Notes);
        }

        if (!string.IsNullOrWhiteSpace(customerDto.Status))
        {
            customer.UpdateStatus(customerDto.Status);
        }

        if (!string.IsNullOrWhiteSpace(customerDto.Code))
        {
            customer.UpdateCode(customerDto.Code);
        }

        if (!string.IsNullOrWhiteSpace(customerDto.Currency))
        {
            customer.UpdateCurrency(customerDto.Currency);
        }

        if (!string.IsNullOrWhiteSpace(customerDto.FullAddress))
        {
            customer.UpdateFullAddress(customerDto.FullAddress);
        }

        if (customerDto.OfferEnabled.HasValue)
        {
            customer.SetOfferEnabled(customerDto.OfferEnabled.Value);
        }

        if (customerDto.CustomerType.HasValue)
        {
            customer.UpdateCustomerType(customerDto.CustomerType);
        }

        // Walidacja encji przed zapisem
        var entityValidationResult = _validator.Validate(customer);
        if (!entityValidationResult.IsValid)
        {
            throw new ArgumentException($"Błędy walidacji encji: {string.Join(", ", entityValidationResult.Errors)}");
        }

        // Zapis w transakcji (dla przyszłych rozszerzeń - jeśli będzie potrzeba wielu operacji)
        int id = await _repository.AddAsync(customer, cancellationToken);

        // Pobranie utworzonego klienta
        var createdCustomer = await _repository.GetByIdAsync(id, customerDto.CompanyId, cancellationToken);
        if (createdCustomer == null)
            throw new InvalidOperationException("Nie udało się utworzyć kontrahenta.");

        return MapToDto(createdCustomer);
    }

    public async Task<CustomerDto> UpdateAsync(CustomerDto customerDto, CancellationToken cancellationToken = default)
    {
        // Walidacja DTO
        var validationResult = _dtoValidator.Validate(customerDto);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Błędy walidacji: {string.Join(", ", validationResult.Errors)}");
        }

        if (customerDto.Id <= 0)
            throw new ArgumentException("Id musi być większe od zera.", nameof(customerDto));
        if (customerDto.CompanyId <= 0)
            throw new ArgumentException("CompanyId musi być większe od zera.", nameof(customerDto));

        // Pobranie istniejącego klienta
        var customer = await _repository.GetByIdAsync(customerDto.Id, customerDto.CompanyId, cancellationToken);
        if (customer == null)
            throw new ArgumentException($"Kontrahent o ID {customerDto.Id} nie został znaleziony.", nameof(customerDto));

        // Aktualizacja pól
        if (!string.IsNullOrWhiteSpace(customerDto.Name))
        {
            customer.ChangeName(customerDto.Name);
        }

        customer.UpdateContactInfo(customerDto.Email1, customerDto.Email2, customerDto.Phone1, customerDto.Phone2);
        customer.UpdateAddress(customerDto.Street, customerDto.PostalCode, customerDto.City, customerDto.Country);
        customer.UpdateShippingAddress(customerDto.ShippingStreet, customerDto.ShippingPostalCode, customerDto.ShippingCity, customerDto.ShippingCountry);
        customer.UpdatePersonalInfo(customerDto.FirstName, customerDto.Surname);
        customer.UpdateCompanyInfo(customerDto.Nip, customerDto.Regon, customerDto.VatStatus);
        customer.UpdateNotes(customerDto.Notes);
        customer.UpdateStatus(customerDto.Status);
        customer.UpdateCode(customerDto.Code);
        customer.UpdateCurrency(customerDto.Currency);
        customer.UpdateFullAddress(customerDto.FullAddress);

        if (customerDto.OfferEnabled.HasValue)
        {
            customer.SetOfferEnabled(customerDto.OfferEnabled.Value);
        }

        if (customerDto.CustomerType.HasValue)
        {
            customer.UpdateCustomerType(customerDto.CustomerType);
        }

        // Walidacja encji przed zapisem
        var entityValidationResult = _validator.Validate(customer);
        if (!entityValidationResult.IsValid)
        {
            throw new ArgumentException($"Błędy walidacji encji: {string.Join(", ", entityValidationResult.Errors)}");
        }

        // Aktualizacja (dla przyszłych rozszerzeń - jeśli będzie potrzeba wielu operacji, użyjemy transakcji)
        await _repository.UpdateAsync(customer, cancellationToken);

        // Pobranie zaktualizowanego klienta
        var updatedCustomer = await _repository.GetByIdAsync(customerDto.Id, customerDto.CompanyId, cancellationToken);
        if (updatedCustomer == null)
            throw new InvalidOperationException("Nie udało się zaktualizować kontrahenta.");

        return MapToDto(updatedCustomer);
    }

    public async Task DeleteAsync(int id, int companyId, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("Id musi być większe od zera.", nameof(id));
        if (companyId <= 0)
            throw new ArgumentException("CompanyId musi być większe od zera.", nameof(companyId));

        // Sprawdzenie czy klient istnieje
        if (!await _repository.ExistsAsync(id, companyId, cancellationToken))
            throw new ArgumentException($"Kontrahent o ID {id} nie został znaleziony.", nameof(id));

        // Usunięcie (dla przyszłych rozszerzeń - jeśli będzie potrzeba wielu operacji, użyjemy transakcji)
        await _repository.DeleteAsync(id, companyId, cancellationToken);
    }

    private static CustomerDto MapToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            CompanyId = customer.CompanyId,
            Name = customer.Name,
            Surname = customer.Surname,
            FirstName = customer.FirstName,
            Notes = customer.Notes,
            Phone1 = customer.Phone1,
            Phone2 = customer.Phone2,
            Nip = customer.Nip,
            Street = customer.Street,
            PostalCode = customer.PostalCode,
            City = customer.City,
            Country = customer.Country,
            ShippingStreet = customer.ShippingStreet,
            ShippingPostalCode = customer.ShippingPostalCode,
            ShippingCity = customer.ShippingCity,
            ShippingCountry = customer.ShippingCountry,
            Email1 = customer.Email1,
            Email2 = customer.Email2,
            Code = customer.Code,
            Status = customer.Status,
            Currency = customer.Currency,
            CustomerType = customer.CustomerType,
            OfferEnabled = customer.OfferEnabled,
            VatStatus = customer.VatStatus,
            Regon = customer.Regon,
            FullAddress = customer.FullAddress,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt
        };
    }
}
