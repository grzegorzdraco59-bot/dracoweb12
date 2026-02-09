using ERP.Domain.Entities;
using ERP.Domain.Enums;
using ERP.Domain.Exceptions;
using ERP.Domain.Repositories;

namespace ERP.Application.Services;

/// <summary>
/// Serwis ofert – walidacja statusu przed edycją/usuwanieniem (FAZA4: tylko Draft).
/// Zgodne z docs/FAZA4_STANY_DOKUMENTOW.md.
/// </summary>
public class OfferService : IOfferService
{
    private readonly IOfferRepository _offerRepository;
    private readonly IOfferPositionRepository _positionRepository;
    private readonly IOfferTotalsService _offerTotalsService;

    public OfferService(IOfferRepository offerRepository, IOfferPositionRepository positionRepository, IOfferTotalsService offerTotalsService)
    {
        _offerRepository = offerRepository ?? throw new ArgumentNullException(nameof(offerRepository));
        _positionRepository = positionRepository ?? throw new ArgumentNullException(nameof(positionRepository));
        _offerTotalsService = offerTotalsService ?? throw new ArgumentNullException(nameof(offerTotalsService));
    }

    public Task<Offer?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken = default)
        => _offerRepository.GetByIdAsync(id, companyId, cancellationToken);

    public Task<IEnumerable<Offer>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
        => _offerRepository.GetByCompanyIdAsync(companyId, cancellationToken);

    public Task<IEnumerable<Offer>> SearchByCompanyIdAsync(int companyId, string? searchText, int limit = 200, CancellationToken cancellationToken = default)
    {
        return _offerRepository.SearchByCompanyIdAsync(companyId, searchText, limit, cancellationToken);
    }

    public Task<int?> GetNextOfferNumberForDateAsync(int offerDate, int companyId, CancellationToken cancellationToken = default)
        => _offerRepository.GetNextOfferNumberForDateAsync(offerDate, companyId, cancellationToken);

    public Task<int> AddAsync(Offer offer, CancellationToken cancellationToken = default)
        => _offerRepository.AddAsync(offer, cancellationToken);

    public async Task UpdateAsync(Offer offer, CancellationToken cancellationToken = default)
    {
        EnsureOfferEditable(offer);
        await _offerRepository.UpdateAsync(offer, cancellationToken);
    }

    public async Task DeleteAsync(int id, int companyId, CancellationToken cancellationToken = default)
    {
        var offer = await _offerRepository.GetByIdAsync(id, companyId, cancellationToken).ConfigureAwait(false);
        if (offer == null)
            return;
        EnsureOfferEditable(offer);
        await _positionRepository.DeleteByOfferIdAsync(id, cancellationToken).ConfigureAwait(false);
        await _offerRepository.DeleteAsync(id, companyId, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetStatusAsync(int offerId, int companyId, OfferStatus newStatus, CancellationToken cancellationToken = default)
    {
        var offer = await _offerRepository.GetByIdAsync(offerId, companyId, cancellationToken).ConfigureAwait(false);
        if (offer == null)
            throw new InvalidOperationException($"Oferta o ID {offerId} nie została znaleziona.");
        if (offer.Status == newStatus)
            return;
        if (!OfferStatusMapping.IsTransitionAllowed(offer.Status, newStatus))
            throw new BusinessRuleException($"Przejście z {offer.Status} do {newStatus} nie jest dozwolone.");
        await _offerRepository.SetStatusAsync(offerId, companyId, newStatus, cancellationToken).ConfigureAwait(false);
    }

    public Task SetFlagsAsync(int offerId, int companyId, bool? forProforma, bool? forOrder, bool forInvoice, CancellationToken cancellationToken = default)
        => _offerRepository.SetFlagsAsync(offerId, companyId, forProforma, forOrder, forInvoice, cancellationToken);

    public Task<IEnumerable<OfferPosition>> GetPositionsByOfferIdAsync(int offerId, CancellationToken cancellationToken = default)
        => _positionRepository.GetByOfferIdAsync(offerId, cancellationToken);

    public Task<OfferPosition?> GetPositionByIdAsync(int positionId, CancellationToken cancellationToken = default)
        => _positionRepository.GetByIdAsync(positionId, cancellationToken);

    public async Task<int> AddPositionAsync(OfferPosition position, CancellationToken cancellationToken = default)
    {
        await EnsureOfferDraftForPositionAsync(position.OfferId, position.CompanyId, cancellationToken).ConfigureAwait(false);
        var id = await _positionRepository.AddAsync(position, cancellationToken).ConfigureAwait(false);
        await _offerTotalsService.RecalculateOfferLinesAsync(position.OfferId, cancellationToken).ConfigureAwait(false);
        await _offerTotalsService.RecalcOfferTotalsAsync(position.OfferId, cancellationToken).ConfigureAwait(false);
        return id;
    }

    public async Task UpdatePositionAsync(OfferPosition position, CancellationToken cancellationToken = default)
    {
        await EnsureOfferDraftForPositionAsync(position.OfferId, position.CompanyId, cancellationToken).ConfigureAwait(false);
        await _positionRepository.UpdateAsync(position, cancellationToken).ConfigureAwait(false);
        await _offerTotalsService.RecalculateOfferLinesAsync(position.OfferId, cancellationToken).ConfigureAwait(false);
        await _offerTotalsService.RecalcOfferTotalsAsync(position.OfferId, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeletePositionAsync(int positionId, CancellationToken cancellationToken = default)
    {
        var position = await _positionRepository.GetByIdAsync(positionId, cancellationToken).ConfigureAwait(false);
        if (position == null)
            return;
        await EnsureOfferDraftForPositionAsync(position.OfferId, position.CompanyId, cancellationToken).ConfigureAwait(false);
        var offerId = position.OfferId;
        await _positionRepository.DeleteAsync(positionId, cancellationToken).ConfigureAwait(false);
        await _offerTotalsService.RecalculateOfferLinesAsync(offerId, cancellationToken).ConfigureAwait(false);
        await _offerTotalsService.RecalcOfferTotalsAsync(offerId, cancellationToken).ConfigureAwait(false);
    }

    private static void EnsureOfferEditable(Offer offer)
    {
        if (offer.Status != OfferStatus.Draft)
            throw new BusinessRuleException($"Dokument w statusie {offer.Status} nie może być edytowany.");
    }

    private async Task EnsureOfferDraftForPositionAsync(int offerId, int companyId, CancellationToken cancellationToken)
    {
        var offer = await _offerRepository.GetByIdAsync(offerId, companyId, cancellationToken).ConfigureAwait(false);
        if (offer == null)
            throw new InvalidOperationException($"Oferta o ID {offerId} nie została znaleziona.");
        EnsureOfferEditable(offer);
    }
}
