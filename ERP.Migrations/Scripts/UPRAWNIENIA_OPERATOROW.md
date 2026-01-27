# System uprawnień operatorów do tabel w bazie danych

## Wprowadzenie

System uprawnień pozwala na kontrolowanie dostępu operatorów do poszczególnych tabel w bazie danych. Każdy operator może mieć różne poziomy dostępu (SELECT, INSERT, UPDATE, DELETE) do różnych tabel.

## Instalacja

1. Uruchom skrypt `011_CreateOperatorTablePermissions.sql` aby utworzyć:
   - Tabelę `operator_table_permissions` - przechowuje uprawnienia
   - Procedury składowane do zarządzania uprawnieniami
   - Widok `v_operator_permissions_summary` - podsumowanie uprawnień

2. (Opcjonalnie) Uruchom skrypt `012_CreateDatabaseUsersForOperators.sql` aby utworzyć:
   - Procedury do tworzenia użytkowników MySQL dla operatorów
   - Automatyczne nadawanie uprawnień GRANT na podstawie `operator_table_permissions`

## Struktura tabeli operator_table_permissions

```sql
CREATE TABLE operator_table_permissions (
    id INT(15) AUTO_INCREMENT PRIMARY KEY,
    id_operatora INT(15) NOT NULL,           -- ID operatora z tabeli operator
    table_name VARCHAR(100) NOT NULL,       -- Nazwa tabeli
    can_select TINYINT(1) NOT NULL DEFAULT 0, -- Uprawnienie SELECT
    can_insert TINYINT(1) NOT NULL DEFAULT 0, -- Uprawnienie INSERT
    can_update TINYINT(1) NOT NULL DEFAULT 0, -- Uprawnienie UPDATE
    can_delete TINYINT(1) NOT NULL DEFAULT 0, -- Uprawnienie DELETE
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (id_operatora) REFERENCES operator(id_operatora) ON DELETE CASCADE,
    UNIQUE KEY unique_operator_table (id_operatora, table_name)
);
```

## Podstawowe operacje

### 1. Ustawianie uprawnień operatora do tabeli

```sql
-- Pełny dostęp (SELECT, INSERT, UPDATE, DELETE)
CALL sp_SetOperatorTablePermission(1, 'Odbiorcy', 1, 1, 1, 1);

-- Tylko odczyt (SELECT)
CALL sp_SetOperatorTablePermission(1, 'towary', 1, 0, 0, 0);

-- Odczyt i wstawianie (SELECT, INSERT)
CALL sp_SetOperatorTablePermission(1, 'magazyn', 1, 1, 0, 0);

-- Odczyt i modyfikacja bez usuwania (SELECT, INSERT, UPDATE)
CALL sp_SetOperatorTablePermission(1, 'operator', 1, 1, 1, 0);
```

**Parametry procedury `sp_SetOperatorTablePermission`:**
- `p_id_operatora` - ID operatora
- `p_table_name` - Nazwa tabeli
- `p_can_select` - 1 = ma uprawnienie SELECT, 0 = nie ma
- `p_can_insert` - 1 = ma uprawnienie INSERT, 0 = nie ma
- `p_can_update` - 1 = ma uprawnienie UPDATE, 0 = nie ma
- `p_can_delete` - 1 = ma uprawnienie DELETE, 0 = nie ma

### 2. Sprawdzanie uprawnień

```sql
-- Sprawdź czy operator ma uprawnienie SELECT
CALL sp_CheckOperatorTablePermission(1, 'Odbiorcy', 'SELECT', @has_permission);
SELECT @has_permission; -- 1 = ma uprawnienie, 0 = nie ma

-- Sprawdź czy operator ma uprawnienie DELETE
CALL sp_CheckOperatorTablePermission(1, 'Odbiorcy', 'DELETE', @has_permission);
SELECT @has_permission;
```

**Dostępne typy uprawnień:** 'SELECT', 'INSERT', 'UPDATE', 'DELETE'

### 3. Pobieranie wszystkich uprawnień operatora

```sql
-- Pobierz wszystkie uprawnienia operatora ID=1
CALL sp_GetOperatorPermissions(1);
```

### 4. Usuwanie uprawnień

```sql
-- Usuń uprawnienia operatora ID=1 do tabeli magazyn
CALL sp_RemoveOperatorTablePermission(1, 'magazyn');
```

### 5. Przeglądanie uprawnień

```sql
-- Wyświetl wszystkie uprawnienia wszystkich operatorów
SELECT * FROM v_operator_permissions_summary;

-- Wyświetl uprawnienia konkretnego operatora
SELECT * FROM v_operator_permissions_summary 
WHERE id_operatora = 1;

-- Wyświetl uprawnienia do konkretnej tabeli
SELECT * FROM v_operator_permissions_summary 
WHERE table_name = 'Odbiorcy';
```

## Przykładowe scenariusze

### Scenariusz 1: Administrator (pełny dostęp)

