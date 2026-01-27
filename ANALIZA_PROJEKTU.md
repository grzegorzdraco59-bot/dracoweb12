# ANALIZA PROJEKTU ERP WEB - dracoWEB5

## 1. STRUKTURA SOLUTION I PROJEKTY

### Struktura Solution (ERP.sln)

Solution zawiera następujące projekty:

1. **ERP.Shared** - Biblioteka współdzielona
   - Stałe bazy danych (`DatabaseConstants`)
   - `SessionManager` (singleton) - **PROBLEM ARCHITEKTONICZNY**

2. **ERP.Domain** - Warstwa domenowa
   - Encje domenowe (`User`, `Company`, `Order`, `Customer`, `Offer`, etc.)
   - Interfejsy repozytoriów
   - Factory klasy dla encji

3. **ERP.Infrastructure** - Warstwa infrastruktury
   - Implementacje repozytoriów (MySqlConnector)
   - `DatabaseContext` - wrapper dla połączeń MySQL
   - Repozytoria: `CustomerRepository`, `OrderRepository`, `OrderMainRepository`, `OfferRepository`, `ProductRepository`, `SupplierRepository`, `UserRepository`, `CompanyRepository`, etc.

4. **ERP.Application** - Warstwa aplikacyjna
   - Serwisy aplikacyjne (`AuthenticationService`, `OrderService`, `CustomerService`, `OperatorPermissionService`)
   - DTOs (Data Transfer Objects)
   - Interfejsy serwisów

5. **ERP.UI.Web** - Aplikacja webowa (ASP.NET Core MVC)
   - Kontrolery MVC
   - Widoki Razor
   - Konfiguracja DI i middleware

6. **ERP.UI.WPF** - Aplikacja desktopowa (WPF)
   - ViewModels (MVVM)
   - Views (XAML)

7. **ERP.Migrations** - Migracje bazy danych
   - Skrypty SQL do migracji

8. **ERP.Reports** - Raporty (pusty projekt)

9. **ERP.Tests** - Testy jednostkowe (minimalne)

10. **Projekty pomocnicze:**
    - `SyncDatabase` - synchronizacja bazy
    - `RunMigration` - uruchamianie migracji
    - `TestConnection` - testowanie połączenia
    - `CheckTable` - sprawdzanie tabel

### Architektura warstwowa

Projekt używa architektury warstwowej (Clean Architecture / Onion Architecture):
- **Domain** - encje i logika biznesowa
- **Application** - serwisy aplikacyjne i DTOs
- **Infrastructure** - implementacje repozytoriów i dostęp do danych
- **UI** - warstwa prezentacji (Web i WPF)

---

## 2. SPOSÓB LOGOWANIA

### Mechanizm autentykacji

1. **Tabele bazy danych:**
   - `operator` - dane użytkownika
   - `operator_login` - dane logowania (login, hasłohash)

2. **Proces logowania:**
   - Użytkownik wprowadza login i hasło w `AccountController.Login`
   - `AuthenticationService.AuthenticateAsync` weryfikuje dane:
     - Pobiera `UserLogin` z tabeli `operator_login` po loginie
     - Weryfikuje hasło używając SHA256 (proste hashowanie)
     - Pobiera dane użytkownika z tabeli `operator`
   - Po sukcesie tworzone są Claims (Identity):
     - `ClaimTypes.NameIdentifier` = UserId
     - `ClaimTypes.Name` = FullName
   - Użytkownik przekierowywany do `Company/Select` (wybór firmy)

3. **Autoryzacja:**
   - Używa Cookie Authentication (`CookieAuthenticationDefaults.AuthenticationScheme`)
   - `BaseController` sprawdza `User.Identity.IsAuthenticated`
   - Sesja cookie: 30 minut, sliding expiration

### Problemy z logowaniem:

1. **Słabe hashowanie hasła:**
   - Używa SHA256 z Base64 - **NIEBEZPIECZNE**
   - Brak salt
   - Komentarz w kodzie: "w produkcji użyj BCrypt lub Argon2" - ale nie zaimplementowane

