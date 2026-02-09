# ETAP 1 – Plan migracji PK → id

**Baza:** locbd (MariaDB/MySQL)  
**Data:** 2026-02-02  
**Status:** Oczekuje na akceptację

---

## 1️⃣ Analiza bazy (wykonana)

### Tabele POMINIĘTE (mają FK)

| Powód | Tabele |
|-------|--------|
| **FK wychodzące** | doc_counters, ofertypozycje, operator_table_permissions, pozycjefaktury |
| **Referencjonowane** | faktury, firmy, oferty, operator |

### Tabele spełniające warunki ETAPU 1 (76 tabel)

| Tabela | PK (obecna) | Typ | AUTO_INCREMENT |
|--------|-------------|-----|----------------|
| auta | id_auta | int(11) | TAK |
| awariamaszyna | id_awaria_maszyna | int(11) | TAK |
| awariapozycje | id_awaria_pozycja | int(15) | TAK |
| banki | id_banku | int(15) | TAK |
| daneobliczmaszyne | id_dane_oblicz_maszyne | int(11) | TAK |
| danetabliczka | id_dane_tabliczka | int(11) | TAK |
| dokdlaodbiorcy | id_dok_dla_odbiorcy | int(15) | TAK |
| dostawcy | id_dostawcy | int(15) | TAK |
| fakturatyp | id_faktura_typ | int(15) | TAK |
| foto | id_foto | int(15) | TAK |
| grupagtu | id_grupagtu | int(15) | TAK |
| grupasprzedazy | id_grupaprzedazy | int(15) | TAK |
| grupytowaru | id_grupy_towaru | int(15) | TAK |
| jednostki | id_jednostki | int(15) | TAK |
| kalendarz | id_kalendarz | int(11) | TAK |
| kasa | id_kasy | int(15) | TAK |
| kurswaluty | id_kurs_waluty | int(15) | TAK |
| liniaprodukcyjna | id_linii_produkcyjnej | int(3) | TAK |
| linieobciazenieoferta | id_linii_obc_ofe | int(11) | TAK |
| linieobciazeniezlecenie | id_obciazenia | int(15) | TAK |
| linieprodukcyjne | id_linii | int(15) | TAK |
| linierhproduktu | id_prod_rh | int(11) | TAK |
| maszynatyp | id_typ_maszyny | int(10) | TAK |
| maszyny | id_maszyny | int(11) | TAK |
| narzedziawydane | id_wydania | int(15) | TAK |
| narzedziawydanepracownik | id_narzedzia_pracownik | int(15) | TAK |
| noty | id_noty | int(11) | TAK |
| odbiorcagrupa | id_odbiorca_grupa | int(15) | TAK |
| odbiorcatyp | id_odbiorca_typ | int(11) | TAK |
| odbiorcazapisanydogrupy | id_odbiorca_zapisany_do_grupy | int(15) | TAK |
| odbiorcy | ID_odbiorcy | int(15) | TAK |
| odbiorcylistatemp | id_odbiorcy_lista_temp | int(15) | TAK |
| ofertastatus | id_statusu_oferty | int(11) | TAK |
| packinglist | id_packing_list | int(11) | TAK |
| platnosci | id_platnosci | int(15) | TAK |
| platnoscstatus | id_platnosc_status | int(15) | TAK |
| platnosctyp | id_platnosc_typ | int(11) | TAK |
| pozycjedelegacji | id_pozycji_delegacji | int(15) | TAK |
| pozycjedokdlaodbiorcy | id_pozycji_dok_dla_odbiorcy | int(15) | TAK |
| pozycjepozycjizlecenia | id_pozycji_pozycji_zlecenia | int(15) | TAK |
| pozycjeproduktu | id_pozycji_produktu | int(15) | TAK |
| pozycjezamowienia | id_pozycji_zamowienia | int(15) | TAK |
| pozycjezlecenia | ID_pozycji_zlecenia | int(15) | TAK |
| pracownicy | id_pracownika | int(15) | TAK |
| premia | id_premii | int(11) | TAK |
| produkcjastatus | id_statusu_zlecenia | int(15) | TAK |
| produkty | id_produktu | int(15) | TAK |
| remanent | id_remanentu | int(15) | TAK |
| rh | id_rh | int(11) | TAK |
| rhproduktu | id_rhproduktu | int(15) | TAK |
| rodzajurlopu | id_rodzaj_urlopu | int(11) | TAK |
| rola | id_roli | int(11) | TAK |
| serwistyp | id_serwistyp | int(11) | TAK |
| serwisy | id_serwisu | int(15) | TAK |
| serwisypracownicy | id_serwisy_pracownicy | int(11) | TAK |
| skrzynie | ID_skrzynia | int(15) | TAK |
| specmaszklienta | id_spec_masz_klienta | int(11) | TAK |
| specyfikacjamaszyny | id_specyfikacja_maszyny | int(15) | TAK |
| sprawdzaniemaszyny | id_spradzania_maszyny | int(11) | TAK |
| srodkitrwale | id_srodka_trwalego | int(15) | TAK |
| statusinne | id_statusu_inne | int(11) | TAK |
| stawkavat | id_stawka_vat | int(11) | TAK |
| tabtym | id_tabtym | int(11) | TAK |
| text | id_tekst | int(11) | TAK |
| towarstatus | id_statusu_towaru | int(11) | TAK |
| towary | ID_towar | int(15) | TAK |
| umowypracownicy | id_umowy | int(15) | TAK |
| urlopy | id_urlopy | int(15) | TAK |
| waluty | id_waluty | int(11) | TAK |
| wyplaty | id_wyplaty | int(15) | TAK |
| wyplatydanem | id_wyplaty_dane_miesiaca | int(11) | TAK |
| wyplatydaney | id_wyplaty_dane_y | int(11) | TAK |
| zamowienia | id_zamowienia | int(15) | TAK |
| zamowieniahala | id_zamowienia_hala | int(11) | TAK |
| zamowieniestatus | id_statusu_zamowienia | int(11) | TAK |
| zlecenia | id_zlecenia | int(15) | TAK |

