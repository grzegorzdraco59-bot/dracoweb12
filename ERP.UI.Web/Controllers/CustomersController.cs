using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ERP.Application.Services;
using ERP.Application.DTOs;
using ERP.UI.Web.Services;
using IUserContext = ERP.UI.Web.Services.IUserContext;

namespace ERP.UI.Web.Controllers;

/// <summary>
/// Kontroler 9. Odbiorcy (Browse)
/// </summary>
[Authorize(Policy = "Customers:Read")]
public class CustomersController : BaseController
{
    private readonly ICustomerService _customerService;
    private readonly IUserContext _userContext;

    public CustomersController(ICustomerService customerService, IUserContext userContext)
    {
        _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var companyId = _userContext.CompanyId;
        if (!companyId.HasValue)
            return RedirectToAction("Select", "Company");

        var customers = await _customerService.GetByCompanyIdAsync(companyId.Value, cancellationToken);
        return View(customers.ToList());
    }
}
