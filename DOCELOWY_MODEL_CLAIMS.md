# Docelowy Model Claims dla ERP System

## 1. Przegląd

Model Claims definiuje strukturę danych o użytkowniku przechowywanych w `HttpContext.User` (ClaimsPrincipal) po autentykacji. Claims są używane do:
- Identyfikacji użytkownika
- Autoryzacji dostępu do zasobów
- Przechowywania kontekstu aktywnej firmy
- Określania ról i uprawnień

---

## 2. Struktura Claims

### 2.1. Claims Użytkownika (User Identity)

| Nazwa Claim | Typ Claim | Typ Danych | Wymagane | Opis |
|-------------|-----------|------------|----------|------|
| `ClaimTypes.NameIdentifier` | Standard | `string` (int) | ✅ Tak | Unikalne ID użytkownika z bazy danych |
| `ClaimTypes.Name` | Standard | `string` | ✅ Tak | Pełna nazwa użytkownika (FullName) |
| `ClaimTypes.Email` | Standard | `string` | ❌ Nie | Email użytkownika (opcjonalnie) |
| `ClaimTypes.GivenName` | Standard | `string` | ❌ Nie | Imię użytkownika (opcjonalnie) |
| `ClaimTypes.Surname` | Standard | `string` | ❌ Nie | Nazwisko użytkownika (opcjonalnie) |

**Uwaga:** `ClaimTypes.NameIdentifier` jest standardowym claimem ASP.NET Core używanym do identyfikacji użytkownika. Nie należy używać dodatkowego claima "UserId".

### 2.2. Claims Aktywnej Firmy (Active Company Context)

| Nazwa Claim | Typ Claim | Typ Danych | Wymagane | Opis |
|-------------|-----------|------------|----------|------|
| `"http://schemas.erp.local/claims/companyid"` | Custom | `string` (int) | ✅ Tak* | ID aktywnie wybranej firmy |
| `"http://schemas.erp.local/claims/companyname"` | Custom | `string` | ❌ Nie | Nazwa aktywnej firmy (dla wyświetlania) |
| `"http://schemas.erp.local/claims/companyroleid"` | Custom | `string` (int) | ❌ Nie | ID roli użytkownika w aktywnej firmie |

**Uwaga:** `CompanyId` jest wymagane po wyborze firmy, ale nie podczas pierwszego logowania (użytkownik musi najpierw wybrać firmę).

### 2.3. Claims Roli (Role Claims)

| Nazwa Claim | Typ Claim | Typ Danych | Wymagane | Opis |
|-------------|-----------|------------|----------|------|
| `ClaimTypes.Role` | Standard | `string` | ❌ Nie | Nazwa roli (np. "Admin", "User") - może być wiele |
| `"http://schemas.erp.local/claims/roleid"` | Custom | `string` (int) | ❌ Nie | ID roli w aktywnej firmie |

**Uwaga:** `ClaimTypes.Role` może występować wielokrotnie, jeśli użytkownik ma wiele ról. `RoleId` odnosi się do roli w kontekście aktywnej firmy.

---

## 3. Definicja Stałych (Constants)

### 3.1. Rekomendowana Implementacja

```csharp
namespace ERP.Shared.Constants;

/// <summary>
/// Stałe dla custom Claims w systemie ERP
/// </summary>
public static class ErpClaimTypes
{
    // Base URI dla custom claims (zgodnie z RFC 3986)
    private const string BaseUri = "http://schemas.erp.local/claims";
    
    /// <summary>
    /// ID aktywnie wybranej firmy przez użytkownika
    /// </summary>
    public const string CompanyId = BaseUri + "/companyid";
    
    /// <summary>
    /// Nazwa aktywnej firmy (dla wyświetlania)
    /// </summary>
    public const string CompanyName = BaseUri + "/companyname";
    
    /// <summary>
    /// ID roli użytkownika w aktywnej firmie
    /// </summary>
    public const string RoleId = BaseUri + "/roleid";
    
    /// <summary>
    /// ID użytkownika (alternatywa dla ClaimTypes.NameIdentifier)
    /// </summary>
    public const string UserId = BaseUri + "/userid";
}
```

### 3.2. Uproszczona Wersja (dla kompatybilności)

Jeśli preferujesz krótsze nazwy (obecna implementacja):

```csharp
public static class ErpClaimTypes
{
    public const string CompanyId = "CompanyId";
    public const string CompanyName = "CompanyName";
    public const string RoleId = "RoleId";
    public const string UserId = "UserId";
}
```

**Rekomendacja:** Użyj pełnych URI dla custom claims, aby uniknąć konfliktów z innymi systemami.

---

## 4. Przykłady Użycia w HttpContext.User

### 4.1. Podstawowe Pobieranie Informacji

