# Insert record – wzorzec Clarion (INSERT + LAST_INSERT_ID)

## Wymagania

1. Przy kliknięciu „Dodaj / Insert record”: INSERT bez podawania ID (AUTO_INCREMENT).
2. Natychmiast po INSERT: `SELECT LAST_INSERT_ID();` – pobierz ID z serwera.
3. Zapisz zwrócone ID do zmiennej `newId`.
4. Użyj `newId` do: UPDATE (wartości domyślne), dalszej edycji w UI, rekordów zależnych.
5. **NIE** generuj ID lokalnie.
6. **NIE** używaj `MAX(id)+1` dla klucza głównego.

## Wspólna metoda: InsertAndGetId()

**Lokalizacja:** `ERP.Infrastructure/Data/MySqlCommandExtensions.cs`

```csharp
/// <summary>
/// Wykonuje polecenie SQL zawierające INSERT oraz SELECT LAST_INSERT_ID(); w jednej transakcji.
/// Zwraca nowe ID (AUTO_INCREMENT) z serwera. NIE generuje ID lokalnie.
/// Wymagane: command.CommandText musi kończyć się na "; SELECT LAST_INSERT_ID();"
/// Wzorzec Clarion "Insert record": INSERT bez ID → pobierz LAST_INSERT_ID() → użyj newId do UPDATE/edycji.
/// </summary>
public static async Task<long> ExecuteInsertAndGetIdAsync(
    this MySqlCommand command,
    CancellationToken cancellationToken = default)
{
    if (command.Connection == null)
        throw new InvalidOperationException("Command must have a connection.");

    await using var transaction = await command.Connection.BeginTransactionAsync(cancellationToken);
    command.Transaction = transaction;
    try
    {
        var result = await command.ExecuteScalarWithDiagnosticsAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Convert.ToInt64(result ?? 0);
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}
```

## Finalny kod INSERT + pobranie ID

**Przykład – OfferRepository.AddAsync:**

```csharp
public async Task<int> AddAsync(Offer offer, CancellationToken cancellationToken = default)
{
    await using var connection = await _context.CreateConnectionAsync();
    var command = new MySqlCommand(
        "INSERT INTO aoferty (id_firmy, do_proformy, do_zlecenia, Data_oferty, Nr_oferty, " +
        "odbiorca_ID_odbiorcy, odbiorca_nazwa, ...) " +
        "VALUES (@CompanyId, @ForProforma, @ForOrder, @OfferDate, @OfferNumber, @CustomerId, ...); " +
        "SELECT LAST_INSERT_ID();",
        connection);

    AddOfferParameters(command, offer);

    var newId = await command.ExecuteInsertAndGetIdAsync(cancellationToken);
    return (int)newId;
}
```

**Przykład – OfferPositionEditViewModel (UI):**

```csharp
if (isNewPosition)
{
    var newId = await _offerService.AddPositionAsync(position);
    _position.Id = (long)newId;
}
```

**Przykład – OffersViewModel.AddOfferAsync:**

```csharp
var id = await _offerService.AddAsync(offer);
var createdOffer = await _offerService.GetByIdAsync(id, companyId);
// ... odświeżenie listy, zaznaczenie nowej oferty, otwarcie edycji
```

## Repozytoria używające InsertAndGetId()

| Repozytorium | Metoda AddAsync |
|--------------|-----------------|
| OfferRepository | `ExecuteInsertAndGetIdAsync` |
| OfferPositionRepository | `ExecuteInsertAndGetIdAsync` |
| CustomerRepository | `ExecuteInsertAndGetIdAsync` |
| SupplierRepository | `ExecuteInsertAndGetIdAsync` |
| UserRepository | `ExecuteInsertAndGetIdAsync` |
| RoleRepository | `ExecuteInsertAndGetIdAsync` |
| OrderMainRepository | `ExecuteInsertAndGetIdAsync` |
| OrderRepository | `ExecuteInsertAndGetIdAsync` |
| OrderPositionMainRepository | `ExecuteInsertAndGetIdAsync` |
| CompanyRepository | `ExecuteInsertAndGetIdAsync` |
| UserLoginRepository | `ExecuteInsertAndGetIdAsync` |
| UserCompanyRepository | `ExecuteInsertAndGetIdAsync` |
| OperatorCompanyRepository | `ExecuteInsertAndGetIdAsync` |

## Gdzie NIE było liczenia ID lokalnie (potwierdzenie)

W kodzie aplikacji **nie znaleziono** miejsc, gdzie:
- generowano ID lokalnie (np. `id = Guid.NewGuid()` lub `id = nextId++`),
- używano `MAX(id)+1` dla **klucza głównego** (AUTO_INCREMENT).

**Uwaga:** `OfferRepository.GetNextOfferNumberForDateAsync` używa `MAX(Nr_oferty)+1` – to dotyczy **numeru oferty** (Nr_oferty), nie klucza głównego. Numer oferty to pole biznesowe (np. "OF-2025-001"), nie PK.

**Skrypt migracyjny** `ERP.Migrations/Scripts/014_SyncOperatorFromLocalbddraco.sql` używa `MAX(id)` do synchronizacji – to logika migracji, nie logika biznesowa Insert record.

## Przepływ „Insert record”

1. Użytkownik klika „Dodaj” → ViewModel tworzy encję z Id=0 (lub bez ustawiania Id).
2. Serwis/repozytorium wywołuje `AddAsync(entity)`.
3. Repozytorium: `INSERT INTO tabela (kolumny...) VALUES (...); SELECT LAST_INSERT_ID();`
4. `ExecuteInsertAndGetIdAsync` wykonuje w transakcji, zwraca `newId`.
5. ViewModel: `_position.Id = (long)newId` – zapisuje ID do DTO.
6. Dalsza edycja / UPDATE / rekordy zależne używają `newId`.
