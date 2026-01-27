using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ERP.UI.Web.Services;

namespace ERP.UI.Web.Attributes;

/// <summary>
/// Atrybut wymagający wybranej firmy w Claims.
/// Jeśli firma nie jest wybrana, przekierowuje do Company/Select
/// </summary>
public class RequireCompanyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var userContext = context.HttpContext.RequestServices.GetRequiredService<IUserContext>();
        var companyId = userContext.CompanyId;
        
        if (!companyId.HasValue)
        {
            // Brak wybranej firmy - przekieruj do wyboru firmy
            context.Result = new RedirectToActionResult("Select", "Company", null);
            return;
        }
        
        base.OnActionExecuting(context);
    }
}
