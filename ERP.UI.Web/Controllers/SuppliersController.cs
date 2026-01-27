using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ERP.Domain.Repositories;
using ERP.Application.DTOs;
using ERP.UI.Web.Services;

namespace ERP.UI.Web.Controllers;

/// <summary>
/// Kontroler 8. Dostawcy (Browse)
/// </summary>
[Authorize(Policy = "Suppliers:Read")]
public class SuppliersController : BaseController
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUserContext _userContext;

    public SuppliersController(ISupplierRepository supplierRepository, IUserContext userContext)
    {
        _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var companyId = _userContext.CompanyId;
        if (!companyId.HasValue)
            return RedirectToAction("Select", "Company");

        var suppliers = await _supplierRepository.GetAllAsync(cancellationToken);
        var dtos = suppliers.Select(MapToDto).ToList();
        return View(dtos);
    }

    private static SupplierDto MapToDto(ERP.Domain.Entities.Supplier s)
    {
        return new SupplierDto
        {
            Id = s.Id,
            CompanyId = s.CompanyId,
            Name = s.Name,
            Currency = s.Currency ?? "PLN",
            Email = s.Email,
            Phone = s.Phone,
            Notes = s.Notes,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
        };
    }
}
