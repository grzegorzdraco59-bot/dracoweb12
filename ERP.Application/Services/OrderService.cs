using ERP.Application.DTOs;
using ERP.Domain.Entities;
using ERP.Domain.Repositories;

namespace ERP.Application.Services;

/// <summary>
/// Implementacja serwisu aplikacyjnego dla zamówień
/// </summary>
public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<OrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await _repository.GetByIdAsync(id, cancellationToken);
        return order != null ? MapToDto(order) : null;
    }

    public async Task<IEnumerable<OrderDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _repository.GetAllAsync(cancellationToken);
        return orders.Select(MapToDto);
    }

    public async Task<int> AddAsync(OrderDto orderDto, CancellationToken cancellationToken = default)
    {
        var order = new Order(orderDto.CompanyId);
        
        // Konwersja OrderDate na int (format Clarion)
        if (orderDto.OrderDate.HasValue)
        {
            order.OrderDateInt = (int)(orderDto.OrderDate.Value - new DateTime(1800, 12, 28)).TotalDays;
            order.OrderDate = orderDto.OrderDate;
        }
        else if (orderDto.OrderDateInt.HasValue)
        {
            order.OrderDateInt = orderDto.OrderDateInt.Value;
            order.OrderDate = new DateTime(1800, 12, 28).AddDays(orderDto.OrderDateInt.Value);
        }
        
        order.OrderNumberInt = orderDto.OrderNumberInt;
        order.UpdateSupplier(orderDto.SupplierId, orderDto.SupplierName, orderDto.SupplierEmail, orderDto.SupplierCurrency);
        order.UpdateStatus(orderDto.Status);
        order.UpdateProductInfo(orderDto.ProductId, orderDto.ProductNameDraco, orderDto.ProductName, orderDto.ProductStatus);
        order.UpdatePricing(orderDto.PurchasePrice, orderDto.Quantity, orderDto.ConversionFactor, orderDto.QuantityInPackage, orderDto.PurchaseUnit, orderDto.SalesUnit);
        order.UpdateDeliveryInfo(orderDto.SentToOrder, orderDto.Delivered);
        order.UpdateVatAndOperator(orderDto.VatRate, orderDto.Operator, orderDto.ScannerOrderNumber);
        order.Notes = orderDto.Notes;

        return await _repository.AddAsync(order, cancellationToken);
    }

    public async Task UpdateAsync(OrderDto orderDto, CancellationToken cancellationToken = default)
    {
        var order = await _repository.GetByIdAsync(orderDto.Id, cancellationToken);
        if (order == null)
            throw new InvalidOperationException($"Zamówienie o ID {orderDto.Id} nie zostało znalezione.");

        // Konwersja OrderDate na int (format Clarion)
        if (orderDto.OrderDate.HasValue)
        {
            order.OrderDateInt = (int)(orderDto.OrderDate.Value - new DateTime(1800, 12, 28)).TotalDays;
        }
        else if (orderDto.OrderDateInt.HasValue)
        {
            order.OrderDateInt = orderDto.OrderDateInt.Value;
            order.OrderDate = new DateTime(1800, 12, 28).AddDays(orderDto.OrderDateInt.Value);
        }

        order.UpdateSupplier(orderDto.SupplierId, orderDto.SupplierName, orderDto.SupplierEmail, orderDto.SupplierCurrency);
        order.UpdateStatus(orderDto.Status);
        order.UpdateProductInfo(orderDto.ProductId, orderDto.ProductNameDraco, orderDto.ProductName, orderDto.ProductStatus);
        order.UpdatePricing(orderDto.PurchasePrice, orderDto.Quantity, orderDto.ConversionFactor, orderDto.QuantityInPackage, orderDto.PurchaseUnit, orderDto.SalesUnit);
        order.UpdateDeliveryInfo(orderDto.SentToOrder, orderDto.Delivered);
        order.UpdateVatAndOperator(orderDto.VatRate, orderDto.Operator, orderDto.ScannerOrderNumber);
        order.OrderNumberInt = orderDto.OrderNumberInt;
        order.Notes = orderDto.Notes;

        await _repository.UpdateAsync(order, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(id, cancellationToken);
    }

    private static OrderDto MapToDto(Order order)
    {
        // Konwersja OrderDate na int (format Clarion)
        int? orderDateInt = null;
        if (order.OrderDate.HasValue)
        {
            orderDateInt = (int)(order.OrderDate.Value - new DateTime(1800, 12, 28)).TotalDays;
        }
        else if (order.OrderDateInt.HasValue)
        {
            orderDateInt = order.OrderDateInt.Value;
        }
        
        return new OrderDto
        {
            Id = order.Id,
            CompanyId = order.CompanyId,
            OrderNumberInt = order.OrderNumberInt,
            OrderDateInt = orderDateInt,
            OrderDate = order.OrderDate,
            SupplierId = order.SupplierId,
            SupplierName = order.SupplierName,
            SupplierEmail = order.SupplierEmail,
            SupplierCurrency = order.SupplierCurrency,
            ProductId = order.ProductId,
            ProductNameDraco = order.ProductNameDraco,
            ProductName = order.ProductName,
            ProductStatus = order.ProductStatus,
            PurchaseUnit = order.PurchaseUnit,
            SalesUnit = order.SalesUnit,
            PurchasePrice = order.PurchasePrice,
            ConversionFactor = order.ConversionFactor,
            Quantity = order.Quantity,
            Notes = order.Notes,
            Status = order.Status,
            SentToOrder = order.SentToOrder,
            Delivered = order.Delivered,
            QuantityInPackage = order.QuantityInPackage,
            VatRate = order.VatRate,
            Operator = order.Operator,
            ScannerOrderNumber = order.ScannerOrderNumber,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }
}