```sql
-- Operator ID=2 ma pełny dostęp do wszystkich tabel
CALL sp_SetOperatorTablePermission(2, 'Odbiorcy', 1, 1, 1, 1);
CALL sp_SetOperatorTablePermission(2, 'towary', 1, 1, 1, 1);
CALL sp_SetOperatorTablePermission(2, 'magazyn', 1, 1, 1, 1);
CALL sp_SetOperatorTablePermission(2, 'operator', 1, 1, 1, 1);
CALL sp_SetOperatorTablePermission(2, 'operator_login', 1, 1, 1, 1);
CALL sp_SetOperatorTablePermission(2, 'operatorfirma', 1, 1, 1, 1);
```

### Scenariusz 2: Użytkownik tylko do odczytu

```sql
-- Operator ID=3 może tylko przeglądać dane
CALL sp_SetOperatorTablePermission(3, 'Odbiorcy', 1, 0, 0, 0);
CALL sp_SetOperatorTablePermission(3, 'towary', 1, 0, 0, 0);
CALL sp_SetOperatorTablePermission(3, 'magazyn', 1, 0, 0, 0);
```

### Scenariusz 3: Użytkownik z możliwością dodawania i modyfikacji (bez usuwania)

```sql
-- Operator ID=4 może dodawać i modyfikować, ale nie usuwać
CALL sp_SetOperatorTablePermission(4, 'Odbiorcy', 1, 1, 1, 0);
CALL sp_SetOperatorTablePermission(4, 'towary', 1, 1, 1, 0);
CALL sp_SetOperatorTablePermission(4, 'magazyn', 1, 1, 1, 0);
```

### Scenariusz 4: Różne poziomy dostępu do różnych tabel

```sql
-- Operator ID=5 ma różne poziomy dostępu do różnych tabel
CALL sp_SetOperatorTablePermission(5, 'Odbiorcy', 1, 1, 1, 1);  -- Pełny dostęp
CALL sp_SetOperatorTablePermission(5, 'towary', 1, 1, 0, 0);   -- Tylko odczyt i dodawanie
CALL sp_SetOperatorTablePermission(5, 'magazyn', 1, 0, 0, 0);  -- Tylko odczyt
```

## Integracja z użytkownikami MySQL (opcjonalne)

Jeśli chcesz używać natywnych uprawnień MySQL/MariaDB, możesz utworzyć użytkowników bazy danych dla każdego operatora:

### 1. Utworzenie użytkownika MySQL dla operatora

```sql
-- Utwórz użytkownika MySQL (użyj login z tabeli operator_login)
CALL sp_CreateOperatorDatabaseUser(1, 'operator1', 'bezpieczne_haslo_123');
```

### 2. Nadanie uprawnień na podstawie operator_table_permissions

```sql
-- Automatycznie nadaj uprawnienia GRANT do tabel na podstawie operator_table_permissions
CALL sp_GrantTablePermissionsToOperator(1, 'operator1');
```

### 3. Usunięcie użytkownika MySQL

```sql
-- Usuń użytkownika MySQL
CALL sp_DropOperatorDatabaseUser('operator1');
```

### 4. Sprawdzenie użytkowników powiązanych z operatorami

```sql
-- Wyświetl listę operatorów i ich użytkowników MySQL
SELECT * FROM v_operator_database_users;
```

## Użycie w aplikacji C#

W aplikacji C# możesz sprawdzać uprawnienia przed wykonaniem operacji:

```csharp
// Przykład sprawdzenia uprawnienia SELECT
public async Task<bool> HasPermissionAsync(int operatorId, string tableName, string permissionType)
{
    await using var connection = await _context.CreateConnectionAsync();
    var command = new MySqlCommand(
        "CALL sp_CheckOperatorTablePermission(@operatorId, @tableName, @permissionType, @result)",
        connection);
    
    command.Parameters.AddWithValue("@operatorId", operatorId);
    command.Parameters.AddWithValue("@tableName", tableName);
    command.Parameters.AddWithValue("@permissionType", permissionType);
    command.Parameters.Add("@result", MySqlDbType.Int16).Direction = ParameterDirection.Output;
    
    await command.ExecuteNonQueryAsync();
    
    return Convert.ToBoolean(command.Parameters["@result"].Value);
}

// Użycie:
if (await HasPermissionAsync(operatorId, "Odbiorcy", "SELECT"))
{
    // Wykonaj SELECT
}
```

## Uwagi

1. **Bezpieczeństwo**: System uprawnień w tabeli `operator_table_permissions` działa na poziomie aplikacji. Aby używać natywnych uprawnień MySQL, użyj skryptu `012_CreateDatabaseUsersForOperators.sql`.

2. **Wydajność**: Sprawdzanie uprawnień przed każdą operacją może wpłynąć na wydajność. Rozważ cachowanie uprawnień w aplikacji.

3. **Synchronizacja**: Jeśli używasz użytkowników MySQL, pamiętaj o synchronizacji uprawnień po każdej zmianie w `operator_table_permissions`.

4. **Nazwy tabel**: Używaj dokładnych nazw tabel (z uwzględnieniem wielkości liter, jeśli dotyczy).

## Przykłady użycia

Zobacz plik `013_ExampleOperatorPermissions.sql` dla dodatkowych przykładów użycia systemu uprawnień.
