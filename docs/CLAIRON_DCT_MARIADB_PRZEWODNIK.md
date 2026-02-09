# Clarion + MariaDB – Przewodnik dopasowania DCT i INSERT

**Cel:** Clarion w 100% zgodny z MariaDB. Jedno źródło prawdy: AUTO_INCREMENT w bazie. Brak konfliktów ID między Clarion i C#.

**Kontekst:**
- Projekt Clarion (ABC)
- Baza: MariaDB (locbd)
- Równolegle: aplikacja C#
- Wszystkie FOREIGN KEY usunięte
- AUTO_INCREMENT ustawiony na wysokie zakresy (≥ 1 000 000 000) dla rekordów C#
- Clarion NIE może sam generować ID – ID nadaje wyłącznie MariaDB

---

## 1. DCT – Konfiguracja PRIMARY KEY dla każdej tabeli

### 1.1 Zasady ogólne

Dla **każdej** tabeli w słowniku Clarion:

| Ustawienie | Wartość |
|------------|---------|
| **Primary Key** | ✔ Zaznaczone |
| **Auto Number / Identity** | ✔ Zaznaczone |
| **Clarionowe sekwencje** | ✘ WYŁĄCZONE |
| **Ręczne liczniki ID** | ✘ WYŁĄCZONE |
| **Typ pola PK** | LONG (zgodny z INT/BIGINT w MariaDB) |
| **PK w aplikacji** | **Read Only** – nigdy nie edytowalne |

### 1.2 Mapowanie tabel → kolumna PK

Uwaga: W bazie mogą występować stare nazwy (aoferty, apozycjeoferty) lub nowe (oferty, ofertypozycje). DCT musi używać **faktycznej** nazwy kolumny PK z bazy.

| Tabela w bazie | Kolumna PK | Typ w MariaDB | Uwagi |
|----------------|------------|---------------|-------|
| firmy | id | INT(15) | |
| oferty / aoferty | id / ID_oferta | INT(15) | Sprawdź aktualną nazwę w SHOW COLUMNS |
| ofertypozycje / apozycjeoferty | id / ID_pozycja_oferty | INT(15) | |
| faktury | id | BIGINT | |
| pozycjefaktury | id | BIGINT | |
| odbiorcy / Odbiorcy | id / ID_odbiorcy | INT(15) | |
| dostawcy | id | INT(15) | |
| towary | id | INT(15) | |
| zamowienia | id | INT(15) | |
| pozycjezamowienia | id | INT(15) | |
| zlecenia | id | INT(15) | |
| pozycjezlecenia | id | INT(15) | |
| operator | id_operatora | INT(15) | Wyjątek – PK nie nazywa się „id” |
| operatorfirma | id | INT(15) | |
| operator_login | id | INT(15) | |
| operator_table_permissions | id | INT(15) | |
| produkty | id | INT(15) | |
| jednostki | id | INT(15) | |
| stawkavat | id | INT(15) | |
| waluty | id | INT(15) | |
| … (pozostałe tabele) | id / id_xxx | INT(15) | Zgodnie z SHOW COLUMNS |

**Uwaga:** Po migracji PK część tabel ma kolumnę `id`, inne nadal używają starych nazw (np. `ID_odbiorcy`, `id_pozycji_zamowienia`). Przed konfiguracją DCT uruchom `SHOW COLUMNS FROM nazwa_tabeli` i sprawdź faktyczną nazwę kolumny PK.

### 1.3 Załącznik – pełna lista PK (przed/po migracji)

| Tabela | PK (możliwa stara nazwa) | PK (po migracji) |
|--------|---------------------------|------------------|
| aoferty / oferty | ID_oferta | id |
| apozycjeoferty / ofertypozycje | ID_pozycja_oferty | id |
| faktury | Id_faktury | id |
| pozycjefaktury | id_pozycji_faktury | id |
| Odbiorcy / odbiorcy | ID_odbiorcy | id |
| dostawcy | id_dostawcy | id |
| towary | ID_towar | id |
| zamowienia | id_zamowienia | id |
| pozycjezamowienia | id_pozycji_zamowienia | id |
| zlecenia | id_zlecenia | id |
| pozycjezlecenia | ID_pozycji_zlecenia | id |
| operator | id_operatora | (bez zmiany) |
| firmy | id_firmy | id |