```csharp
using System.Security.Claims;
using ERP.Shared.Extensions;
using ERP.Shared.Constants;

public class ExampleController : Controller
{
    // Pobieranie UserId
    public IActionResult GetUserId()
    {
        // Metoda 1: Używając extension method (rekomendowane)
        var userId = User.GetUserId(); // int?
        
        // Metoda 2: Bezpośrednio z Claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId2))
        {
            // użyj userId2
        }
        
        return Ok(new { UserId = userId });
    }
    
    // Pobieranie CompanyId
    public IActionResult GetCompanyId()
    {
        // Metoda 1: Używając extension method (rekomendowane)
        var companyId = User.GetCompanyId(); // int?
        
        // Metoda 2: Bezpośrednio z Claims
        var companyIdClaim = User.FindFirst(ErpClaimTypes.CompanyId)?.Value;
        if (int.TryParse(companyIdClaim, out int companyId2))
        {
            // użyj companyId2
        }
        
        return Ok(new { CompanyId = companyId });
    }
    
    // Pobieranie RoleId
    public IActionResult GetRoleId()
    {
        var roleId = User.GetRoleId(); // int?
        return Ok(new { RoleId = roleId });
    }
    
    // Pobieranie nazwy użytkownika
    public IActionResult GetUserName()
    {
        var userName = User.GetUserName(); // string?
        // lub bezpośrednio:
        var name = User.FindFirst(ClaimTypes.Name)?.Value;
        
        return Ok(new { UserName = userName });
    }
}
```

### 4.2. Walidacja Dostępu do Firmy

```csharp
public class CustomersController : BaseController
{
    public async Task<IActionResult> Index()
    {
        // Sprawdzenie czy użytkownik ma wybraną firmę
        var companyId = User.GetCompanyId();
        if (!companyId.HasValue)
        {
            return RedirectToAction("Select", "Company");
        }
        
        // Użycie companyId w zapytaniu
        var customers = await _customerService.GetByCompanyIdAsync(companyId.Value);
        return View(customers);
    }
}
```

### 4.3. Sprawdzanie Ról

```csharp
public class AdminController : Controller
{
    [Authorize(Policy = "Admin")]
    public IActionResult Index()
    {
        // Policy sprawdza ClaimTypes.Role automatycznie
        // Możemy też sprawdzić ręcznie:
        var isAdmin = User.IsInRole("Admin");
        var roleId = User.GetRoleId();
        
        return View();
    }
}
```

### 4.4. Tworzenie Claims podczas Logowania

```csharp
// W AccountController.Login()
var claims = new List<Claim>
{
    // Standardowe claims użytkownika
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Name, user.FullName),
    
    // Opcjonalne standardowe claims
    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
    new Claim(ClaimTypes.GivenName, user.FirstName ?? string.Empty),
    new Claim(ClaimTypes.Surname, user.Surname ?? string.Empty),
    
    // NIE dodajemy CompanyId i RoleId tutaj - użytkownik jeszcze nie wybrał firmy
};

var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
await HttpContext.SignInAsync(
    CookieAuthenticationDefaults.AuthenticationScheme,
    new ClaimsPrincipal(claimsIdentity),
    authProperties);
```

### 4.5. Aktualizacja Claims po Wyborze Firmy

```csharp
// W CompanyController.Select()
var claims = User.Claims.ToList();

// Usuwamy stare claims firmy
claims.RemoveAll(c => 
    c.Type == ErpClaimTypes.CompanyId || 
    c.Type == ErpClaimTypes.CompanyName ||
    c.Type == ErpClaimTypes.RoleId);

// Dodajemy nowe claims
claims.Add(new Claim(ErpClaimTypes.CompanyId, companyId.ToString()));
claims.Add(new Claim(ErpClaimTypes.CompanyName, company.Name));
if (roleId.HasValue)
{
    claims.Add(new Claim(ErpClaimTypes.RoleId, roleId.Value.ToString()));
    claims.Add(new Claim(ClaimTypes.Role, role.Name)); // Nazwa roli dla policy
}

var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
await HttpContext.SignInAsync(
    CookieAuthenticationDefaults.AuthenticationScheme,
    new ClaimsPrincipal(claimsIdentity),
    authProperties);
```

---

## 5. Extension Methods (Rozszerzenia ClaimsPrincipal)

### 5.1. Aktualna Implementacja

