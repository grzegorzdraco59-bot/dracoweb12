# Audyt Clarion (DCT + kod) vs MariaDB – plan dopasowania

**Cel:** Clarion działa równolegle z C# na tej samej bazie. Jedno źródło ID = MariaDB AUTO_INCREMENT. Brak konfliktów, poprawne kopiowanie nagłówek+pozycje.

**Status:** Brak plików Clarion (.dct, .clw, .inc) w workspace – dokument stanowi szablon audytu i listę poprawek do wykonania w projekcie Clarion.

---

## A) AUDYT SŁOWNIKA (DCT) vs BAZA

### A.1 Zapytanie do weryfikacji aktualnej struktury bazy

Uruchom na locbd przed audytem DCT:

```sql
-- PK i AUTO_INCREMENT dla wszystkich tabel
SELECT t.TABLE_NAME, k.COLUMN_NAME AS pk_column, c.COLUMN_TYPE, c.EXTRA
FROM information_schema.TABLES t
LEFT JOIN information_schema.KEY_COLUMN_USAGE k 
  ON k.TABLE_SCHEMA = t.TABLE_SCHEMA AND k.TABLE_NAME = t.TABLE_NAME AND k.CONSTRAINT_NAME = 'PRIMARY'
LEFT JOIN information_schema.COLUMNS c 
  ON c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.TABLE_NAME = t.TABLE_NAME AND c.COLUMN_NAME = k.COLUMN_NAME
WHERE t.TABLE_SCHEMA = 'locbd' AND t.TABLE_TYPE = 'BASE TABLE'
ORDER BY t.TABLE_NAME;
```

### A.2 Mapowanie: DCT (stare nazwy) ↔ Baza (aktualne)

Na podstawie migracji i raportów. **Sprawdź faktyczny stan** zapytaniem powyżej.

| Tabela w bazie | PK w bazie (po migracji) | Stara nazwa PK (DCT?) | Kolumna FK do nagłówka (pozycje) |
|----------------|--------------------------|------------------------|-----------------------------------|
| firmy | id | id_firmy | – |
| oferty | id | ID_oferta | – |
| ofertypozycje | id | ID_pozycja_oferty | oferta_id lub ID_oferta |
| faktury | id | Id_faktury | – |
| pozycjefaktury | id | id_pozycji_faktury | id_faktury |
| odbiorcy | id | ID_odbiorcy | – |
| dostawcy | id | id_dostawcy | – |
| towary | id | ID_towar | – |
| zamowienia | id | id_zamowienia | – |
| pozycjezamowienia | id | id_pozycji_zamowienia | id_zamowienia |
| zlecenia | id | id_zlecenia | – |
| pozycjezlecenia | id | ID_pozycji_zlecenia | ID_zlecenia / id_zlecenia |
| operator | id (po etap3) lub id_operatora (przed) | id_operatora | – |
| operatorfirma | id | id | – |
| operator_login | id | id | – |
| operator_table_permissions | id | id | – |
| magazyn | **id_magazyn + id_firmy** (composite) | – | **BEZ AUTO_INCREMENT** |
| doc_counters | company_id, doc_type, year, month | – | composite PK |
| pozycjeremanentu | id_pozycji_rem, id_rem | – | composite PK |

### A.3 Rozjazdy nazw PK – opcje korekty

**Opcja 1: Korekta DCT (zalecana)**  
- Zaktualizuj DCT: zmień nazwę pola PK na `id` (lub faktyczną w bazie).  
- Regeneruj aplikację – kod będzie używał nowych nazw.  
- Wymaga przeszukania kodu pod kątem odwołań do starych nazw.

**Opcja 2: Widoki mapujące (jeśli nie chcesz ruszać DCT)**  
- Dla tabel, gdzie baza ma `id`, a Clarion oczekuje `id_firmy` / `id_towar` itd.:  
  utwórz widok `SELECT id AS id_firmy, ... FROM firmy` i podłącz DCT do widoku zamiast tabeli.  
- **Uwaga:** Widoki tylko do SELECT. Dla INSERT/UPDATE Clarion musi pisać do tabeli bazowej – wtedy i tak trzeba dopasować DCT.

