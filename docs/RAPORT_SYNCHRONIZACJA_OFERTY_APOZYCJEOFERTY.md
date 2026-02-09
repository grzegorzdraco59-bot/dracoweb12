# Raport synchronizacji: aoferty / apozycjeoferty

**Data:** 2026-02-05  
**Projekt:** DracoWeb12 (C#)  
**Baza:** MariaDB  
**Źródło struktury:** `SyncDatabase/database_structure.txt` (locbd, 2026-01-25)

---

## 1️⃣ ANALIZA BAZY DANYCH

### 1.1 Tabela aoferty (aktualna struktura z database_structure.txt)

| Kolumna | Typ | Null | Klucz | Domyślna | Uwagi |
|---------|-----|------|-------|----------|-------|
| **ID_oferta** | int(15) | NO | **PRI** | NULL | PK |
| id_firmy | int(15) | NO | | NULL | FK do firmy |
| do_proformy | bit(1) | YES | | NULL | |
| do_zlecenia | bit(1) | YES | | NULL | |
| Data_oferty | int(10) | YES | MUL | NULL | Clarion date |
| Nr_oferty | int(4) | YES | | NULL | |
| odbiorca_ID_odbiorcy | int(11) | YES | | NULL | FK do Odbiorcy |
| odbiorca_nazwa | varchar(100) | YES | MUL | NULL | |
| odbiorca_ulica | varchar(100) | YES | | NULL | |
| odbiorca_kod_poczt | varchar(20) | YES | | NULL | |
| odbiorca_miasto | varchar(100) | YES | | NULL | |
| odbiorca_panstwo | varchar(100) | YES | | NULL | |
| odbiorca_nip | varchar(20) | YES | | NULL | |
| odbiorca_mail | varchar(100) | YES | | NULL | |
| Waluta | varchar(5) | YES | | NULL | |
| Cena_calkowita | decimal(15,2) | YES | | NULL | |
| stawka_vat | decimal(15,4) | YES | | NULL | |
| total_vat | decimal(15,2) | YES | | NULL | |
| total_brutto | decimal(15,2) | YES | | NULL | |
| uwagi_do_oferty | varchar(800) | YES | | NULL | |
| dane_dodatkowe | varchar(800) | YES | | NULL | |
| operator | varchar(50) | NO | | NULL | |
| uwagi_targi | varchar(1000) | NO | | NULL | |
| do_faktury | bit(1) | NO | | b'0' | |
| historia | varchar(50) | NO | | b'0' | |

**Brak w database_structure.txt:** `sum_netto`, `sum_vat`, `sum_brutto`, `status`  
*(mogą być dodane przez migracje: faza4_krok2_status_oferty_zamowienia.sql, oferty_ofertypozycje_add_columns.sql)*

### 1.2 Tabela apozycjeoferty (aktualna struktura)

| Kolumna | Typ | Null | Klucz | Domyślna | Uwagi |
|---------|-----|------|-------|----------|-------|
| **ID_pozycja_oferty** | int(15) | NO | **PRI** | NULL | PK |
| id_firmy | int(15) | NO | | NULL | |
| **ID_oferta** | int(15) | YES | MUL | NULL | **FK → aoferty(ID_oferta)** |
| id_towaru | int(15) | YES | | NULL | |
| id_dostawcy | int(15) | YES | | NULL | |
| kod_towaru | varchar(100) | YES | | NULL | |
| Nazwa | varchar(200) | YES | | NULL | |
| Nazwa_ENG | varchar(200) | YES | | NULL | |
| jednostki | varchar(10) | NO | | NULL | |
| jednostki_en | varchar(10) | YES | | NULL | |
| **Sztuki** | decimal(15,2) | YES | | NULL | *(kod używa: ilosc)* |
| **Cena** | decimal(15,2) | YES | | NULL | *(kod używa: cena_netto)* |
| Rabat | decimal(15,2) | YES | | NULL | |
| Cena_po_rabacie | decimal(15,2) | YES | | NULL | |
| Cena_po_rabacie_i_sztukach | decimal(15,2) | YES | | NULL | |
| stawka_vat | varchar(10) | YES | | NULL | |
| **vat** | decimal(10,4) | YES | | NULL | *(kod używa: vat_poz)* |
| **cena_brutto** | decimal(15,2) | YES | | NULL | *(kod używa: brutto_poz)* |
| Uwagi_oferta | varchar(1240) | YES | | NULL | |
| uwagi_faktura | varchar(500) | YES | | NULL | |
| inne1 | varchar(100) | YES | | NULL | |
| nr_zespolu | decimal(10,2) | YES | | NULL | |

**Brak w database_structure.txt:** `oferta_id`, `ilosc`, `cena_netto`, `netto_poz`, `vat_poz`, `brutto_poz`  
*(apozycjeoferty ma ID_oferta, nie oferta_id; Sztuki/Cena/vat/cena_brutto zamiast ilosc/cena_netto/vat_poz/brutto_poz)*

### 1.3 Różnice względem modeli C# i repozytoriów

| Obszar | Baza (database_structure) | Kod (OfferRepository / OfferPositionRepository) | Status |
|--------|---------------------------|--------------------------------------------------|--------|
| aoferty PK | ID_oferta | id_oferta | ✅ MySQL case-insensitive |
| aoferty sum_* | BRAK | sum_netto, sum_vat, sum_brutto | ⚠️ Wymaga migracji lub kolumny już dodane |
| aoferty status | BRAK | status | ⚠️ Wymaga faza4_krok2 |
| apozycjeoferty PK | ID_pozycja_oferty | id_pozycja_oferty | ✅ |
| apozycjeoferty FK | **ID_oferta** | **oferta_id** | ❌ **KRYTYCZNE** – kod używa oferta_id, baza ma ID_oferta |
| apozycjeoferty ilosc | **Sztuki** | ilosc | ❌ Różne nazwy |
| apozycjeoferty cena_netto | **Cena** | cena_netto | ❌ Różne nazwy |
| apozycjeoferty vat_poz | **vat** | vat_poz | ❌ Różne nazwy |
| apozycjeoferty brutto_poz | **cena_brutto** | brutto_poz | ❌ Różne nazwy |
| apozycjeoferty netto_poz | BRAK | netto_poz | ⚠️ Może być dodane przez migrację |

---

## 2️⃣ MODELE C#

### 2.1 Offer (aoferty)

- **Lokalizacja:** `ERP.Domain/Entities/Offer.cs`
- **Dziedziczy:** BaseEntity (Id, CreatedAt, UpdatedAt)
- **Mapowanie PK:** Id ↔ aoferty.ID_oferta (lub id_oferta)
- **EF Core:** Projekt NIE używa EF Core – brak atrybutów [Table], [Column], [Key] w kontekście ORM

**Wymagania spełnione:**
- PK w modelu = `Id` (BaseEntity)
- Relacja 1:N z OfferPosition (OfferId)
- Wszystkie pola z bazy mają odpowiedniki (poza sum_netto/sum_vat/sum_brutto/status jeśli brak w DB)

### 2.2 OfferPosition (apozycjeoferty)

- **Lokalizacja:** `ERP.Domain/Entities/OfferPosition.cs`
- **Dziedziczy:** BaseEntity
- **Mapowanie PK:** Id ↔ apozycjeoferty.ID_pozycja_oferty
- **Mapowanie FK:** OfferId ↔ apozycjeoferty.ID_oferta (lub oferta_id – zależnie od schematu)

**Uwaga:** Jeśli baza ma `ID_oferta`, repozytorium musi używać `ID_oferta`, nie `oferta_id`.

---

## 3️⃣ MAPOWANIA / DOSTĘP DO DANYCH

### 3.1 Technologia

- **EF Core:** NIE
- **Dapper:** NIE
- **Czysty SQL:** TAK (MySqlConnector)

### 3.2 OfferRepository

- **Tabela:** aoferty
- **PK w WHERE:** id_oferta
- **SELECT:** id_oferta, id_firmy, do_proformy, do_zlecenia, Data_oferty, Nr_oferty, odbiorca_ID_odbiorcy, … sum_netto, sum_vat, sum_brutto, status
- **INSERT:** bez PK (AUTO_INCREMENT)
- **MapToOffer:** reader.GetOrdinal("id_oferta") → Id

### 3.3 OfferPositionRepository

- **Tabela:** apozycjeoferty
- **Problem:** używa `oferta_id` w SELECT/WHERE/INSERT/UPDATE, podczas gdy database_structure pokazuje `ID_oferta`
- **Problem:** używa `ilosc`, `cena_netto`, `netto_poz`, `vat_poz`, `brutto_poz` – baza ma `Sztuki`, `Cena`, `vat`, `cena_brutto` (i brak netto_poz)

### 3.4 JOIN aoferty ↔ apozycjeoferty

- **OrderPositionMainRepository:** `INNER JOIN apozycjeoferty a ON a.id_pozycja_oferty = p.id_pozycji_pozycji_oferty`
- **OfferTotalsService:** `FROM apozycjeoferty WHERE oferta_id = @OfferId` – **oferta_id może nie istnieć w DB**

---

## 4️⃣ LOGIKA APLIKACJI – CHECKLISTA PROBLEMÓW

| # | Miejsce | Problem | Akcja |
|---|---------|---------|-------|
| 1 | OfferPositionRepository | Używa `oferta_id` zamiast `ID_oferta` | Zweryfikować schemat: `SHOW COLUMNS FROM apozycjeoferty` |
| 2 | OfferPositionRepository | Używa `ilosc` zamiast `Sztuki` | Mapowanie Sztuki → Ilosc lub zmiana SQL |
| 3 | OfferPositionRepository | Używa `cena_netto` zamiast `Cena` | Mapowanie Cena → CenaNetto |
| 4 | OfferPositionRepository | Używa `vat_poz`, `brutto_poz` zamiast `vat`, `cena_brutto` | Mapowanie |
| 5 | OfferPositionRepository | Używa `netto_poz` – brak w DB | Sprawdzić czy migracja dodała kolumnę |
| 6 | OfferRepository | Używa `sum_netto`, `sum_vat`, `sum_brutto`, `status` | Sprawdzić czy kolumny istnieją (faza4, oferty_add) |
| 7 | OfferTotalsService | `WHERE oferta_id = @OfferId` | Zmienić na ID_oferta jeśli baza ma ID_oferta |
| 8 | id_oferty → id | Wymaganie PK=id | Model ma Id; mapowanie w reader: id_oferta AS Id lub bezpośrednio |

---

## 5️⃣ UI – BINDINGI

### 5.1 OffersView.xaml

| Binding | Model/DTO | Kolumna w DB | Status |
|---------|-----------|--------------|--------|
| DataOferty, NrOferty | OfferDto | Data_oferty, Nr_oferty | ✅ |
| ForProforma, ForOrder, ForInvoice | OfferDto | do_proformy, do_zlecenia, do_faktury | ✅ |
| Status | OfferDto | status | ⚠️ Kolumna może nie istnieć |
| SumBrutto | OfferDto | sum_brutto | ⚠️ Kolumna może nie istnieć |
| CustomerName, Currency, TotalPrice, TotalVat | OfferDto | odbiorca_nazwa, Waluta, Cena_calkowita, total_vat | ✅ |

### 5.2 Pozycje oferty (DataGrid)

| Binding | Model | Kolumna w DB | Status |
|---------|-------|--------------|--------|
| Name | OfferPositionDto | Nazwa | ✅ |
| Ilosc | OfferPositionDto | Sztuki / ilosc | ⚠️ Zależnie od schematu |
| CenaNetto | OfferPositionDto | Cena / cena_netto | ⚠️ |
| Discount | OfferPositionDto | Rabat | ✅ |
| VatRate | OfferPositionDto | stawka_vat | ✅ |
| BruttoPoz | OfferPositionDto | cena_brutto / brutto_poz | ⚠️ |
| OfferNotes | OfferPositionDto | Uwagi_oferta | ✅ |

**Wszystkie pola używane w UI istnieją w modelu.** Brak bindingów do usuniętych kolumn.

---

## 6️⃣ WALIDACJA – CHECKLISTA TESTÓW

| # | Test | Miejsce do sprawdzenia | Wymaga ręcznego potwierdzenia |
|---|------|------------------------|------------------------------|
| 1 | Lista ofert | OffersViewModel.LoadOffersAsync, OfferRepository.GetByCompanyIdAsync | Czy kolumny sum_*, status istnieją |
| 2 | Otwarcie oferty | OfferRepository.GetByIdAsync, MapToOffer | Czy reader.GetOrdinal("id_oferta") działa |
| 3 | Zapis oferty | OfferRepository.AddAsync, UpdateAsync | Czy INSERT/UPDATE zawiera poprawne kolumny |
| 4 | Lista pozycji oferty | OfferPositionRepository.GetByOfferIdAsync | **WHERE oferta_id vs ID_oferta** |
| 5 | Dodanie pozycji | OfferPositionRepository.AddAsync | **INSERT oferta_id vs ID_oferta** |
| 6 | Edycja pozycji | OfferPositionRepository.UpdateAsync | Mapowanie ilosc↔Sztuki, cena_netto↔Cena |
| 7 | Usunięcie pozycji | OfferPositionRepository.DeleteAsync | WHERE id_pozycja_oferty |
| 8 | Kopiuj do FPF | OfferToFpfConversionService | Zależne od aoferty.id_oferta |
| 9 | Przelicz sum_brutto | OfferTotalsService | apozycjeoferty.oferta_id vs ID_oferta |

---

## 7️⃣ REKOMENDACJE I ROZWIĄZANIA

### Opcja A: Baza ma oryginalny schemat (ID_oferta, Sztuki, Cena, vat, cena_brutto)

**Wymagane zmiany w OfferPositionRepository:**

1. Zamienić `oferta_id` → `ID_oferta` we wszystkich zapytaniach SQL.
2. Mapowanie kolumn:
   - `Sztuki` → Ilosc (AS ilosc w SELECT lub mapowanie w C#)
   - `Cena` → CenaNetto
   - `vat` → VatPoz
   - `cena_brutto` → BruttoPoz
3. `netto_poz`: jeśli brak w DB – obliczać w C# (ComputePositionAmounts) i nie zapisywać do DB, lub dodać kolumnę migracją.

### Opcja B: Baza została zmigrowana (oferta_id, ilosc, cena_netto, netto_poz, vat_poz, brutto_poz)

Wtedy aktualny kod jest zgodny. Należy to potwierdzić przez:

```sql
SHOW COLUMNS FROM apozycjeoferty;
SHOW COLUMNS FROM aoferty;
```

### Opcja C: Ujednolicenie PK do `id` (bez zmiany bazy)

Zadanie mówi „PK ujednolicone do id”. W modelu C# już jest `Id`. Mapowanie:
- aoferty: `SELECT id_oferta AS Id` lub `SELECT ID_oferta AS Id` – wartość trafia do `Offer.Id`
- apozycjeoferty: `SELECT id_pozycja_oferty AS Id` – wartość trafia do `OfferPosition.Id`

**Nie zmieniamy nazw kolumn w bazie** – tylko aliasujemy w SELECT.

---

## 8️⃣ LISTA PLIKÓW DO POTENCJALNEJ MODYFIKACJI

| Plik | Zmiany |
|------|--------|
| `ERP.Infrastructure/Repositories/OfferPositionRepository.cs` | SQL: oferta_id↔ID_oferta, ilosc↔Sztuki, cena_netto↔Cena, vat_poz↔vat, brutto_poz↔cena_brutto |
| `ERP.Infrastructure/Repositories/OfferRepository.cs` | Ewent. fallback gdy brak sum_*, status (GetOrdinal w try-catch) |
| `ERP.Infrastructure/Services/OfferTotalsService.cs` | oferta_id → ID_oferta jeśli baza ma ID_oferta |
| `ERP.Infrastructure/Repositories/OrderPositionMainRepository.cs` | Sprawdzić JOIN – obecnie id_pozycja_oferty |
| `ERP.Application/DTOs/OfferPositionDto.cs` | Komentarze: ID_oferta vs oferta_id |
| `ERP.Domain/Entities/Offer.cs` | Komentarz: PK id_oferta / ID_oferta |
| `ERP.Domain/Entities/OfferPosition.cs` | Komentarz: PK id_pozycja_oferty, FK ID_oferta |

---

## 9️⃣ POTENCJALNE RYZYKA

| Ryzyko | Opis | Mitygacja |
|--------|------|-----------|
| R1 | Różne środowiska (locbd vs prod) mają różny schemat | Uruchomić `SHOW COLUMNS` na docelowej bazie przed wdrożeniem |
| R2 | Migracje faza4, oferty_add mogły nie być wykonane | Sprawdzić istnienie kolumn status, sum_*, netto_poz, vat_poz, brutto_poz |
| R3 | LAST_INSERT_ID() po INSERT do aoferty/apozycjeoferty | Działa dla AUTO_INCREMENT niezależnie od nazwy kolumny PK |
| R4 | Wielkość liter (ID_oferta vs id_oferta) | MySQL na Windows: case-insensitive dla identyfikatorów |
| R5 | Zamiana oferta_id↔ID_oferta | Może wymagać aktualizacji innych tabel (np. pozycjezamowienia.id_pozycji_pozycji_oferty) |

---

## 🔟 KROK NASTĘPNY

**Przed wprowadzeniem zmian wykonaj na docelowej bazie:**

```sql
SHOW COLUMNS FROM aoferty;
SHOW COLUMNS FROM apozycjeoferty;
```

Porównaj wynik z `database_structure.txt` i z decyzjami w sekcji 7 (Opcja A/B/C).