2. **Brak walidacji siły hasła**
3. **Brak rate limiting** - możliwość brute force
4. **Brak 2FA/MFA**
5. **Hasła w plaintext w niektórych miejscach** (np. `SenderEmailPassword` w encji `User`)

---

## 3. OBSŁUGA WIELU FIRM

### Model multi-tenant

System obsługuje wiele firm poprzez:

1. **Tabele relacyjne:**
   - `firmy` - firmy
   - `operatorfirma` - relacja użytkownik-firma-rola
   - Wszystkie tabele biznesowe mają kolumnę `id_firmy`

2. **Proces wyboru firmy:**
   - Po zalogowaniu użytkownik wybiera firmę w `CompanyController.Select`
   - Wybrana firma zapisywana w **sesji HTTP** (`HttpContext.Session.SetInt32("CompanyId", companyId)`)
   - `BaseController` sprawdza czy firma jest wybrana (`RequireCompanyAttribute`)

3. **Filtrowanie danych:**
   - Repozytoria pobierają `CompanyId` z `SessionManager.Instance.CurrentCompanyId`
   - Wszystkie zapytania SQL zawierają `WHERE id_firmy = @CompanyId`
   - Przykład: `CustomerRepository.GetAllAsync()` filtruje po `id_firmy`

### Problemy z obsługą wielu firm:

1. **SessionManager jako Singleton:**
   - `SessionManager` jest singletonem - **KRYTYCZNY PROBLEM**
   - W aplikacji webowej singleton jest współdzielony między wszystkimi requestami
   - Może prowadzić do wycieku danych między użytkownikami
   - Używany zarówno w Web jak i WPF - konflikt architektury

2. **Podwójne przechowywanie stanu:**
   - Firma przechowywana w sesji HTTP (`HttpContext.Session`)
   - Równocześnie w `SessionManager.Instance` (singleton)
   - Ryzyko niespójności

3. **Brak weryfikacji uprawnień:**
   - System nie weryfikuje czy użytkownik ma dostęp do wybranej firmy
   - Możliwość manipulacji `CompanyId` w sesji

4. **Brak izolacji danych na poziomie bazy:**
   - Brak row-level security
   - Wszystkie zapytania muszą ręcznie filtrować po `id_firmy`
   - Ryzyko zapomnienia filtru w nowym kodzie

---

## 4. GDZIE JEST LOGIKA BIZNESOWA

### Rozmieszczenie logiki biznesowej:

1. **ERP.Domain (Encje domenowe):**
   - Podstawowa walidacja w konstruktorach i metodach `Update*`
   - Przykład: `Company.UpdateName()` sprawdza czy nazwa nie jest pusta
   - Przykład: `Order.UpdateStatus()` - zmiana statusu zamówienia
   - **PROBLEM:** Większość logiki biznesowej jest w serwisach, nie w encjach

2. **ERP.Application (Serwisy aplikacyjne):**
   - `OrderService` - konwersja dat (format Clarion), mapowanie DTO ↔ Entity
   - `CustomerService` - operacje na klientach
   - `AuthenticationService` - logika autentykacji i autoryzacji
   - `OperatorPermissionService` - zarządzanie uprawnieniami
   - **PROBLEM:** Serwisy są głównie thin wrappers nad repozytoriami

3. **ERP.Infrastructure (Repozytoria):**
   - **PROBLEM:** Logika biznesowa w repozytoriach:
     - `OrderMainRepository` - konwersja dat Clarion
     - Dynamiczne wykrywanie nazw kolumn (`GetIdColumnNameAsync`)
     - Mapowanie danych z bazy do encji

4. **ERP.UI.Web (Kontrolery):**
   - **PROBLEM:** Logika biznesowa w kontrolerach:
     - `OrdersController` - ręczne ustawianie `SessionManager`
     - `CompanyController` - logika wyboru firmy

### Brakująca logika biznesowa:

1. **Brak walidacji biznesowej:**
   - Brak walidacji reguł biznesowych (np. czy można usunąć zamówienie ze statusem "dostarczone")
   - Brak walidacji stanów (state machine dla zamówień)

2. **Brak transakcji:**
   - Operacje wieloetapowe nie są w transakcjach
   - Ryzyko niespójności danych

3. **Brak eventów domenowych:**
   - Brak mechanizmu eventów dla zmian w encjach
   - Trudność w implementacji powiadomień, audytu

---

## 5. BŁĘDY ARCHITEKTONICZNE

### Krytyczne błędy:

1. **SessionManager jako Singleton w aplikacji webowej:**
   ```csharp
   public static SessionManager Instance { get; }
   ```
   - Singleton współdzielony między requestami
   - Ryzyko wycieku danych między użytkownikami
   - **ROZWIĄZANIE:** Używać `HttpContext.Session` lub scoped service

2. **Hardcoded connection string z hasłem:**
   ```csharp
   "Server=localhost;Port=3306;Database=locbd;User Id=root;Password=dracogk0909;SslMode=None;"
   ```
   - Hasło w kodzie źródłowym
   - W wielu miejscach: `Program.cs`, `DatabaseConstants.cs`, `SyncDatabase`, `TestConnection`, etc.
   - **ROZWIĄZANIE:** Tylko w `appsettings.json` (i w .gitignore), używać User Secrets w dev

3. **Słabe hashowanie hasła:**
   ```csharp
   using var sha256 = SHA256.Create();
   var hash = Convert.ToBase64String(sha256.ComputeHash(bytes));
   ```
   - SHA256 bez salt - podatne na rainbow tables
   - **ROZWIĄZANIE:** BCrypt lub Argon2

4. **Brak izolacji warstw:**
   - Repozytoria używają `SessionManager` (Shared) - naruszenie warstw
   - Infrastructure zależy od Shared - powinno być odwrotnie

5. **Podwójne źródło prawdy:**
   - Firma w sesji HTTP i w `SessionManager`
   - Ryzyko niespójności

### Poważne błędy:

6. **Logika biznesowa w repozytoriach:**
   - Konwersja dat Clarion w repozytoriach
   - Dynamiczne wykrywanie kolumn
   - Powinno być w serwisach lub value objects

7. **Brak walidacji uprawnień:**
   - System ma `OperatorTablePermission`, ale nie jest używany w kontrolerach
   - Brak sprawdzania uprawnień przed operacjami

8. **Refleksja do ustawiania Id:**
   ```csharp
   var idProperty = typeof(BaseEntity).GetProperty("Id", ...);
   idProperty.SetValue(company, id);
   ```
   - Naruszenie enkapsulacji
   - Trudne w debugowaniu

9. **Brak obsługi błędów:**
   - Brak globalnego exception handlera
   - Brak logowania błędów
   - Błędy zwracane jako JSON w niektórych miejscach

10. **Brak transakcji:**
    - Operacje wieloetapowe nie są w transakcjach
    - Przykład: tworzenie zamówienia z pozycjami

11. **Mieszanie DTOs i Entities:**
    - `OrderMainRepository` zwraca DTOs zamiast Entities
    - Naruszenie separacji warstw

12. **Brak dependency injection dla SessionManager:**
    - Singleton dostępny globalnie
    - Trudne w testowaniu

### Średnie błędy:

13. **Brak walidacji wejścia:**
    - Kontrolery nie walidują DTOs
    - Brak użycia FluentValidation lub Data Annotations

14. **Brak cache'owania:**
    - Każde zapytanie idzie do bazy
    - Brak cache dla często używanych danych (firmy, użytkownicy)

15. **Brak async/await consistency:**
    - Niektóre metody synchroniczne (`CreateConnection()`)

16. **Brak unit testów:**
    - Projekt `ERP.Tests` istnieje ale jest pusty
    - Brak testów dla logiki biznesowej

17. **Brak dokumentacji API:**
    - Brak Swagger/OpenAPI
    - Trudne w integracji