**Opcja 3: Aliasy w DCT**  
- W Clarion ABC możesz ustawić „Physical Name” kolumny inaczej niż „Logical Name”.  
- Physical = `id` (baza), Logical = `Id_firmy` (w kodzie).  
- Minimalizuje zmiany w kodzie.

### A.4 Tabele z AUTO_INCREMENT – wymagania DCT

Dla każdej tabeli z AUTO_INCREMENT na PK (wszystkie oprócz magazyn, doc_counters, pozycjeremanentu i tabel bez PK):

| Wymaganie | DCT |
|-----------|-----|
| Primary Key | ✔ Zaznaczone |
| Auto Number / Identity | ✔ Zaznaczone (źródło = baza, nie Clarion) |
| Do Not Reset | ✘ Wyłączone |
| Wypełniane przez aplikację | ✘ Nigdy – PK tylko odczyt po INSERT |
| Clarion Sequence | ✘ None / Off |
| Default | Brak – baza nadaje wartość |

**Lista tabel wymagających poprawy DCT** (jeśli powyższe nie są spełnione):

- oferty (aoferty)
- ofertypozycje (apozycjeoferty)
- faktury
- pozycjefaktury
- odbiorcy (Odbiorcy)
- dostawcy
- towary
- zamowienia
- pozycjezamowienia
- zlecenia
- pozycjezlecenia
- operator
- operatorfirma
- operator_login
- operator_table_permissions
- firmy
- (+ wszystkie pozostałe tabele z prostym PK i AUTO_INCREMENT)

### A.5 Nazwy tabel: aoferty vs oferty

- Jeśli wykonano `RENAME TABLE aoferty TO oferty` – DCT musi wskazywać na `oferty`.  
- Jeśli nie – DCT wskazuje na `aoferty`.  
- Analogicznie: `apozycjeoferty` vs `ofertypozycje`.  
- Sprawdź: `SHOW TABLES LIKE '%oferty%';`

### A.6 Kolumna pozycji → nagłówek

W `ofertypozycje`:
- Po migracji/rename: `oferta_id` (FK logiczny do oferty.id).  
- Stara nazwa: `ID_oferta`.  
- DCT musi używać nazwy zgodnej z bazą. Sprawdź: `SHOW COLUMNS FROM ofertypozycje LIKE '%oferta%';`

---

## B) STANDARD INSERT (w całym projekcie)

### B.1 Wzorzec obowiązkowy

```
PRZED TryInsert / ADD:
  [Tabela].PK = 0

TryInsert([Tabela])  lub  ADD [Tabela]

PO sukcesie:
  Fetch([Tabela], PRIMARY)   ! lub Refresh – pobierz rekord z ID z bazy
  NewId = [Tabela].PK
```

### B.2 Kopiowanie nagłówek → pozycje

```
! Nagłówek
[Hdr].PK = 0
! ... uzupełnij pola ...
ADD [Hdr]
Fetch([Hdr], PRIMARY)
NewHeaderID = [Hdr].PK

! Pozycje – KRYTYCZNE: używaj NewHeaderID, NIE bufora
LOOP przez pozycje źródłowe
  [Pos].PK = 0
  [Pos].ID_oferta = NewHeaderID   ! NIE [Hdr].PK ani AliaHdr:ID_oferta
  ! ... skopiuj pozostałe pola ...
  ADD [Pos]
  Fetch([Pos], PRIMARY)
END
```

### B.3 Zakazy

| Zakaz | Opis |
|-------|------|
| Kopiowanie PK ze źródła | `NowaOferta.Id = StaraOferta.Id` – ZAKAZANE |
| Ręczne liczenie PK | GetNextId(), #NEXT(), liczniki w plikach |
| Przypisywanie PK przed INSERT | `[Tabela].PK = wartość` – ZAKAZANE |
| Używanie bufora zamiast zmiennej | Po kopii nagłówka używaj `NewHeaderID`, nie `[Hdr].PK` (bufor może być nieodświeżony) |

---

## C) KOLEKCJA MIEJSC DO POPRAWY

