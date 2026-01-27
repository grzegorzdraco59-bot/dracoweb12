# Skrypty migracji bazy danych

## Instrukcja użycia

1. Połącz się z bazą danych MariaDB/MySQL
2. Wybierz bazę danych: `USE locbd;`
3. Uruchom skrypty w kolejności numeracji:
   - `001_CreateOdbiorcyTable.sql` - tworzy tabelę Odbiorcy
   - `002_CreateOperatorLoginTable.sql` - tworzy tabelę operator_login
   - `004_CreateOperatorFirmaTable.sql` - tworzy tabelę operatorfirma
   - `009_FixOperatorPrimaryKey.sql` - naprawia PRIMARY KEY w tabeli operator
   - `011_CreateOperatorTablePermissions.sql` - tworzy system uprawnień operatorów do tabel
   - `012_CreateDatabaseUsersForOperators.sql` - tworzy użytkowników MySQL dla operatorów (opcjonalne)
   - `013_ExampleOperatorPermissions.sql` - przykłady użycia systemu uprawnień

## System uprawnień operatorów

Zobacz dokumentację w pliku `UPRAWNIENIA_OPERATOROW.md` dla szczegółowych informacji o zarządzaniu uprawnieniami operatorów do poszczególnych tabel.

## Uwagi

- Przed uruchomieniem skryptów upewnij się, że masz odpowiednie uprawnienia
- Skrypty używają `CREATE TABLE IF NOT EXISTS`, więc można je uruchomić wielokrotnie bezpiecznie
- Wszystkie tabele używają InnoDB engine i utf8mb4 charset dla pełnego wsparcia Unicode
- Skrypt `012_CreateDatabaseUsersForOperators.sql` wymaga uprawnień administratora (root)