18. **Mieszanie konwencji nazewnictwa:**
    - Tabele: `zamowienia`, `aoferty`, `Odbiorcy` (różne konwencje)
    - Kolumny: `ID_FIRMY`, `id_firmy`, `id` (różne konwencje)

---

## 6. CO ZOSTAWIĆ

### Dobre praktyki do zachowania:

1. **Architektura warstwowa:**
   - Separacja Domain/Application/Infrastructure/UI
   - Interfejsy repozytoriów w Domain

2. **Użycie DTOs:**
   - Separacja encji domenowych od warstwy prezentacji
   - DTOs w Application layer

3. **Dependency Injection:**
   - Właściwa konfiguracja DI w `Program.cs`
   - Scoped repositories i services

4. **Encje domenowe z metodami Update:**
   - Enkapsulacja zmian w encjach
   - Przykład: `Company.UpdateName()`, `Order.UpdateStatus()`

5. **Factory pattern:**
   - `CustomerFactory.FromDatabase()` - tworzenie encji z danych bazy

6. **BaseController:**
   - Centralna logika autoryzacji
   - Sprawdzanie autentykacji i wybranej firmy

7. **Użycie Claims dla autentykacji:**
   - Standardowy mechanizm ASP.NET Core
   - Rozszerzalny

---

## 7. CO REFAKTOROWAĆ

### Priorytet 1 (Krytyczne):

1. **SessionManager:**
   - Usunąć singleton
   - Używać scoped service z `HttpContext.Session`
   - Dla WPF użyć osobnego mechanizmu

2. **Hashowanie hasła:**
   - Zaimplementować BCrypt lub Argon2
   - Migracja istniejących haseł

3. **Connection string:**
   - Usunąć hardcoded hasła z kodu
   - Tylko w `appsettings.json` (w .gitignore)
   - User Secrets dla development

4. **Izolacja warstw:**
   - Usunąć zależność Infrastructure → Shared
   - Przenieść `SessionManager` do Infrastructure jako scoped service
   - Repozytoria powinny otrzymywać `CompanyId` jako parametr

### Priorytet 2 (Wysokie):

5. **Logika biznesowa:**
   - Przenieść konwersję dat Clarion do serwisów lub value objects
   - Dodać walidację biznesową w encjach
   - Implementować domain events

6. **Walidacja uprawnień:**
   - Dodać sprawdzanie `OperatorTablePermission` w kontrolerach
   - Middleware lub attribute dla uprawnień

7. **Transakcje:**
   - Dodać Unit of Work pattern
   - Transakcje dla operacji wieloetapowych

8. **Obsługa błędów:**
   - Global exception handler
   - Logowanie błędów (Serilog/NLog)
   - Zwracanie odpowiednich kodów HTTP

9. **Refleksja:**
   - Usunąć użycie refleksji do ustawiania Id
   - Dodać public setter lub factory method

10. **DTOs vs Entities:**
    - Repozytoria powinny zwracać Entities
    - Serwisy mapują Entities → DTOs

### Priorytet 3 (Średnie):

11. **Walidacja wejścia:**
    - FluentValidation dla DTOs
    - ModelState validation w kontrolerach

12. **Testy:**
    - Unit testy dla serwisów
    - Integration testy dla repozytoriów
    - Mockowanie zależności

13. **Cache:**
    - Memory cache dla często używanych danych
    - Distributed cache dla multi-instance

14. **Dokumentacja API:**
    - Swagger/OpenAPI
    - XML comments dla kontrolerów

15. **Konwencje nazewnictwa:**
    - Ujednolicić nazwy tabel i kolumn
    - Migration script do zmiany nazw

---

## 8. CO JEST NIEBEZPIECZNE DŁUGOTERMINOWO

### Problemy bezpieczeństwa:

1. **SessionManager Singleton:**
   - **RYZYKO:** Wyciek danych między użytkownikami
   - **SKUTKI:** Użytkownik A widzi dane użytkownika B
   - **PRIORYTET:** KRYTYCZNY - naprawić natychmiast

