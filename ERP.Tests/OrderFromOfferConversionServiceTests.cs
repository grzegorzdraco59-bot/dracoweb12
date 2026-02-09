using ERP.Application.Repositories;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using ERP.Domain.Exceptions;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Services;
using Moq;
using MySqlConnector;

namespace ERP.Tests;

/// <summary>
/// Minimalne testy konwersji oferty na zamówienie (FAZA 4 / KROK 7):
/// - tylko oferta Accepted może być konwertowana (BusinessRuleException);
/// - idempotencja: już skonwertowana oferta zwraca istniejące ID zamówienia;
/// - atomowość: wyjątek wewnątrz transakcji jest propagowany (rollback).
/// </summary>
public class OrderFromOfferConversionServiceTests
{
    private static Offer CreateOffer(int id, int companyId, OfferStatus status)
    {
        var offer = new Offer(companyId, "operator");
        var idProp = typeof(BaseEntity).GetProperty("Id", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        idProp?.SetValue(offer, id);
        offer.UpdateStatus(status);
        return offer;
    }

    [Fact]
    public async Task CreateFromOfferAsync_WhenOfferNotAccepted_ThrowsBusinessRuleException()
    {
        const int offerId = 1;
        const int companyId = 10;
        var offerDraft = CreateOffer(offerId, companyId, OfferStatus.Draft);

        var offerRepo = new Mock<IOfferRepository>();
        offerRepo.Setup(r => r.GetByIdAsync(offerId, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(offerDraft);

        var orderPositionRepo = new Mock<IOrderPositionMainRepository>();
        orderPositionRepo.Setup(r => r.GetOrderIdLinkedToOfferAsync(offerId, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var uow = new Mock<IUnitOfWork>();
        var idGenerator = new Mock<IIdGenerator>();
        var offerPositionRepo = new Mock<IOfferPositionRepository>();

        var sut = new OrderFromOfferConversionService(
            uow.Object, idGenerator.Object, offerRepo.Object, offerPositionRepo.Object, orderPositionRepo.Object);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            sut.CreateFromOfferAsync(offerId, companyId));

        Assert.Contains("Accepted", ex.Message);
        Assert.Contains("Draft", ex.Message);
        uow.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<MySqlTransaction, Task<int>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateFromOfferAsync_WhenOfferAlreadyConverted_ReturnsExistingOrderId()
    {
        const int offerId = 2;
        const int companyId = 10;
        const int existingOrderId = 42;
        var offerAccepted = CreateOffer(offerId, companyId, OfferStatus.Accepted);

        var offerRepo = new Mock<IOfferRepository>();
        offerRepo.Setup(r => r.GetByIdAsync(offerId, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(offerAccepted);

        var orderPositionRepo = new Mock<IOrderPositionMainRepository>();
        orderPositionRepo.Setup(r => r.GetOrderIdLinkedToOfferAsync(offerId, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrderId);

        var uow = new Mock<IUnitOfWork>();
        var idGenerator = new Mock<IIdGenerator>();
        var offerPositionRepo = new Mock<IOfferPositionRepository>();

        var sut = new OrderFromOfferConversionService(
            uow.Object, idGenerator.Object, offerRepo.Object, offerPositionRepo.Object, orderPositionRepo.Object);

        var result = await sut.CreateFromOfferAsync(offerId, companyId);

        Assert.Equal(existingOrderId, result);
        uow.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<MySqlTransaction, Task<int>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateFromOfferAsync_WhenTransactionThrows_PropagatesException()
    {
        const int offerId = 3;
        const int companyId = 10;
        var offerAccepted = CreateOffer(offerId, companyId, OfferStatus.Accepted);

        var offerRepo = new Mock<IOfferRepository>();
        offerRepo.Setup(r => r.GetByIdAsync(offerId, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(offerAccepted);

        var orderPositionRepo = new Mock<IOrderPositionMainRepository>();
        orderPositionRepo.Setup(r => r.GetOrderIdLinkedToOfferAsync(offerId, companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var offerPositionRepo = new Mock<IOfferPositionRepository>();
        offerPositionRepo.Setup(r => r.GetByOfferIdAsync(offerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<OfferPosition>());

        var uow = new Mock<IUnitOfWork>();
        var idGenerator = new Mock<IIdGenerator>();
        uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<MySqlTransaction, Task<int>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated DB failure"));

        var sut = new OrderFromOfferConversionService(
            uow.Object, idGenerator.Object, offerRepo.Object, offerPositionRepo.Object, orderPositionRepo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateFromOfferAsync(offerId, companyId));
    }
}
