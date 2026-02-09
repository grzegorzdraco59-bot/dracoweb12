# Logowanie WPF – propozycja docelowego rozwiązania

**Status:** Propozycja – NIE zmieniać schematu bazy bez wyraźnego polecenia.

**Obecny stan:** Logowanie działa w trybie testowym (login = ID operatora) oraz przez `operator_login` (login + hasło).

---

## Opcja A: Dodanie pola `login` do tabeli `operator`

### Zalety
- Jeden rekord = jeden operator z loginem
- Prostsze zapytania – bez JOIN do `operator_login`
- Spójność z Clarionem, jeśli tam też będzie `login` w operatorze

### Wady
- Wymaga migracji bazy
- Duplikacja danych, jeśli `operator_login` nadal istnieje (login w dwóch miejscach)
- Konieczna migracja istniejących loginów z `operator_login` do `operator`

### Schemat (do wykonania ręcznie, gdy zatwierdzone)

```sql
-- 1) Dodanie kolumny
ALTER TABLE `operator` ADD COLUMN login VARCHAR(100) NULL UNIQUE AFTER imie_nazwisko;

-- 2) Migracja istniejących loginów z operator_login
UPDATE `operator` o
INNER JOIN operator_login ol ON ol.id_operatora = o.id
SET o.login = ol.login
WHERE o.login IS NULL;

-- 3) Opcjonalnie: indeks dla wyszukiwania
CREATE INDEX idx_operator_login ON `operator`(login);
```

### Zmiany w C#
- `UserRepository.GetByLoginAsync(string login)` – zapytanie `FROM operator WHERE login = @Login`
- Usunięcie zależności od `operator_login` w ścieżce logowania (lub pozostawienie jako fallback)

---

## Opcja B: Widok `operator_v`

### Zalety
- Brak zmian w tabelach bazowych
- Łączy `operator` + `operator_login` w jeden „virtual” rekord
- Można dodać aliasy kolumn (np. `id AS id_operatora` dla kompatybilności)

### Wady
- Widok tylko do SELECT – INSERT/UPDATE muszą iść do tabel bazowych
- Logika logowania nadal wymaga `operator_login` dla hasła

### Schemat (do wykonania ręcznie, gdy zatwierdzone)

```sql
CREATE OR REPLACE VIEW operator_v AS
SELECT
    o.id,
    o.id AS id_operatora,  -- alias dla kompatybilności
    o.id_firmy,
    o.imie_nazwisko,
    ol.login,
    o.uprawnienia,
    o.senderEmail,
    o.senderUserName,
    o.senderEmailServer,
    o.senderEmailPassword,
    o.messageText,
    o.ccAdresse
FROM `operator` o
LEFT JOIN operator_login ol ON ol.id_operatora = o.id;
```

### Zmiany w C#
- Dla **odczytu** (np. lista operatorów z loginem): `SELECT ... FROM operator_v`
- Dla **logowania**: bez zmian – nadal `operator_login` (login + hasło) → `operator` (GetById)
- Widok nie zastępuje logowania, tylko ułatwia wyświetlanie

---

## Opcja C: Zachowanie obecnego modelu (operator + operator_login)

### Zalety
- Brak migracji
- Rozdzielenie: dane operatora vs dane logowania
- Już działa (testowo po ID, normalnie po loginie z operator_login)

### Wady
- Dwa miejsca do utrzymania
- Logowanie wymaga JOIN lub dwóch zapytań

### Rekomendacja
Jeśli logowanie działa – **zostawić jak jest**. Opcje A/B rozważyć tylko przy refaktoryzacji schematu.

---

## Podsumowanie

| Opcja | Zmiana bazy | Złożoność | Użycie |
|-------|-------------|-----------|--------|
| A – login w operator | TAK | Średnia | Jeden rekord, prostsze zapytania |
| B – widok operator_v | TAK (tylko VIEW) | Niska | Głównie do odczytu/list |
| C – status quo | NIE | – | Obecne rozwiązanie |

**Nie wykonuj żadnych zmian w bazie bez wyraźnego polecenia.**
