# ETAP 3 – Plan migracji PK → id (KLASA C)

**Baza:** locbd (MariaDB/MySQL)  
**Data:** 2026-02-03  
**Status:** Plan i SQL wygenerowane – NIE WYKONYWAĆ automatycznie

---

## 1️⃣ Definicja klasy C

**KLASA C** = tabele RDZEŃ/RODZICE: `incoming_fk>0` (inne tabele wskazują na nie przez FK).

- Tabela jest referencjonowana przez co najmniej jedną tabelę dziecka
- Migracja wymaga: DROP FK w dzieciach → CHANGE PK w rodzicu → ADD FK w dzieciach (REFERENCED_COLUMN = id)

---

## 2️⃣ Lista tabel KLASA C (z analizy)

| Tabela (rodzic) | Aktualny PK | Tabele dzieci (incoming FK) | Wymaga migracji |
|-----------------|-------------|-----------------------------|-----------------|
| **faktury** | id | pozycjefaktury (faktura_id→id) | ❌ PK już = id |
| **firmy** | id | doc_counters (company_id→id) | ❌ PK już = id |
| **oferty** | id | ofertypozycje (oferta_id→id) | ❌ PK już = id |
| **operator** | id_operatora | operator_table_permissions (id_operatora→id_operatora) | ✅ TAK |

---

## 3️⃣ Szczegóły: tabele dzieci (FK)

### faktury
| Tabela dziecka | Kolumna FK | CONSTRAINT | REFERENCED_COLUMN | DELETE | UPDATE |
|----------------|------------|------------|-------------------|--------|--------|
| pozycjefaktury | faktura_id | fk_pozycjefaktury_faktury | id | RESTRICT | RESTRICT |

### firmy
| Tabela dziecka | Kolumna FK | CONSTRAINT | REFERENCED_COLUMN | DELETE | UPDATE |
|----------------|------------|------------|-------------------|--------|--------|
| doc_counters | company_id | FK_doc_counters_firmy | id | RESTRICT | RESTRICT |

### oferty
| Tabela dziecka | Kolumna FK | CONSTRAINT | REFERENCED_COLUMN | DELETE | UPDATE |
|----------------|------------|------------|-------------------|--------|--------|
| ofertypozycje | oferta_id | fk_ofertypozycje_oferty | id | RESTRICT | RESTRICT |

### operator (WYMAGA MIGRACJI)
| Tabela dziecka | Kolumna FK | CONSTRAINT | REFERENCED_COLUMN | DELETE | UPDATE |
|----------------|------------|------------|-------------------|--------|--------|
| operator_table_permissions | id_operatora | operator_table_permissions_ibfk_1 | id_operatora | CASCADE | RESTRICT |

---

## 4️⃣ Plan migracji – tylko operator

**Kolejność:** operator (jedyna tabela do migracji)

### Kroki dla operator:

1. **a) DROP FK w dzieciach**
   - `ALTER TABLE operator_table_permissions DROP FOREIGN KEY operator_table_permissions_ibfk_1;`

2. **b) Zmiana PK w rodzicu**
   - `ALTER TABLE operator CHANGE COLUMN id_operatora id int(15) NOT NULL AUTO_INCREMENT;`
   - (zachowaj typ, NOT NULL, AUTO_INCREMENT, PRIMARY KEY)

3. **c) Odtworzenie FK w dzieciach**
   - Kolumna FK w dziecku: `id_operatora` (bez zmiany nazwy)
   - REFERENCED_COLUMN: `id` (nowy PK rodzica)
   - `ALTER TABLE operator_table_permissions ADD CONSTRAINT fk_operator_table_permissions_operator FOREIGN KEY (id_operatora) REFERENCES operator(id) ON DELETE CASCADE ON UPDATE RESTRICT;`

4. **d) Walidacja**
   - COUNT(*) operator, operator_table_permissions
   - Sprawdzenie PK w operator (id)
   - Sprawdzenie FK w operator_table_permissions

---

## 5️⃣ Tabele POMINIĘTE

| Tabela | Powód |
|--------|-------|
| faktury | PK już = id |
| firmy | PK już = id |
| oferty | PK już = id |

**Brak:** composite PK, brak PK, nietypowy PK w klasie C.

---

## 6️⃣ Cykle FK / Self-FK

**Brak wykrytych** cykli ani self-FK w tabelach klasy C.

---

## 7️⃣ Pliki

| Plik | Opis |
|------|------|
| `ERP.Infrastructure/Sql/etap3_analiza_klasa_C.sql` | Analiza PK i FK – uruchom PRZED migracją |
| `ERP.Infrastructure/Sql/etap3_klasa_C_pk_do_id.sql` | Skrypt migracji (NIE wykonuj automatycznie) |
| `docs/ETAP3_PLAN_MIGRACJI_KLASA_C.md` | Ten dokument |
| `docs/ETAP3_ANALIZA_WYNIK.txt` | Wynik analizy (dotnet run --project SyncDatabase etap3) |

---

## 8️⃣ Kolejność wykonania

1. **Backup bazy** – pełny dump locbd
2. **Analiza:** `dotnet run --project SyncDatabase -- etap3` lub `mysql < etap3_analiza_klasa_C.sql`
3. **Migracja:** Wykonuj bloki z `etap3_klasa_C_pk_do_id.sql` po kolei, waliduj po każdym
