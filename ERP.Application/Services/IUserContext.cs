namespace ERP.Application.Services;

/// <summary>
/// Wspólny kontrakt na kontekst użytkownika (UserId, CompanyId).
/// Używany m.in. przez repozytoria infrastruktury (OrderRepository itd.).
/// Web i WPF dostarczają swoje implementacje przez DI.
/// </summary>
public interface IUserContext
{
    int? UserId { get; }
    int? CompanyId { get; }
}
