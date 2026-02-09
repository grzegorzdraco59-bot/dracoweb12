# ETAP 2 – Plan migracji PK → id (KLASA B)

**Baza:** locbd (MariaDB/MySQL)  
**Data:** 2026-02-03  
**Status:** Oczekuje na akceptację

---

## 1️⃣ Definicja klasy B

**KLASA B** = tabele LIŚCIE/DZIECI: `incoming_fk=0` oraz `outgoing_fk>0`.

- Brak tabel referencjonujących daną tabelę (incoming=0)
- Tabela ma FK wychodzące do innych tabel (outgoing>0)

---

## 2️⃣ Lista tabel KLASA B (z raportu RAPORT_FK_PK_KLASY.txt)

| Tabela | PK (obecna) | Typ | FK wychodzące | Uwagi |
|--------|-------------|-----|---------------|-------|
| doc_counters | company_id, doc_type, year, month | composite | 1 (company_id→firmy) | **POMIŃ** – composite PK |
| ofertypozycje | ID_pozycja_oferty lub id | int(15) | 1 (ID_oferta→aoferty) | Migracja PK |
| operator_table_permissions | id | int(15) | 1 (id_operatora→operator) | **Bez zmian** – PK już = id |
| pozycjefaktury | id_pozycji_faktury lub id | int(15) | 1+ | Migracja PK |

---

## 3️⃣ Tabele do migracji

### ofertypozycje (lub apozycjeoferty)

- **PK:** ID_pozycja_oferty → id
- **FK:** DROP fk_apozycjeoferty_aoferty → CHANGE COLUMN → ADD fk_ofertypozycje_oferty
- **Uwaga:** Jeśli tabela nazywa się `apozycjeoferty`, zamień nazwy w skrypcie. Po rename aoferty→oferty: FK (oferta_id) REFERENCES oferty(id).

### operator_table_permissions

- **PK:** id (już poprawny)
- **Akcja:** Tylko walidacja – brak zmian

### pozycjefaktury

- **PK:** id_pozycji_faktury → id
- **FK:** Uruchom `etap2_analiza_klasa_B.sql` – pobierz nazwy CONSTRAINT i definicje. Jeśli brak FK w bazie (tylko kolumny), wykonaj tylko CHANGE COLUMN.

---

## 4️⃣ Tabele POMINIĘTE

| Tabela | Powód |
|--------|-------|
| doc_counters | Composite PK (company_id, doc_type, year, month) |

---

## 5️⃣ Zasady bezpieczeństwa

- Nie ruszaj tabel spoza listy klasy B
- Nie zmieniaj żadnych FK w innych tabelach (incoming_fk=0)
- Nie usuwaj danych, nie rób DROP TABLE
- Jeśli tabela ma composite PK albo brak PK → pomiń i opisz w raporcie
- Jeśli incoming_fk>0 (sprzeczność) → oznacz jako **KLASA C**, przerwij

---

## 6️⃣ Pliki

| Plik | Opis |
|------|------|
| `ERP.Infrastructure/Sql/etap2_analiza_klasa_B.sql` | Analiza PK, FK, incoming_fk – uruchom PRZED migracją. Sekcja 6 generuje DROP/ADD FK. |
| `ERP.Infrastructure/Sql/etap2_klasa_B_pk_do_id.sql` | Skrypt migracji (NIE wykonuj automatycznie). Wykonuj blok po bloku. |
| `docs/ETAP2_PLAN_MIGRACJI_KLASA_B.md` | Ten dokument |

---

## 7️⃣ Kolejność wykonania

1. **Backup bazy** – pełny dump locbd
2. **Analiza:** `mysql -u user -p locbd < ERP.Infrastructure/Sql/etap2_analiza_klasa_B.sql`
3. **Weryfikacja:** Sprawdź incoming_fk=0, zapisz nazwy FK z sekcji 4
4. **Migracja:** Wykonuj bloki z `etap2_klasa_B_pk_do_id.sql` po kolei, waliduj po każdym
5. **Raport:** Wypełnij tabelę raportu na końcu skryptu migracji