2. **Słabe hashowanie:**
   - **RYZYKO:** Łatwe złamanie haseł przy wycieku bazy
   - **SKUTKI:** Kompromitacja kont użytkowników
   - **PRIORYTET:** WYSOKI - migracja haseł

3. **Hardcoded hasła:**
   - **RYZYKO:** Wyciek credentials w repozytorium
   - **SKUTKI:** Dostęp do bazy danych
   - **PRIORYTET:** WYSOKI - usunąć z kodu

4. **Brak walidacji uprawnień:**
   - **RYZYKO:** Nieautoryzowany dostęp do danych
   - **SKUTKI:** Użytkownik może modyfikować dane innych firm
   - **PRIORYTET:** WYSOKI - dodać sprawdzanie uprawnień

5. **Brak izolacji danych:**
   - **RYZYKO:** Zapomnienie filtru `id_firmy` w nowym kodzie
   - **SKUTKI:** Wyciek danych między firmami
   - **PRIORYTET:** ŚREDNI - row-level security lub automatyczne filtrowanie

### Problemy skalowalności:

6. **Brak cache'owania:**
   - **RYZYKO:** Problemy z wydajnością przy wzroście danych
   - **SKUTKI:** Wolna aplikacja, przeciążenie bazy
   - **PRIORYTET:** ŚREDNI - dodać cache

7. **Brak transakcji:**
   - **RYZYKO:** Niespójność danych przy błędach
   - **SKUTKI:** Uszkodzone dane, trudne do naprawy
   - **PRIORYTET:** ŚREDNI - dodać transakcje

8. **Singleton SessionManager:**
   - **RYZYKO:** Nie działa w środowisku multi-instance (load balancer)
   - **SKUTKI:** Błędy w produkcji
   - **PRIORYTET:** WYSOKI - naprawić przed skalowaniem

### Problemy utrzymania:

9. **Logika w repozytoriach:**
   - **RYZYKO:** Trudne w testowaniu i utrzymaniu
   - **SKUTKI:** Wysokie koszty rozwoju
   - **PRIORYTET:** ŚREDNI - refaktoryzacja

10. **Brak testów:**
    - **RYZYKO:** Regresje przy zmianach
    - **SKUTKI:** Błędy w produkcji
    - **PRIORYTET:** ŚREDNI - dodać testy

11. **Mieszanie konwencji:**
    - **RYZYKO:** Trudności w onboarding nowych developerów
    - **SKUTKI:** Więcej błędów, wolniejszy development
    - **PRIORYTET:** NISKI - ujednolicić stopniowo

12. **Brak dokumentacji:**
    - **RYZYKO:** Trudności w zrozumieniu systemu
    - **SKUTKI:** Wysokie koszty utrzymania
    - **PRIORYTET:** NISKI - dodać dokumentację

---

## PODSUMOWANIE

### Mocne strony projektu:

- ✅ Architektura warstwowa
- ✅ Separacja Domain/Application/Infrastructure
- ✅ Użycie DTOs
- ✅ Dependency Injection
- ✅ Encje domenowe z enkapsulacją

### Krytyczne problemy do naprawy:

1. 🔴 **SessionManager jako Singleton** - wyciek danych
2. 🔴 **Słabe hashowanie hasła** - bezpieczeństwo
3. 🔴 **Hardcoded hasła** - bezpieczeństwo
4. 🟠 **Brak walidacji uprawnień** - bezpieczeństwo
5. 🟠 **Brak izolacji warstw** - architektura

### Rekomendacje:

1. **Natychmiast:** Naprawić SessionManager (scoped service)
2. **Wkrótce:** Zaimplementować BCrypt, usunąć hardcoded hasła
3. **W następnej iteracji:** Dodać walidację uprawnień, transakcje
4. **Długoterminowo:** Refaktoryzacja logiki biznesowej, testy, dokumentacja

---

**Data analizy:** 2026-01-26  
**Wersja projektu:** dracoWEB5  
**Analizujący:** AI Assistant
