using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ERP.UI.Web.Services;

namespace ERP.UI.Web.Controllers;

/// <summary>
/// Bazowy kontroler z ochroną autentykacji i wybranej firmy
/// 
/// Dlaczego BaseController?
/// - Najprostsze rozwiązanie - dziedziczenie, łatwe do zrozumienia
/// - Centralna logika - jedna klasa, łatwa do utrzymania
/// - Elastyczne - można łatwo dodać więcej kontrolerów chronionych
/// - Działa automatycznie - nie trzeba pamiętać o atrybutach
/// </summary>
public class BaseController : Controller
{
    private IUserContext? _userContext;

    protected IUserContext UserContext
    {
        get
        {
            if (_userContext == null)
            {
                _userContext = HttpContext.RequestServices.GetRequiredService<IUserContext>();
            }
            return _userContext;
        }
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Sprawdzenie 1: Użytkownik musi być zalogowany
        if (!UserContext.IsAuthenticated)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        // Sprawdzenie 2: Użytkownik musi mieć wybraną firmę
        if (!UserContext.HasCompanySelected)
        {
            context.Result = new RedirectToActionResult("Select", "Company", null);
            return;
        }

        base.OnActionExecuting(context);
    }
}