---

## 2️⃣ Plan zmian – metoda

**Sposób:** `CHANGE COLUMN` (MariaDB/MySQL) – zachowuje typ, AUTO_INCREMENT, PRIMARY KEY.

```sql
ALTER TABLE tabela CHANGE COLUMN stary_pk id typ NOT NULL AUTO_INCREMENT;
```

- **RENAME COLUMN** (MariaDB 10.5.2+, MySQL 8.0): krótsza składnia, ale `CHANGE` działa wszędzie.
- **ADD + COPY + DROP**: niepotrzebne – `CHANGE` nie usuwa danych.

---

## 3️⃣ Skrypt migracji

Plik: `ERP.Infrastructure/Sql/etap1_migracja_pk_do_id.sql`

Każda tabela = osobny blok z transakcją. **NIE wykonuj** przed akceptacją.

---

## 4️⃣ Walidacja (po każdej tabeli)

```sql
-- Przed migracją: zapisz liczbę rekordów
SELECT COUNT(*) FROM tabela;

-- Po migracji:
SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS 
WHERE TABLE_SCHEMA='locbd' AND TABLE_NAME='tabela' AND COLUMN_NAME='id';
-- Oczekiwane: COLUMN_KEY=PRI, EXTRA=auto_increment

SELECT COUNT(*) FROM tabela;
-- Musi być równe wartości przed migracją
```

---

## 5️⃣ Szablon raportu (po migracji)

| Tabela | Było | Jest |
|--------|------|------|
| auta | id_auta | id |
| ... | ... | id |

---

## ⚠️ Uwaga

Przed wykonaniem migracji:
1. **Backup bazy** – pełny dump locbd
2. **Akceptacja** – zatwierdź listę tabel
3. **Test** – wykonaj na 1–2 tabelach testowych (np. `text`, `tabtym`)

**Po migracji:** zaktualizuj kod C# (ERP.Domain, ERP.Infrastructure) – zamień odwołania do starych nazw PK (np. `ID_odbiorcy`, `id_dostawcy`, `ID_towar`) na `id` w encjach i repozytoriach.

---

## Pliki

| Plik | Opis |
|------|------|
| `docs/ETAP1_ANALIZA_RAPORT.md` | Wynik analizy |
| `docs/ETAP1_PLAN_MIGRACJI.md` | Ten dokument |
| `ERP.Infrastructure/Sql/etap1_migracja_pk_do_id.sql` | Skrypt migracji (76 tabel) |
| `ERP.Infrastructure/Sql/etap1_walidacja.sql` | Szablon walidacji |