```csharp
// ERP.Shared.Extensions.ClaimsPrincipalExtensions
public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? 
                         principal.FindFirst(ErpClaimTypes.UserId);
        
        if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value) || 
            !int.TryParse(userIdClaim.Value, out int userId))
            return null;
            
        return userId;
    }
    
    public static int? GetCompanyId(this ClaimsPrincipal principal)
    {
        var companyIdClaim = principal.FindFirst(ErpClaimTypes.CompanyId);
        
        if (companyIdClaim == null || string.IsNullOrEmpty(companyIdClaim.Value) || 
            !int.TryParse(companyIdClaim.Value, out int companyId))
            return null;
            
        return companyId;
    }
    
    public static int? GetRoleId(this ClaimsPrincipal principal)
    {
        var roleIdClaim = principal.FindFirst(ErpClaimTypes.RoleId);
        
        if (roleIdClaim == null || string.IsNullOrEmpty(roleIdClaim.Value) || 
            !int.TryParse(roleIdClaim.Value, out int roleId))
            return null;
            
        return roleId;
    }
    
    public static string? GetUserName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Name)?.Value;
    }
    
    public static bool HasCompanySelected(this ClaimsPrincipal principal)
    {
        return GetCompanyId(principal).HasValue;
    }
    
    public static string? GetCompanyName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ErpClaimTypes.CompanyName)?.Value;
    }
}
```

---

## 6. Lifecycle Claims

### 6.1. Podczas Logowania

```
1. Użytkownik loguje się → AccountController.Login()
   └─> Dodawane Claims:
       ✅ ClaimTypes.NameIdentifier (UserId)
       ✅ ClaimTypes.Name (FullName)
       ❌ CompanyId (brak - użytkownik jeszcze nie wybrał)
       ❌ RoleId (brak - użytkownik jeszcze nie wybrał)

2. Przekierowanie → CompanyController.Select()
   └─> Użytkownik wybiera firmę
```

### 6.2. Po Wyborze Firmy

```
3. Użytkownik wybiera firmę → CompanyController.Select(companyId)
   └─> Aktualizacja Claims:
       ✅ ClaimTypes.NameIdentifier (bez zmian)
       ✅ ClaimTypes.Name (bez zmian)
       ✅ CompanyId (DODANE)
       ✅ CompanyName (DODANE - opcjonalnie)
       ✅ RoleId (DODANE - jeśli użytkownik ma rolę w firmie)
       ✅ ClaimTypes.Role (DODANE - nazwa roli dla policy)

4. Przekierowanie → MainController.Index()
   └─> Wszystkie Claims dostępne w HttpContext.User
```

### 6.3. Podczas Zmiany Firmy

```
5. Użytkownik zmienia firmę → CompanyController.Select(newCompanyId)
   └─> Aktualizacja Claims:
       ✅ Stare CompanyId, CompanyName, RoleId → USUNIĘTE
       ✅ Nowe CompanyId, CompanyName, RoleId → DODANE
       ✅ ClaimTypes.NameIdentifier (bez zmian)
       ✅ ClaimTypes.Name (bez zmian)
```

---

## 7. Bezpieczeństwo i Walidacja

### 7.1. Walidacja per Request

Każdy request powinien walidować:
1. **UserId** - czy użytkownik jest zalogowany
2. **CompanyId** - czy użytkownik wybrał firmę (dla operacji wymagających firmy)
3. **CompanyId + UserId** - czy użytkownik ma dostęp do wybranej firmy (sprawdzenie w bazie)

### 7.2. Przykład Walidacji w BaseController

```csharp
public abstract class BaseController : Controller
{
    protected override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        
        // Sprawdzenie czy użytkownik jest zalogowany
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            context.Result = RedirectToAction("Login", "Account");
            return;
        }
        
        // Sprawdzenie czy użytkownik wybrał firmę (dla akcji wymagających firmy)
        // Można to oznaczyć atrybutem [RequireCompany]
        var companyId = User.GetCompanyId();
        if (!companyId.HasValue && RequiresCompany())
        {
            context.Result = RedirectToAction("Select", "Company");
            return;
        }
        
        // Opcjonalnie: Walidacja dostępu do firmy w bazie danych
        // (sprawdzenie czy użytkownik faktycznie ma dostęp do CompanyId)
    }
    
    protected virtual bool RequiresCompany() => false;
}
```

---

## 8. Standardowe ClaimTypes vs Custom Claims

### 8.1. Standardowe ClaimTypes (Rekomendowane)

| Claim | Użycie | Przykład |
|-------|--------|----------|
| `ClaimTypes.NameIdentifier` | ID użytkownika | `"123"` |
| `ClaimTypes.Name` | Pełna nazwa | `"Jan Kowalski"` |
| `ClaimTypes.Email` | Email | `"jan@example.com"` |
| `ClaimTypes.Role` | Nazwa roli | `"Admin"`, `"User"` |
| `ClaimTypes.GivenName` | Imię | `"Jan"` |
| `ClaimTypes.Surname` | Nazwisko | `"Kowalski"` |

### 8.2. Custom Claims (Dla specyfiki ERP)