### 1.4 Weryfikacja w DCT

- Otwórz każdą tabelę w DCT.
- Dla pola PK:
  - **Key** = Primary
  - **Auto Increment** = Yes (Identity / Auto Number)
  - **Clarion Sequence** = None / Off
  - **Default** = brak (baza nadaje wartość)
- Pole PK **nie** może być w żadnym generatorze Clarion (np. `#NEXT()`).

---

## 2. INSERT – TryInsert / ADD

### 2.1 Wzorzec obowiązkowy

```
PRZED INSERT:
  PK = 0   (lub nie wstawiaj PK w ogóle – MariaDB nadaje)

INSERT (TryInsert / ADD)

PO INSERT:
  Fetch(PRIMARY)   ! WYMUSIĆ – pobierz rekord z ID nadanym przez MariaDB
  ! Teraz PK zawiera poprawne ID
```

### 2.2 Przykład w Clarion (pseudokod)

```clarion
! ❌ ŹLE – stary sposób
! NextId = GetNextId()   ! Clarionowe liczniki – ZAKAZANE
! Rec.Id = NextId
! ADD OfertyFile

! ✅ DOBRZE
OfertyFile.Id = 0
OfertyFile.Id_firmy = CurrentCompanyId
OfertyFile.Data_oferty = Today()
! ... pozostałe pola ...
ADD OfertyFile
IF ERRORCODE() = 0
  Fetch(OfertyFile, PRIMARY)   ! Pobierz ID nadane przez MariaDB
  NewOfferId = OfertyFile.Id
END
```

### 2.3 Zakazy

| Zakaz | Opis |
|-------|------|
| ✘ Ręczne przypisywanie ID | Nigdy `Rec.Id = wartość` przed INSERT |
| ✘ Kopiowanie ID ze źródła | Przy kopiowaniu oferty: `NowaOferta.Id = StaraOferta.Id` – ZAKAZANE |
| ✘ Clarionowe sekwencje | `#NEXT()`, liczniki w plikach .cln, generatory |
| ✘ Pomijanie Fetch po Insert | Bez Fetch nie znasz ID nadanego przez bazę |

### 2.4 Użycie LAST_INSERT_ID()

Jeśli używasz SQL bezpośrednio (np. `EXECUTE`):

```sql
INSERT INTO oferty (id_firmy, Data_oferty, ...) VALUES (@id_firmy, @data, ...);
SELECT LAST_INSERT_ID() AS new_id;
```

W Clarion: po `EXECUTE` odczytaj `new_id` z wyniku i ustaw w rekordzie.

---

## 3. Relacje

### 3.1 Brak FOREIGN KEY w bazie

- **NIE** dodawaj FOREIGN KEY w MariaDB.
- Relacje realizuj **wyłącznie logicznie** w kodzie Clarion.

### 3.2 Pola relacyjne

Pola typu:
- `id_firmy`
- `id_oferty` / `oferta_id`
- `id_zamowienia`
- `id_odbiorcy`
- `id_towaru`
- `id_dostawcy`
- itd.

Traktuj jako **zwykłe LONG** – bez constraintów w bazie. W Clarion możesz używać RELATE do łączenia danych, ale baza nie wymusza integralności.

---

## 4. Widoki (VIEW)

- Widoki mogą łączyć tabele (JOIN).
- Widoki **NIE** mogą:
  - wymagać FK (już usunięte)
  - generować ID
- Widoki **tylko do SELECT** – nie używaj ich do INSERT/UPDATE/DELETE.

---

## 5. Kopiowanie rekordów (np. oferta → nowa oferta)

### 5.1 Nagłówek (oferta)

