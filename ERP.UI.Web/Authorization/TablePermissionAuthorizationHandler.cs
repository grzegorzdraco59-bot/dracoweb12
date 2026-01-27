using Microsoft.AspNetCore.Authorization;
using ERP.Application.Services;
using ERP.UI.Web.Services;

namespace ERP.UI.Web.Authorization;

/// <summary>
/// Wymaganie autoryzacji: użytkownik musi mieć uprawnienie do tabeli
/// </summary>
public class TablePermissionRequirement : IAuthorizationRequirement
{
    public string TableName { get; }
    public string PermissionType { get; } // SELECT, INSERT, UPDATE, DELETE

    public TablePermissionRequirement(string tableName, string permissionType)
    {
        TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
        PermissionType = permissionType ?? throw new ArgumentNullException(nameof(permissionType));
    }
}

/// <summary>
/// Handler sprawdzający czy użytkownik ma uprawnienie do tabeli
/// </summary>
public class TablePermissionAuthorizationHandler : AuthorizationHandler<TablePermissionRequirement>
{
    private readonly IOperatorPermissionService _permissionService;
    private readonly IUserContext _userContext;

    public TablePermissionAuthorizationHandler(IOperatorPermissionService permissionService, IUserContext userContext)
    {
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TablePermissionRequirement requirement)
    {
        var userId = _userContext.UserId;

        if (!userId.HasValue)
        {
            context.Fail();
            return;
        }

        // Sprawdzamy czy użytkownik ma wymagane uprawnienie
        var hasPermission = await _permissionService.HasPermissionAsync(
            userId.Value,
            requirement.TableName,
            requirement.PermissionType,
            CancellationToken.None);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
