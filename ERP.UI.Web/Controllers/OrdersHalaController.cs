using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ERP.Domain.Repositories;
using ERP.Application.DTOs;
using ERP.UI.Web.Services;

namespace ERP.UI.Web.Controllers;

/// <summary>
/// Kontroler 13. zamowienia hala (Browse zamowieniahala)
/// </summary>
[Authorize(Policy = "Orders:Read")]
public class OrdersHalaController : BaseController
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserContext _userContext;

    public OrdersHalaController(IOrderRepository orderRepository, IUserContext userContext)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var companyId = _userContext.CompanyId;
        if (!companyId.HasValue)
            return RedirectToAction("Select", "Company");

        var orders = await _orderRepository.GetByCompanyIdAsync(companyId.Value, cancellationToken);
        var dtos = orders.Select(MapToDto).ToList();
        return View(dtos);
    }

    private static OrderDto MapToDto(ERP.Domain.Entities.Order o)
    {
        return new OrderDto
        {
            Id = o.Id,
            CompanyId = o.CompanyId,
            OrderNumberInt = o.OrderNumberInt,
            OrderDateInt = o.OrderDateInt,
            OrderDate = o.OrderDate,
            SupplierId = o.SupplierId,
            SupplierName = o.SupplierName,
            SupplierEmail = o.SupplierEmail,
            SupplierCurrency = o.SupplierCurrency,
            ProductId = o.ProductId,
            ProductNameDraco = o.ProductNameDraco,
            ProductName = o.ProductName,
            ProductStatus = o.ProductStatus,
            PurchaseUnit = o.PurchaseUnit,
            SalesUnit = o.SalesUnit,
            PurchasePrice = o.PurchasePrice,
            ConversionFactor = o.ConversionFactor,
            Quantity = o.Quantity,
            Notes = o.Notes,
            Status = o.Status,
            SentToOrder = o.SentToOrder,
            Delivered = o.Delivered,
            QuantityInPackage = o.QuantityInPackage,
            VatRate = o.VatRate,
            Operator = o.Operator,
            ScannerOrderNumber = o.ScannerOrderNumber,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt,
        };
    }
}
