# Refaktoryzacja: Przeniesienie logiki biznesowej do warstwy Application

## Przegląd zmian

Zrefaktoryzowano architekturę projektu, przenosząc logikę biznesową z repozytoriów do warstwy Application. Repozytoria zawierają teraz tylko operacje CRUD, a logika biznesowa, walidacje i zarządzanie transakcjami znajdują się w warstwie Application.

## Utworzone komponenty

### 1. Unit of Work (Transakcje)

**Lokalizacja:** `ERP.Infrastructure/Services/UnitOfWork.cs`

- **IUnitOfWork** - Interfejs dla zarządzania transakcjami
- **UnitOfWork** - Implementacja dla MySQL
- **Funkcjonalność:**
  - `BeginTransactionAsync()` - Rozpoczyna transakcję
  - `CommitAsync()` - Zatwierdza transakcję
  - `RollbackAsync()` - Cofa transakcję
  - `ExecuteInTransactionAsync<T>()` - Wykonuje operację w transakcji z wartością zwracaną
  - `ExecuteInTransactionAsync()` - Wykonuje operację w transakcji bez wartości zwracanej

**Uwaga:** UnitOfWork jest w warstwie Infrastructure, ponieważ używa `DatabaseContext` i `MySqlTransaction` z tej warstwy.

### 2. Walidatory

**Lokalizacja:** `ERP.Application/Validation/`

#### BaseValidator<T>
- Bazowa klasa dla wszystkich walidatorów
- Zawiera wspólne metody walidacji:
  - `IsNotEmpty()` - Sprawdza czy wartość nie jest pusta
  - `IsNotNull()` - Sprawdza czy wartość nie jest null
  - `IsGreaterThanZero()` - Sprawdza czy wartość jest większa od zera
  - `HasValidLength()` - Sprawdza długość stringa
  - `IsValidEmail()` - Sprawdza format emaila
  - `IsValidNip()` - Sprawdza format NIP

#### CustomerValidator
- Waliduje encję `Customer`
- Sprawdza: nazwę, emaile, NIP, REGON, CompanyId

#### CustomerDtoValidator
- Waliduje DTO `CustomerDto`
- Sprawdza: nazwę, emaile, NIP, CompanyId

### 3. Zaktualizowane repozytoria

#### CustomerRepository
**Zmiany:**
- ✅ Usunięto `GetCurrentCompanyId()` - logika biznesowa przeniesiona do serwisu
- ✅ Usunięto zależność od `IHttpContextAccessor`
- ✅ Wszystkie metody przyjmują `companyId` jako parametr:
  - `GetByIdAsync(int id, int companyId, ...)`
  - `GetByCompanyIdAsync(int companyId, ...)`
  - `GetActiveByCompanyIdAsync(int companyId, ...)`
  - `GetByNameAsync(string name, int companyId, ...)`
  - `DeleteAsync(int id, int companyId, ...)`
  - `ExistsAsync(int id, int companyId, ...)`

**Interfejs:** `ICustomerRepository` - zaktualizowany zgodnie z nowymi sygnaturami

#### OfferRepository
**Zmiany:**
- ✅ Usunięto `GetCurrentCompanyId()`
- ✅ Usunięto zależność od `IHttpContextAccessor`
- ✅ Metody przyjmują `companyId` jako parametr:
  - `GetByIdAsync(int id, int companyId, ...)`
  - `DeleteAsync(int id, int companyId, ...)`
  - `ExistsAsync(int id, int companyId, ...)`
  - `GetNextOfferNumberForDateAsync(int offerDate, int companyId, ...)`

**Interfejs:** `IOfferRepository` - zaktualizowany zgodnie z nowymi sygnaturami

### 4. Zaktualizowane serwisy Application

#### CustomerService
**Zmiany:**
- ✅ Usunięto zależność od `SessionManager` (stary sposób przechowywania kontekstu)
- ✅ Dodano walidacje przed operacjami Create/Update
- ✅ Dodano logikę biznesową:
  - Sprawdzanie duplikatów nazw klientów przed utworzeniem
  - Walidacja DTO i encji przed zapisem
