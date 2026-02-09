using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.Domain.Enums;
using ERP.Domain.Exceptions;

namespace ERP.Application.Services;

/// <summary>
/// Serwis zamówień głównych (nagłówek zamowienia + pozycje) – walidacja statusu (FAZA4: tylko Draft).
/// Zgodne z docs/FAZA4_STANY_DOKUMENTOW.md.
/// </summary>
public class OrderMainService : IOrderMainService
{
    private readonly IOrderMainRepository _orderRepository;
    private readonly IOrderPositionMainRepository _positionRepository;
    private readonly IUserContext _userContext;
    private readonly IOrderFromOfferConversionService _conversionService;

    public OrderMainService(IOrderMainRepository orderRepository, IOrderPositionMainRepository positionRepository, IUserContext userContext, IOrderFromOfferConversionService conversionService)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _positionRepository = positionRepository ?? throw new ArgumentNullException(nameof(positionRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _conversionService = conversionService ?? throw new ArgumentNullException(nameof(conversionService));
    }

    public Task<OrderMainDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _orderRepository.GetByIdAsync(id, cancellationToken);

    public Task<IEnumerable<OrderMainDto>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
        => _orderRepository.GetByCompanyIdAsync(companyId, cancellationToken);

    public async Task<IEnumerable<OrderMainDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var companyId = _userContext.CompanyId ?? throw new InvalidOperationException("Brak wybranej firmy.");
        return await _orderRepository.GetByCompanyIdAsync(companyId, cancellationToken).ConfigureAwait(false);
    }

    public Task<int> AddAsync(OrderMainDto order, CancellationToken cancellationToken = default)
        => _orderRepository.AddAsync(order, cancellationToken);

    public async Task UpdateAsync(OrderMainDto order, CancellationToken cancellationToken = default)
    {
        EnsureOrderEditable(order);
        await _orderRepository.UpdateAsync(order, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (order == null)
            return;
        EnsureOrderEditable(order);
        var positions = await _positionRepository.GetByOrderIdAsync(id, cancellationToken).ConfigureAwait(false);
        foreach (var pos in positions)
        {
            await _positionRepository.DeleteAsync(pos.Id, cancellationToken).ConfigureAwait(false);
        }
        await _orderRepository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetStatusAsync(int orderId, OrderStatus newStatus, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order == null)
            throw new InvalidOperationException($"Zamówienie o ID {orderId} nie zostało znalezione.");
        var current = OrderStatusMapping.FromDb(order.Status);
        if (!OrderStatusMapping.IsTransitionAllowed(current, newStatus))
            throw new BusinessRuleException($"Przejście z {current} do {newStatus} nie jest dozwolone.");
        await _orderRepository.SetStatusAsync(orderId, newStatus, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CreateFromOfferAsync(int offerId, CancellationToken cancellationToken = default)
    {
        var companyId = _userContext.CompanyId ?? throw new InvalidOperationException("Brak wybranej firmy.");
        return await _conversionService.CreateFromOfferAsync(offerId, companyId, cancellationToken).ConfigureAwait(false);
    }

    public Task<IEnumerable<OrderPositionMainDto>> GetPositionsByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
        => _positionRepository.GetByOrderIdAsync(orderId, cancellationToken);

    public Task<OrderPositionMainDto?> GetPositionByIdAsync(int positionId, CancellationToken cancellationToken = default)
        => _positionRepository.GetByIdAsync(positionId, cancellationToken);

    public async Task<int> AddPositionAsync(OrderPositionMainDto position, CancellationToken cancellationToken = default)
    {
        await EnsureOrderDraftForPositionAsync(position.OrderId, cancellationToken).ConfigureAwait(false);
        var newId = await _positionRepository.AddAsync(position, cancellationToken).ConfigureAwait(false);
        await _orderRepository.RecalculateOrderTotalAsync(position.OrderId, cancellationToken).ConfigureAwait(false);
        return newId;
    }

    public async Task UpdatePositionAsync(OrderPositionMainDto position, CancellationToken cancellationToken = default)
    {
        await EnsureOrderDraftForPositionAsync(position.OrderId, cancellationToken).ConfigureAwait(false);
        await _positionRepository.UpdateAsync(position, cancellationToken).ConfigureAwait(false);
        await _orderRepository.RecalculateOrderTotalAsync(position.OrderId, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeletePositionAsync(int positionId, CancellationToken cancellationToken = default)
    {
        var position = await _positionRepository.GetByIdAsync(positionId, cancellationToken).ConfigureAwait(false);
        if (position == null)
            return;
        await EnsureOrderDraftForPositionAsync(position.OrderId, cancellationToken).ConfigureAwait(false);
        await _positionRepository.DeleteAsync(positionId, cancellationToken).ConfigureAwait(false);
        await _orderRepository.RecalculateOrderTotalAsync(position.OrderId, cancellationToken).ConfigureAwait(false);
    }

    private static void EnsureOrderEditable(OrderMainDto order)
    {
        var status = OrderStatusMapping.FromDb(order.Status);
        if (status != OrderStatus.Draft)
            throw new BusinessRuleException($"Dokument w statusie {status} nie może być edytowany.");
    }

    private async Task EnsureOrderDraftForPositionAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order == null)
            throw new InvalidOperationException($"Zamówienie o ID {orderId} nie zostało znalezione.");
        EnsureOrderEditable(order);
    }
}
