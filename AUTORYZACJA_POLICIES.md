# Implementacja Authorization Policies

## Przegląd zmian

Zaimplementowano system autoryzacji oparty na ASP.NET Core Authorization Policies, który:
- Sprawdza role użytkowników
- Sprawdza dostęp do wybranej firmy (activeCompanyId)
- Zabezpiecza wszystkie endpointy kontrolerów

## Utworzone komponenty

### 1. Authorization Handlers

#### `CompanyAccessAuthorizationHandler`
- **Lokalizacja:** `ERP.UI.Web/Authorization/CompanyAccessAuthorizationHandler.cs`
- **Cel:** Sprawdza czy użytkownik ma dostęp do wybranej firmy
- **Mechanizm:** Weryfikuje relację użytkownik-firma w tabeli `operatorfirma` (UserCompany)

#### `RoleAuthorizationHandler`
- **Lokalizacja:** `ERP.UI.Web/Authorization/RoleAuthorizationHandler.cs`
- **Cel:** Sprawdza czy użytkownik ma wymaganą rolę
- **Mechanizm:** Weryfikuje `RoleId` z Claims użytkownika

#### `TablePermissionAuthorizationHandler`
- **Lokalizacja:** `ERP.UI.Web/Authorization/TablePermissionAuthorizationHandler.cs`
- **Cel:** Sprawdza uprawnienia użytkownika do konkretnych tabel (SELECT, INSERT, UPDATE, DELETE)
- **Mechanizm:** Wykorzystuje `IOperatorPermissionService` do sprawdzania uprawnień w tabeli `operator_table_permissions`

### 2. Authorization Policies

Zarejestrowane w `Program.cs`:

#### Podstawowe policies:
- **`CompanyAccess`** - Wymaga dostępu do wybranej firmy
- **`HasRole`** - Wymaga jakiejkolwiek roli
- **`CompanyAccessAndRole`** - Wymaga dostępu do firmy I roli
- **`Admin`** - Wymaga dostępu do firmy i roli (dla administratorów)

#### Policies dla uprawnień do tabel:

**Customers (Odbiorcy):**
- `Customers:Read` - Uprawnienie SELECT
- `Customers:Write` - Uprawnienia INSERT + UPDATE
- `Customers:Delete` - Uprawnienie DELETE

**Suppliers (Dostawcy):**
- `Suppliers:Read` - Uprawnienie SELECT
- `Suppliers:Write` - Uprawnienia INSERT + UPDATE
- `Suppliers:Delete` - Uprawnienie DELETE

**Products (Towary):**
- `Products:Read` - Uprawnienie SELECT
- `Products:Write` - Uprawnienia INSERT + UPDATE
- `Products:Delete` - Uprawnienie DELETE

**Offers (Oferty):**
- `Offers:Read` - Uprawnienie SELECT
- `Offers:Write` - Uprawnienia INSERT + UPDATE
- `Offers:Delete` - Uprawnienie DELETE

**Orders (Zamówienia):**
- `Orders:Read` - Uprawnienie SELECT
- `Orders:Write` - Uprawnienia INSERT + UPDATE
- `Orders:Delete` - Uprawnienie DELETE

### 3. Zabezpieczenie kontrolerów

Wszystkie kontrolery biznesowe zostały zabezpieczone odpowiednimi policies:

| Kontroler | Policy | Opis |
|-----------|--------|------|
| `CustomersController` | `Customers:Read` | Odbiorcy - wymaga uprawnienia SELECT |
| `SuppliersController` | `Suppliers:Read` | Dostawcy - wymaga uprawnienia SELECT |
| `ProductsController` | `Products:Read` | Towary - wymaga uprawnienia SELECT |
| `OffersController` | `Offers:Read` | Oferty - wymaga uprawnienia SELECT |
| `OrdersController` | `Orders:Read` | Zamówienia - wymaga uprawnienia SELECT |
| `OrdersHalaController` | `Orders:Read` | Zamówienia hala - wymaga uprawnienia SELECT |
| `MainController` | `CompanyAccessAndRole` | Główny kontroler - wymaga firmy i roli |
| `AdminController` | `Admin` | Panel administracyjny - wymaga roli |
| `CompanyController` | `[Authorize]` | Wybór firmy - wymaga tylko autentykacji |
| `AccountController` | Brak | Publiczny - logowanie |
| `HomeController` | Brak | Publiczny - strona główna |

## Mechanizm działania

### 1. Sprawdzanie CompanyId
- `CompanyAccessAuthorizationHandler` sprawdza czy użytkownik ma relację z wybraną firmą w tabeli `operatorfirma`
- `CompanyId` jest pobierany z Claims użytkownika (ustawiane w `CompanyController.Select`)

### 2. Sprawdzanie ról
- `RoleAuthorizationHandler` sprawdza `RoleId` z Claims użytkownika
- `RoleId` jest ustawiane podczas wyboru firmy w `CompanyController.Select`

### 3. Sprawdzanie uprawnień do tabel
- `TablePermissionAuthorizationHandler` wykorzystuje `IOperatorPermissionService`
- Sprawdza uprawnienia w tabeli `operator_table_permissions` dla konkretnej tabeli i typu operacji

### 4. BaseController
- `BaseController` nadal sprawdza autentykację i wybraną firmę na poziomie action filter
- Działa jako dodatkowa warstwa ochrony przed kontrolerami, które nie mają atrybutu `[Authorize]`

## Rejestracja w Program.cs

```csharp
// Authorization Handlers
builder.Services.AddScoped<IAuthorizationHandler, CompanyAccessAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TablePermissionAuthorizationHandler>();

// Authorization Policies
builder.Services.AddAuthorization(options => { /* ... */ });
```

## Bezpieczeństwo

### Warstwy ochrony:
1. **Autentykacja** - Cookie Authentication (sprawdzana przez `BaseController`)
2. **Company Access** - Sprawdzanie relacji użytkownik-firma
3. **Role** - Sprawdzanie roli użytkownika
4. **Table Permissions** - Sprawdzanie uprawnień do konkretnych tabel i operacji

### Zalety implementacji:
- ✅ Centralne zarządzanie uprawnieniami przez policies
- ✅ Elastyczne - łatwe dodawanie nowych policies
- ✅ Testowalne - handlers można testować niezależnie
- ✅ Skalowalne - łatwe rozszerzanie o nowe wymagania
- ✅ Zgodne z best practices ASP.NET Core

## Uwagi

1. **Brak zmian w UI** - Wszystkie zmiany są po stronie backendu, UI pozostaje niezmienione
2. **Backward compatibility** - Istniejące uprawnienia w bazie danych są wykorzystywane
3. **Performance** - Handlers wykonują zapytania do bazy danych, można rozważyć cachowanie dla często używanych uprawnień

## Następne kroki (opcjonalne)

1. Dodanie cachowania uprawnień dla lepszej wydajności
2. Rozszerzenie policies o bardziej szczegółowe uprawnienia (np. per-endpoint)
3. Dodanie logowania prób nieautoryzowanego dostępu
4. Utworzenie middleware do automatycznego sprawdzania uprawnień na podstawie routingu
