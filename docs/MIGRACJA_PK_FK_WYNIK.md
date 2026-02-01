# Migracja PK/FK – standard id i <tabela>_id

## Zasada globalna

- **Każda tabela główna:** PRIMARY KEY = `id` (BIGINT, AUTO_INCREMENT gdzie potrzeba).
- **Każda tabela zależna:** FK = `<tabela>_id` (np. `faktura_id`, `oferta_id`).
- **Kolejność:** najpierw migracja danych (ETAP 1–2 SQL), potem zmiany w kodzie WPF.
- **Web:** niezmieniany. **Logika biznesowa:** bez zmian (tylko nazwy kolumn w SQL i mapowanie).

---

## ETAP 0 – Audyt (wykonany z kodu)

| Tabela | Stary PK | Nowy PK | Uwagi |
|--------|----------|---------|--------|
| oferty | id | id | Już poprawny |
| faktury | Id_faktury | id | Migracja w ETAP 1 |
| pozycjefaktury | id_pozycji_faktury | id | + FK faktura_id |
| ofertypozycje | ID_pozycja_oferty | id | oferta_id już jest |
| Odbiorcy | ID_odbiorcy | id | |
| dostawcy | id_dostawcy | id | |
| rola | id_roli | id | |
| towary | ID_towar | id | |
| zamowienia | id / id_zamowienia | id | Sprawdzić SHOW COLUMNS |
| pozycjezamowienia | id_pozycji_zamowienia | id | |
| operatorfirma | id | id | Bez zmian |
| operator_login | id | id | Bez zmian |

Szczegóły: `docs/AUDYT_PK_FK_ETAP0.md`.

---

## SQL migracyjny (krok po kroku)

### ETAP 1 – Migracja PK na `id`

**Plik:** `ERP.Infrastructure/Sql/migrate_pk_to_id_etap1.sql`

- Dla każdej tabeli: `ADD COLUMN id BIGINT NULL` → `UPDATE ... SET id = stary_PK` → `MODIFY id BIGINT NOT NULL` → `DROP PRIMARY KEY, ADD PRIMARY KEY (id)`.
- Dla `faktury`: po ustawieniu PK dodane jest `AUTO_INCREMENT` na `id` (nowe wiersze).
- Starych kolumn PK (np. `Id_faktury`, `id_pozycji_faktury`) **nie usuwać** na tym etapie.

Uruchomić w kliencie SQL (HeidiSQL, DBeaver, mysql) na kopii bazy; sprawdzić brak FK blokujących DROP PRIMARY KEY.

### ETAP 2 – Migracja FK na `<tabela>_id`

**Plik:** `ERP.Infrastructure/Sql/migrate_fk_etap2.sql`

- `pozycjefaktury`: dodanie `faktura_id`, backfill z `id_faktury` (JOIN `faktury.id`), `MODIFY faktura_id BIGINT NOT NULL`.
- `faktury`: opcjonalnie `oferta_id` (backfill z `id_oferty`).
- Starych kolumn FK (np. `id_faktury`) **nie usuwać** na tym etapie.

**Kolejność wdrożenia:** 1) ETAP 1, 2) ETAP 2, 3) wdrożenie kodu WPF.

---

## ETAP 3 – Klucze obce (opcjonalnie, na końcu)

Gdy dane są poprawne, można dodać:

```sql
ALTER TABLE pozycjefaktury
ADD CONSTRAINT fk_pozycje_faktury
FOREIGN KEY (faktura_id) REFERENCES faktury(id);
```

Analogicznie dla innych relacji. **Nie wykonywane w tym zestawie** – do zrobienia po testach.

---

## ETAP 4 – Dostosowanie kodu (WPF + Infra)

### Zmienione pliki

