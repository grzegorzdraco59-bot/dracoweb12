using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ERP.Infrastructure.Repositories;
using ERP.Application.DTOs;
using ERP.UI.Web.Services;

namespace ERP.UI.Web.Controllers;

/// <summary>
/// Kontroler 10. Towary (Browse)
/// </summary>
[Authorize(Policy = "Products:Read")]
public class ProductsController : BaseController
{
    private readonly ProductRepository _productRepository;
    private readonly IUserContext _userContext;

    public ProductsController(ProductRepository productRepository, IUserContext userContext)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var companyId = _userContext.CompanyId;
        if (!companyId.HasValue)
            return RedirectToAction("Select", "Company");

        var products = await _productRepository.GetAllAsync(cancellationToken);
        return View(products);
    }
}
