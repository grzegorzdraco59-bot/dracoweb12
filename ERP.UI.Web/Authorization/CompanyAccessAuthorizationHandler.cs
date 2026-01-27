using Microsoft.AspNetCore.Authorization;
using ERP.Domain.Repositories;
using ERP.UI.Web.Services;

namespace ERP.UI.Web.Authorization;

/// <summary>
/// Wymaganie autoryzacji: użytkownik musi mieć dostęp do wybranej firmy
/// </summary>
public class CompanyAccessRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Handler sprawdzający czy użytkownik ma dostęp do wybranej firmy
/// </summary>
public class CompanyAccessAuthorizationHandler : AuthorizationHandler<CompanyAccessRequirement>
{
    private readonly IUserCompanyRepository _userCompanyRepository;
    private readonly IUserContext _userContext;

    public CompanyAccessAuthorizationHandler(
        IUserCompanyRepository userCompanyRepository,
        IUserContext userContext)
    {
        _userCompanyRepository = userCompanyRepository ?? throw new ArgumentNullException(nameof(userCompanyRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CompanyAccessRequirement requirement)
    {
        var userId = _userContext.UserId;
        var companyId = _userContext.CompanyId;

        if (!userId.HasValue || !companyId.HasValue)
        {
            context.Fail();
            return;
        }

        // Sprawdzamy czy użytkownik ma dostęp do wybranej firmy
        var userCompany = await _userCompanyRepository.GetByUserAndCompanyAsync(
            userId.Value, 
            companyId.Value, 
            CancellationToken.None);

        if (userCompany != null)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
