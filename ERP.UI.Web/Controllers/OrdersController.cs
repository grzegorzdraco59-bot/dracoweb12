using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.Application.Services;
using ERP.Domain.Enums;
using ERP.UI.Web.Services;
using IUserContext = ERP.UI.Web.Services.IUserContext;

namespace ERP.UI.Web.Controllers;

/// <summary>
/// Kontroler 11. Zamówienia (Browse zamowienia)
/// </summary>
[Authorize(Policy = "Orders:Read")]
public class OrdersController : BaseController
{
    private readonly IOrderMainRepository _orderMainRepository;
    private readonly IOrderPositionMainRepository _orderPositionMainRepository;
    private readonly IOrderMainService _orderMainService;
    private readonly IUserContext _userContext;

    public OrdersController(IOrderMainRepository orderMainRepository, IOrderPositionMainRepository orderPositionMainRepository, IOrderMainService orderMainService, IUserContext userContext)
    {
        _orderMainRepository = orderMainRepository ?? throw new ArgumentNullException(nameof(orderMainRepository));
        _orderPositionMainRepository = orderPositionMainRepository ?? throw new ArgumentNullException(nameof(orderPositionMainRepository));
        _orderMainService = orderMainService ?? throw new ArgumentNullException(nameof(orderMainService));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var companyId = _userContext.CompanyId;
        if (!companyId.HasValue)
            return RedirectToAction("Select", "Company");

        var orders = await _orderMainRepository.GetByCompanyIdAsync(companyId.Value, cancellationToken);
        return View(orders);
    }

    /// <summary>
    /// Pobiera pozycje zamówienia (API endpoint dla AJAX) - podobnie jak OffersController.GetPositions
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPositions(int orderId, CancellationToken cancellationToken = default)
    {
        var companyId = _userContext.CompanyId;
        if (!companyId.HasValue)
        {
            return Json(new { error = "Brak wybranej firmy" });
        }

        try
        {
            var positions = await _orderPositionMainRepository.GetByOrderIdAsync(orderId, cancellationToken);
            return Json(positions);
        }
        catch (Exception ex)
        {
            return Json(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Zmiana statusu zamówienia (FAZA4 – test: Draft→Confirmed).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int orderId, string newStatus, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<OrderStatus>(newStatus, true, out var status))
            return RedirectToAction(nameof(Index), new { error = "Nieprawidłowy status" });

        try
        {
            await _orderMainService.SetStatusAsync(orderId, status, cancellationToken);
            return RedirectToAction(nameof(Index), new { message = $"Status zmieniony na {status}" });
        }
        catch (ERP.Domain.Exceptions.BusinessRuleException ex)
        {
            return RedirectToAction(nameof(Index), new { error = ex.Message });
        }
    }
}