| Plik | Zmiana |
|------|--------|
| **ERP.Infrastructure/Repositories/InvoiceRepository.cs** | SELECT/ORDER po `id` zamiast `Id_faktury`; MapToDto: `Id` z kolumny `id`. |
| **ERP.Infrastructure/Repositories/InvoicePositionRepository.cs** | SELECT/WHERE/ORDER po `COALESCE(id, id_pozycji_faktury)` i `COALESCE(faktura_id, id_faktury)`; MapToDto: `id`, `faktura_id`. |
| **ERP.Infrastructure/Services/InvoiceTotalsService.cs** | WHERE/UPDATE po `id` (faktury) i `COALESCE(faktura_id, id_faktury)` (pozycjefaktury). |
| **ERP.Infrastructure/Services/OfferToFpfConversionService.cs** | SELECT `id` z faktury; JOIN/UPDATE po `f.id`; INSERT do pozycjefaktury z `faktura_id` + `id_faktury`. |
| **ERP.Infrastructure/Sql/backfill_faktury_pozycje_i_naglowki.sql** | JOIN/UPDATE nagłówków po `f.id` i `COALESCE(faktura_id, id_faktury)`. |
| **ERP.Infrastructure/Sql/migrate_pk_to_id_etap1.sql** | Nowy – dodanie `id` i PK; AUTO_INCREMENT na `faktury.id`. |
| **ERP.Infrastructure/Sql/migrate_fk_etap2.sql** | Nowy – dodanie `faktura_id` (i opcjonalnie `oferta_id`), backfill. |
| **docs/AUDYT_PK_FK_ETAP0.md** | Nowy – audyt tabel i PK. |
| **docs/MIGRACJA_PK_FK_WYNIK.md** | Ten raport. |

### Nie zmieniane

- **ERP.UI.Web** – brak zmian.
- **DTO/Entity:** właściwości `Id`, `InvoiceId` bez zmian; wartość `Id` pochodzi z kolumny `id`, `InvoiceId` z `faktura_id`.
- **WPF ViewModels/Binding:** używają `SelectedInvoice.Id`, `SelectedOffer.Id` – bez zmian.
- **Oferty, ofertypozycje, Odbiorcy, dostawcy, rola, towary, zamowienia, pozycjezamowienia** – w tym kroku zmienione są tylko **faktury** i **pozycjefaktury** oraz powiązane serwisy/SQL. Pozostałe tabele mają skrypty w ETAP 1–2; repozytoria można przełączyć na `id` / `<tabela>_id` w kolejnej iteracji.

---

## ETAP 5 – Sprzątanie (nie w tym zestawie)

- Usuwanie starych kolumn PK/FK (np. `Id_faktury`, `id_faktury`) – **nie wykonywane**.
- Do rozważenia po ustabilizowaniu działania aplikacji.

---

## ETAP 6 – Testy

1. Aplikacja WPF startuje.
2. Otwierają się: Oferty, Faktury.
3. Lista faktur ładuje się (SELECT po `id`).
4. Pozycje wybranej faktury ładują się (WHERE po `COALESCE(faktura_id, id_faktury)`).
5. Kopiowanie oferty do FPF tworzy fakturę i pozycje (INSERT z `faktura_id`).
6. Przeliczenia sum faktury działają (InvoiceTotalsService po `id` / `faktura_id`).
7. Brak błędów „Unknown column” (wymaga wykonania ETAP 1 i ETAP 2 przed wdrożeniem kodu).

---

## Potwierdzenie

- Standard **PK = id** jest wprowadzony dla **faktury** i **pozycjefaktury** (w SQL i w kodzie).
- Relacja **pozycjefaktury → faktury** używa **faktura_id** (z zachowaniem odczytu `id_faktury` przez COALESCE do czasu ewentualnego sprzątania).
- Skrypty ETAP 1 i ETAP 2 przygotowane także pod inne tabele (Odbiorcy, dostawcy, rola, towary, ofertypozycje, pozycjezamowienia); po ich uruchomieniu można analogicznie zmienić repozytoria.