- ✅ Metody przyjmują `companyId` jako parametr:
  - `GetByIdAsync(int id, int companyId, ...)`
  - `GetByCompanyIdAsync(int companyId, ...)`
  - `GetActiveByCompanyIdAsync(int companyId, ...)`
  - `CreateAsync(CustomerDto customerDto, ...)` - companyId z DTO
  - `UpdateAsync(CustomerDto customerDto, ...)` - companyId z DTO
  - `DeleteAsync(int id, int companyId, ...)`

**Interfejs:** `ICustomerService` - zaktualizowany zgodnie z nowymi sygnaturami

### 5. Zaktualizowane kontrolery

#### CustomersController
**Zmiany:**
- ✅ Używa `ICustomerService` zamiast `ICustomerRepository`
- ✅ Pobiera `companyId` z Claims (`User.GetCompanyId()`)
- ✅ Przekazuje `companyId` do serwisu
- ✅ Usunięto mapowanie DTO z kontrolera (mapowanie jest w serwisie)

## Rejestracja w Program.cs

```csharp
// Unit of Work dla transakcji
builder.Services.AddScoped<ERP.Infrastructure.Services.IUnitOfWork, ERP.Infrastructure.Services.UnitOfWork>();

// Validators
builder.Services.AddScoped<ERP.Application.Validation.CustomerValidator>();
builder.Services.AddScoped<ERP.Application.Validation.CustomerDtoValidator>();

// Services
builder.Services.AddScoped<ICustomerService, CustomerService>();
```

## Architektura po refaktoryzacji

### Przed refaktoryzacją:
```
Controller → Repository (logika biznesowa + CRUD) → Database
```

### Po refaktoryzacji:
```
Controller → Service (logika biznesowa + walidacje) → Repository (tylko CRUD) → Database
```

## Zalety nowej architektury

1. **Separacja odpowiedzialności:**
   - Repozytoria: tylko operacje CRUD
   - Serwisy: logika biznesowa, walidacje, transakcje

2. **Testowalność:**
   - Logika biznesowa jest w serwisach, łatwa do testowania jednostkowego
   - Repozytoria są proste i łatwe do mockowania

3. **Centralne walidacje:**
   - Wszystkie walidacje w jednym miejscu
   - Łatwe do utrzymania i rozszerzania

4. **Zarządzanie transakcjami:**
   - UnitOfWork umożliwia zarządzanie transakcjami na poziomie serwisu
   - Gotowe do użycia dla złożonych operacji wymagających wielu operacji na bazie

5. **Niezależność od kontekstu HTTP:**
   - Repozytoria nie zależą od `HttpContext`
   - Można używać repozytoriów poza kontekstem HTTP (np. w testach, jobach)

## Następne kroki

1. **Zaktualizować pozostałe repozytoria:**
   - SupplierRepository
   - ProductRepository
   - OrderRepository
   - OrderMainRepository
   - itd.

2. **Utworzyć serwisy Application dla pozostałych encji:**
   - OfferService
   - OrderService
   - SupplierService
   - ProductService

3. **Zaktualizować kontrolery:**
   - Używać serwisów zamiast repozytoriów
   - Przekazywać companyId z Claims

4. **Rozszerzyć walidacje:**
   - Utworzyć walidatory dla pozostałych encji
   - Dodać bardziej szczegółowe reguły walidacji

5. **Użyć transakcji w złożonych operacjach:**
   - Gdy operacja wymaga wielu operacji na bazie (np. tworzenie oferty z pozycjami)
   - Użyć UnitOfWork.ExecuteInTransactionAsync()

## Uwagi

1. **Backward compatibility:** 
   - Stare metody repozytoriów zostały usunięte lub zmienione
   - Wszystkie wywołania muszą być zaktualizowane

2. **CompanyId jako parametr:**
   - Wszystkie metody repozytoriów wymagają `companyId` jako parametru
   - CompanyId jest pobierane z Claims w kontrolerach i przekazywane do serwisów

3. **Transakcje:**
   - UnitOfWork jest gotowy do użycia, ale dla prostych operacji CRUD nie jest jeszcze używany
   - Będzie używany w złożonych operacjach wymagających wielu operacji na bazie

4. **Walidacje:**
   - Walidacje są wykonywane przed operacjami Create/Update
   - Błędy walidacji są rzucane jako `ArgumentException` z listą błędów