### C.1 Wzorce wyszukiwania (w projekcie Clarion)

Przeszukaj pliki .clw, .inc, .app:

| Wzorzec | Znaczenie | Akcja |
|---------|-----------|-------|
| `TryInsert(` | INSERT przez ABC | Sprawdź: PK=0 przed, Fetch po |
| `ADD ` + nazwa pliku | INSERT | Sprawdź: PK=0 przed, Fetch po |
| `INSERT` | SQL INSERT | Sprawdź: nie wstawiać PK, pobrać LAST_INSERT_ID() |
| `ID_oferta = ` przed ADD | Przypisanie FK pozycji | Upewnij się, że = NewHeaderID (zmienna), nie ze źródła |
| `ID_faktury = ` przed ADD | j.w. | j.w. |
| `id_zamowienia = ` przed ADD | j.w. | j.w. |
| `ID_odbiorcy = ` | Może być FK lub PK | Kontekst: jeśli to PK nowego rekordu – ZAKAZ |
| `#NEXT(` | Clarion sequence | Usuń, zastąp PK=0 + Fetch |
| `GetNextId` / `NextId` | Ręczny licznik | Usuń |
| `AliaOfe:ID_oferta` w kontekście kopiowania | Odwołanie do aliasu | Zamień na zmienną NewHeaderID po Fetch nagłówka |

### C.2 Szablon listy procedur do poprawy

| Procedura / Embed | Plik | Tabela | Problem | Patch |
|------------------|------|--------|---------|-------|
| (wypełnij po przeszukaniu) | | | | |

### C.3 Minimalne patche (szablony)

**Patch 1: INSERT nagłówka**
```clarion
! BYŁO:
ADD OfertyFile

! JEST:
OfertyFile.id = 0   ! lub ID_oferta jeśli baza ma starą nazwę
! ... uzupełnij pola ...
ADD OfertyFile
IF ERRORCODE() = 0
  Fetch(OfertyFile, PRIMARY)
  NewOfferId = OfertyFile.id
END
```

**Patch 2: INSERT pozycji (nowa pozycja)**
```clarion
! BYŁO:
OfertypozycjeFile.ID_oferta = OfertyFile.ID_oferta

! JEST (przy dodawaniu z formularza):
OfertypozycjeFile.id = 0
OfertypozycjeFile.oferta_id = CurrentOfferId   ! zmienna z kontekstu (Fetch nagłówka)
! ... pola ...
ADD OfertypozycjeFile
IF ERRORCODE() = 0
  Fetch(OfertypozycjeFile, PRIMARY)
END
```

**Patch 3: Kopiowanie oferty z pozycjami**
```clarion
! BYŁO:
COPY OfertyFile TO NewOfertyFile
ADD NewOfertyFile
LOOP przez pozycje
  COPY OfertypozycjeFile TO NewPos
  NewPos.ID_oferta = AliaOfe:ID_oferta
  ADD NewPos
END

! JEST:
NewOfertyFile.id = 0
! ... skopiuj pola (BEZ id) ...
ADD NewOfertyFile
Fetch(NewOfertyFile, PRIMARY)
NewOfferId = NewOfertyFile.id

LOOP przez pozycje
  NewPos.id = 0
  NewPos.oferta_id = NewOfferId   ! zmienna, nie bufor
  ! ... skopiuj pozostałe pola ...
  ADD NewPos
  Fetch(NewPos, PRIMARY)
END
```

---

## D) TABELA MAGAZYN (przypadek specjalny)

### D.1 Struktura w bazie

- **PRIMARY KEY:** `(id_magazyn, id_firmy)` – złożony  
- **Brak AUTO_INCREMENT** na `id_magazyn`  
- Clarion musi sam nadawać `id_magazyn` (np. MAX+1 w ramach `id_firmy`) lub używać innego mechanizmu

### D.2 Zasady

- **NIE** dodawać AUTO_INCREMENT na `id_magazyn` – spowoduje błąd 1062 (duplicate key) przy composite PK.  
- W DCT: PK = `id_magazyn` + `id_firmy`, **bez** Auto Number na `id_magazyn`.  
- Przy INSERT: Clarion musi ustawić obie wartości PK (np. pobrać następny `id_magazyn` dla danej firmy).

