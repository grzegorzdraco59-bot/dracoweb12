# Ofertypozycje w WPF – tylko ofertypozycje.id i oferta_id

## Kontekst DB (obowiązujący)

- Tabela pozycji ofert: **ofertypozycje**
- JEDYNY identyfikator pozycji: **ofertypozycje.id** (AUTO_INCREMENT, PRIMARY KEY)
- Kolumna **ID_pozycja_oferty** NIE ISTNIEJE lub NIE MOŻE BYĆ używana w kodzie
- Relacja do nagłówka: **ofertypozycje.oferta_id** → oferty.id

## Cel (zrealizowany)

Cały WPF jest dostosowany tak, że:

1. **Dodawanie pozycji (INSERT)** – działa bez odwołań do ID_pozycja_oferty; INSERT używa kolumny **oferta_id**.
2. **Edycja pozycji (UPDATE)** – wykonywana po **ofertypozycje.id** (WHERE id = @Id).
3. **Ładowanie pozycji** – po **ofertypozycje.oferta_id** (WHERE p.oferta_id = @OfferId).
4. W kodzie **nie ma** żadnego odwołania do **ID_pozycja_oferty** (w kontekście tabeli ofertypozycje).

---

## KROK 1 — Modele / DTO

| Element | Stan |
|--------|------|
| **OfferPositionDto** | Ma **long Id** (mapa na ofertypozycje.id) i **long OfferId** (mapa na oferta_id). Brak pola ID_pozycja_oferty. |
| **OfferPosition (Entity)** | Używa BaseEntity.Id (int) i OfferId (int); mapowanie z readerów zwraca int (BIGINT → int w encji). |

---

## KROK 2 — SELECT (ładowanie pozycji)

**OfferPositionRepository** – wszystkie zapytania SELECT:

- Zwracają: **p.id AS Id**, **p.oferta_id AS OfertaId**
- Filtrują: **WHERE p.oferta_id = @OfferId** (oraz np. GetById: WHERE p.id = @Id)
- Nie używają: ID_pozycja_oferty, id_pozycja_oferty

Metody: `GetByIdAsync`, `GetByOfferIdAsync`, `GetByCompanyIdAsync`.

---

## KROK 3 — INSERT (dodawanie pozycji)

**OfferPositionRepository.AddAsync**:

- INSERT INTO ofertypozycje **(id_firmy, oferta_id**, id_towaru, …) VALUES (@CompanyId, **@OfferId**, …)
- Kolumna **oferta_id** jest jawnie używana; **ID_pozycja_oferty** nie występuje.
- Po INSERT: `SELECT LAST_INSERT_ID()` → zwracane jako Id nowej pozycji (ofertypozycje.id).

---

## KROK 4 — UPDATE (edycja pozycji)

**OfferPositionRepository.UpdateAsync**:

- UPDATE ofertypozycje SET … **WHERE id = @Id** AND id_firmy = @CompanyId
- Warunek wyłącznie po **ofertypozycje.id**; bez użycia id_faktury / ID_pozycja_oferty.

---

## Pozostałe serwisy (WPF / Application)

- **OfferTotalsService** – przeliczenia po **p.oferta_id** (WHERE p.oferta_id = @OfferId).
- **OrderPositionMainRepository** – JOIN: ofertypozycje **a ON a.id = p.id_pozycji_pozycji_oferty** (pozycje zamówienia odwołują się do ofertypozycje.id).
- **OfferToFpfConversionService** – parametr @IdPozycjiOferty dotyczy tabeli **pozycjefaktury** (kolumna id_pozycji_oferty w fakturze), nie tabeli ofertypozycje; wartość to ofertypozycje.id (pos.Id). Nie jest to odwołanie do kolumny ID_pozycja_oferty w ofertypozycje.

---

## Potwierdzenie

- W **Application** i **ERP.UI.WPF** nie ma odwołań do **ID_pozycja_oferty** ani **id_pozycja_oferty** w kontekście tabeli ofertypozycje.
- Lista faktur / pozycje oferty: identyfikacja po **ofertypozycje.id** i **ofertypozycje.oferta_id**.
- INSERT/UPDATE/SELECT pozycji oferty używają wyłącznie **id** i **oferta_id**.
