using ERP.Application.DTOs;
using ERP.Domain.Entities;

namespace ERP.Application.Validation;

/// <summary>
/// Walidator dla Customer
/// </summary>
public class CustomerValidator : BaseValidator<Customer>
{
    public override ValidationResult Validate(Customer customer)
    {
        var result = new ValidationResult();

        // Walidacja nazwy
        IsNotEmpty(customer.Name, "Nazwa", result);
        HasValidLength(customer.Name, 200, "Nazwa", result, minLength: 1);

        // Walidacja emaili
        if (!string.IsNullOrWhiteSpace(customer.Email1))
            IsValidEmail(customer.Email1, "Email 1", result);

        if (!string.IsNullOrWhiteSpace(customer.Email2))
            IsValidEmail(customer.Email2, "Email 2", result);

        // Walidacja NIP
        if (!string.IsNullOrWhiteSpace(customer.Nip))
            IsValidNip(customer.Nip, "NIP", result);

        // Walidacja REGON
        if (!string.IsNullOrWhiteSpace(customer.Regon))
        {
            var cleanRegon = customer.Regon.Replace(" ", "").Replace("-", "");
            if (cleanRegon.Length != 9 && cleanRegon.Length != 14)
            {
                result.AddError("REGON musi składać się z 9 lub 14 cyfr.");
            }
        }

        // Walidacja CompanyId
        IsGreaterThanZero(customer.CompanyId, "CompanyId", result);

        return result;
    }
}

/// <summary>
/// Walidator dla CustomerDto
/// </summary>
public class CustomerDtoValidator : BaseValidator<CustomerDto>
{
    public override ValidationResult Validate(CustomerDto dto)
    {
        var result = new ValidationResult();

        // Walidacja nazwy
        IsNotEmpty(dto.Name, "Nazwa", result);
        HasValidLength(dto.Name, 200, "Nazwa", result, minLength: 1);

        // Walidacja emaili
        if (!string.IsNullOrWhiteSpace(dto.Email1))
            IsValidEmail(dto.Email1, "Email 1", result);

        if (!string.IsNullOrWhiteSpace(dto.Email2))
            IsValidEmail(dto.Email2, "Email 2", result);

        // Walidacja NIP
        if (!string.IsNullOrWhiteSpace(dto.Nip))
            IsValidNip(dto.Nip, "NIP", result);

        // Walidacja CompanyId
        if (dto.CompanyId > 0)
            IsGreaterThanZero(dto.CompanyId, "CompanyId", result);

        return result;
    }
}