| Claim | Użycie | Przykład |
|-------|--------|----------|
| `ErpClaimTypes.CompanyId` | ID aktywnej firmy | `"5"` |
| `ErpClaimTypes.CompanyName` | Nazwa firmy | `"Firma XYZ Sp. z o.o."` |
| `ErpClaimTypes.RoleId` | ID roli w firmie | `"2"` |

**Uwaga:** Custom claims powinny używać pełnych URI (np. `"http://schemas.erp.local/claims/companyid"`) zamiast krótkich nazw, aby uniknąć konfliktów.

---

## 9. Przykłady Scenariuszy

### 9.1. Scenariusz 1: Użytkownik loguje się i wybiera firmę

```
1. POST /Account/Login
   Claims: [NameIdentifier="123", Name="Jan Kowalski"]
   
2. GET /Company/Select
   Claims: [NameIdentifier="123", Name="Jan Kowalski"]
   
3. POST /Company/Select?companyId=5
   Claims: [NameIdentifier="123", Name="Jan Kowalski", CompanyId="5", RoleId="2"]
   
4. GET /Main/Index
   Claims: [NameIdentifier="123", Name="Jan Kowalski", CompanyId="5", RoleId="2"]
   └─> User.GetCompanyId() → 5
   └─> User.GetRoleId() → 2
```

### 9.2. Scenariusz 2: Użytkownik zmienia firmę

```
1. GET /Company/Select
   Claims: [NameIdentifier="123", Name="Jan Kowalski", CompanyId="5", RoleId="2"]
   
2. POST /Company/Select?companyId=7
   Claims: [NameIdentifier="123", Name="Jan Kowalski", CompanyId="7", RoleId="1"]
   └─> Stare CompanyId="5" → USUNIĘTE
   └─> Nowe CompanyId="7" → DODANE
```

### 9.3. Scenariusz 3: Sprawdzanie uprawnień

```csharp
// W kontrolerze
[Authorize(Policy = "Customers:Read")]
public async Task<IActionResult> Index()
{
    var companyId = User.GetCompanyId(); // Z Claims
    var customers = await _customerService.GetByCompanyIdAsync(companyId.Value);
    return View(customers);
}

// Policy sprawdza:
// 1. Czy użytkownik ma CompanyId w Claims
// 2. Czy użytkownik ma dostęp do CompanyId (sprawdzenie w bazie)
// 3. Czy użytkownik ma uprawnienie "Customers:Read"
```

---

## 10. Rekomendacje Implementacyjne

### 10.1. ✅ DO

- Używaj standardowych `ClaimTypes` dla podstawowych informacji o użytkowniku
- Używaj extension methods (`GetUserId()`, `GetCompanyId()`) zamiast bezpośredniego dostępu do Claims
- Waliduj `CompanyId` per request (sprawdzenie w bazie czy użytkownik ma dostęp)
- Używaj pełnych URI dla custom claims (`http://schemas.erp.local/claims/...`)
- Aktualizuj Claims przy zmianie firmy (usuwaj stare, dodawaj nowe)

### 10.2. ❌ DON'T

- Nie przechowuj wrażliwych danych w Claims (hasła, tokeny)
- Nie używaj Claims jako cache (Claims są w cookie, nie w bazie)
- Nie duplikuj informacji (np. `ClaimTypes.NameIdentifier` i `"UserId"`)
- Nie używaj krótkich nazw dla custom claims (ryzyko konfliktów)
- Nie zakładaj, że Claims są zawsze obecne (zawsze sprawdzaj `HasValue`)

---

## 11. Podsumowanie

### 11.1. Minimalny Zestaw Claims po Logowaniu

```
✅ ClaimTypes.NameIdentifier → UserId
✅ ClaimTypes.Name → FullName
```

### 11.2. Pełny Zestaw Claims po Wyborze Firmy

```
✅ ClaimTypes.NameIdentifier → UserId
✅ ClaimTypes.Name → FullName
✅ ErpClaimTypes.CompanyId → ActiveCompanyId
✅ ErpClaimTypes.CompanyName → CompanyName (opcjonalnie)
✅ ErpClaimTypes.RoleId → RoleIdInCompany
✅ ClaimTypes.Role → RoleName (dla policy)
```

### 11.3. Typy Danych

- **UserId**: `int` (z `ClaimTypes.NameIdentifier`)
- **CompanyId**: `int?` (z `ErpClaimTypes.CompanyId`)
- **RoleId**: `int?` (z `ErpClaimTypes.RoleId`)
- **UserName**: `string?` (z `ClaimTypes.Name`)

---

**Dokumentacja przygotowana:** 2026-01-26  
**Wersja:** 1.0  
**Status:** Docelowy model (bez modyfikacji kodu)