### D.3 Wycofanie błędnej migracji

Jeśli ktoś dodał AUTO_INCREMENT na `id_magazyn`:

```sql
ALTER TABLE magazyn MODIFY COLUMN id_magazyn INT(15) NOT NULL;
-- (bez AUTO_INCREMENT)
```

---

## E) WALIDACJA

### E.1 Scenariusze testowe

| # | Scenariusz | Oczekiwany wynik |
|---|------------|------------------|
| 1 | Dodaj nową ofertę w Clarionie | `SELECT id FROM oferty ORDER BY id DESC LIMIT 1` – id zgodne z AUTO_INCREMENT (np. > 1e9 jeśli C# używa wysokich zakresów) |
| 2 | Kopiuj ofertę z pozycjami | Wszystkie pozycje mają `oferta_id` = id nowego nagłówka |
| 3 | Dodaj fakturę | `id` z bazy, pozycje z `id_faktury` = id nagłówka |
| 4 | Dodaj zamówienie z pozycjami | Pozycje mają `id_zamowienia` = id nowego zamówienia |

### E.2 Zapytania kontrolne (tylko do testów – brak FK)

**Sieroty pozycji ofert (pozycje bez nagłówka):**
```sql
SELECT op.id, op.oferta_id
FROM ofertypozycje op
LEFT JOIN oferty o ON o.id = op.oferta_id
WHERE o.id IS NULL;
-- Oczekiwane: 0 wierszy
```

**Sieroty pozycji faktur:**
```sql
SELECT pf.id, pf.id_faktury
FROM pozycjefaktury pf
LEFT JOIN faktury f ON f.id = pf.id_faktury
WHERE f.id IS NULL;
-- Oczekiwane: 0 wierszy
```

**Sieroty pozycji zamówień:**
```sql
SELECT pz.id, pz.id_zamowienia
FROM pozycjezamowienia pz
LEFT JOIN zamowienia z ON z.id = pz.id_zamowienia
WHERE z.id IS NULL;
-- Oczekiwane: 0 wierszy
```

### E.3 Sprawdzenie AUTO_INCREMENT

```sql
SELECT TABLE_NAME, COLUMN_NAME, EXTRA, AUTO_INCREMENT
FROM information_schema.COLUMNS c
JOIN information_schema.TABLES t ON t.TABLE_SCHEMA = c.TABLE_SCHEMA AND t.TABLE_NAME = c.TABLE_NAME
WHERE c.TABLE_SCHEMA = 'locbd'
  AND c.EXTRA LIKE '%auto_increment%'
  AND t.TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
```

---

## WIDOKI (v_kontrahenci, v_operator_permissions_summary)

- **v_operator_permissions_summary** – istnieje w migracjach, używa `o.id AS id_operatora` (po migracji operator.id).  
- **v_kontrahenci** – nie znaleziono w workspace; jeśli istnieje w bazie, upewnij się, że:  
  - jest tylko do SELECT,  
  - nie wymaga FK,  
  - nie generuje ID.

---

## Załącznik: Skrypt audytu bazy

Plik `ERP.Migrations/Scripts/023_ClarionAudytStrukturaBazy.sql` zawiera zapytania do:
- listowania PK i AUTO_INCREMENT dla wszystkich tabel,
- wykrywania złożonych PK,
- weryfikacji nazw kolumn FK w pozycjach,
- listowania widoków.

Uruchom przed audytem DCT.

---

## Kolejność wykonania

1. Uruchom `023_ClarionAudytStrukturaBazy.sql` oraz zapytania z sekcji A.1 – ustal faktyczny stan bazy.  
2. Porównaj z DCT – wypełnij A.2 i A.4.  
3. Wprowadź poprawki w DCT (A.3, A.4).  
4. Przeszukaj kod według C.1 – wypełnij C.2.  
5. Zastosuj patche z C.3.  
6. Zweryfikuj magazyn (D).  
7. Uruchom scenariusze z E.1 i zapytania z E.2.
