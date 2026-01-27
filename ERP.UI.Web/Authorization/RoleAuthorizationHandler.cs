using Microsoft.AspNetCore.Authorization;
using ERP.UI.Web.Services;

namespace ERP.UI.Web.Authorization;

/// <summary>
/// Wymaganie autoryzacji: użytkownik musi mieć określoną rolę
/// </summary>
public class RoleRequirement : IAuthorizationRequirement
{
    public int? RequiredRoleId { get; }

    public RoleRequirement(int? requiredRoleId = null)
    {
        RequiredRoleId = requiredRoleId;
    }
}

/// <summary>
/// Handler sprawdzający czy użytkownik ma wymaganą rolę
/// </summary>
public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
{
    private readonly IUserContext _userContext;

    public RoleAuthorizationHandler(IUserContext userContext)
    {
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        var userRoleId = _userContext.RoleId;

        // Jeśli nie wymagamy konkretnej roli, sprawdzamy tylko czy użytkownik ma jakąkolwiek rolę
        if (!requirement.RequiredRoleId.HasValue)
        {
            if (userRoleId.HasValue)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
            return Task.CompletedTask;
        }

        // Sprawdzamy czy użytkownik ma wymaganą rolę
        if (userRoleId.HasValue && userRoleId.Value == requirement.RequiredRoleId.Value)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}