```
NowaOferta.Id = 0                    ! Zawsze 0
NowaOferta.Id_firmy = StaraOferta.Id_firmy
NowaOferta.Data_oferty = StaraOferta.Data_oferty
! ... skopiuj pozostałe pola (BEZ Id) ...
ADD OfertyFile
Fetch(OfertyFile, PRIMARY)
NoweIdOferty = OfertyFile.Id
```

### 5.2 Pozycje (ofertypozycje)

```
LOOP przez pozycje starej oferty
  NowaPozycja.Id = 0                 ! Zawsze 0
  NowaPozycja.Oferta_id = NoweIdOferty   ! ID z nagłówka (po Fetch!)
  NowaPozycja.Id_towaru = StaraPozycja.Id_towaru
  ! ... skopiuj pozostałe pola ...
  ADD OfertypozycjeFile
  Fetch(OfertypozycjeFile, PRIMARY)
END
```

**Kluczowe:** `Oferta_id` musi być `NoweIdOferty` z nagłówka (po Fetch), a nie `StaraOferta.Id`.

---

## 6. Kontrola – procedury do sprawdzenia

### 6.1 Gdzie szukać starego mechanizmu

Przeszukaj projekt Clarion (APP, INC, CLW) pod kątem:

| Wzorzec | Znaczenie |
|---------|-----------|
| `#NEXT()` | Clarionowy generator – usuń |
| `GetNextId` / `NextId` | Ręczny licznik – usuń |
| `SET` / `ASSIGN` na polu PK przed ADD | Ręczne ID – usuń |
| `ADD` bez `Fetch` po nim | Brak pobrania ID – dodaj Fetch |
| `COPY` / kopiowanie z zachowaniem Id | Nieprawidłowe – ustaw Id=0 |

### 6.2 Szablon listy kontrolnej

| Procedura / Ekran | Tabela | Stary AUTO? | Fetch po Insert? | Uwagi |
|------------------|--------|-------------|------------------|-------|
| DodajOferte | oferty | ☐ | ☐ | |
| DodajPozycjeOferty | ofertypozycje | ☐ | ☐ | |
| KopiujOferte | oferty, ofertypozycje | ☐ | ☐ | |
| DodajOdbiorce | odbiorcy | ☐ | ☐ | |
| DodajTowar | towary | ☐ | ☐ | |
| DodajZamowienie | zamowienia | ☐ | ☐ | |
| DodajPozycjeZamowienia | pozycjezamowienia | ☐ | ☐ | |
| DodajFakture | faktury | ☐ | ☐ | |
| DodajPozycjeFaktury | pozycjefaktury | ☐ | ☐ | |
| … | … | ☐ | ☐ | |

### 6.3 Czego NIE zmieniać

- **Logika biznesowa** – pozostaw bez zmian
- **Nazwy pól** – nie zmieniaj (poza dopasowaniem do bazy)
- **Kolejność pól, walidacje** – bez zmian

---

## 7. Weryfikacja w bazie

Sprawdź, czy kolumna PK ma AUTO_INCREMENT:

```sql
SELECT TABLE_NAME, COLUMN_NAME, COLUMN_KEY, EXTRA
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = 'locbd'
  AND TABLE_NAME IN ('oferty', 'ofertypozycje', 'faktury', 'pozycjefaktury', 'odbiorcy', 'zamowienia')
  AND COLUMN_KEY = 'PRI';
```

Oczekiwane: `EXTRA = 'auto_increment'` dla kolumny PK.

---

## 8. Podsumowanie

| Element | Działanie |
|---------|-----------|
| DCT – PK | Primary Key + Auto Number, bez Clarion sequences |
| DCT – typ PK | LONG |
| PK w UI | Read Only |
| INSERT | PK=0 przed, Fetch(PRIMARY) po |
| Relacje | Tylko logiczne, bez FK w bazie |
| Widoki | Tylko SELECT |
| Kopiowanie | Nowy rekord: Id=0, po Fetch użyj nowego ID dla pozycji |

**Wynik:** Clarion i C# współdzielą bazę bez konfliktów ID. MariaDB jest jedynym źródłem wartości AUTO_INCREMENT.
