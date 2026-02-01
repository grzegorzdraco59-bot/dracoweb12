# ETAP 0 – Audyt tabel: PRIMARY KEY i relacje

## Skrypt weryfikacji (uruchomić w MariaDB/MySQL)

```sql
SHOW TABLES;
```

Dla każdej tabeli:
```sql
SHOW COLUMNS FROM <tabela>;
```

---

## Lista tabel z kodu (repozytoria, SQL)

| Tabela | Obecny PK | Ma już `id`? | Uwagi |
|--------|-----------|--------------|--------|
| **oferty** | `id` | ✅ TAK | Już poprawny |
| **ofertypozycje** | `ID_pozycja_oferty` | ❌ NIE | FK: oferta_id, id_firmy |
| **faktury** | `Id_faktury` | ❌ NIE | FK: id_firmy, id_oferty |
| **pozycjefaktury** | `id_pozycji_faktury` | ❌ NIE | FK: id_faktury, id_firmy, id_oferty |
| **Odbiorcy** | `ID_odbiorcy` | ❌ NIE | FK: id_firmy (klienci) |
| **dostawcy** | `id_dostawcy` | ❌ NIE | FK: id_firmy (suppliers) |
| **zamowienia** | `id` lub `id_zamowienia` | ⚠️ RÓŻNIE | GetIdColumnNameAsync – dynamiczne |
| **zamowieniahala** | `id_zamowienia_hala` | ❌ NIE | FK: id_zamowienia, id_firmy |
| **pozycjezamowienia** | `id_pozycji_zamowienia` | ❌ NIE | FK: id_zamowienia, id_firmy, id_towaru |
| **operatorfirma** | `id` | ✅ TAK | FK: id_operatora, id_firmy |
| **operator_login** | `id` | ✅ TAK | FK: id_operatora |
| **rola** | `id_roli` | ❌ NIE | Tabela słownikowa |
| **towary** | `ID_towar` | ❌ NIE | FK: id_firmy, id_dostawcy |
| **magazyn** | `id_magazyn` | ❌ NIE | FK: id_firmy, id_towaru |
| **doc_counters** | (company_id, doc_type, year, month) | – | PK złożony – bez zmian |

---

## Mapowanie: stary PK → nowy `id`

| Tabela | Stary PK | Nowy PK (docelowy) |
|--------|----------|---------------------|
| faktury | Id_faktury | id |
| pozycjefaktury | id_pozycji_faktury | id |
| ofertypozycje | ID_pozycja_oferty | id |
| Odbiorcy | ID_odbiorcy | id |
| dostawcy | id_dostawcy | id |
| zamowienia | id_zamowienia / id | id |
| pozycjezamowienia | id_pozycji_zamowienia | id |
| rola | id_roli | id |
| towary | ID_towar | id |
| magazyn | id_magazyn | id |
| zamowieniahala | id_zamowienia_hala | id |

---

## Mapowanie FK: stare kolumny → nowe `<tabela>_id`

| Tabela zależna | Stara FK | Nowa FK (docelowa) |
|----------------|----------|---------------------|
| pozycjefaktury | id_faktury | faktura_id |
| pozycjefaktury | id_oferty | oferta_id (już może być oferta_id w ofertypozycje) |
| ofertypozycje | oferta_id | oferta_id ✅ (już poprawna nazwa) |
| faktury | id_oferty | oferta_id |
| pozycjezamowienia | id_zamowienia | zamowienie_id |
| zamowieniahala | id_zamowienia | zamowienie_id |

Po wykonaniu audytu w bazie (SHOW TABLES, SHOW COLUMNS) uzupełnij powyższą listę o faktyczny stan kolumn.
