# Dostosowanie WPF do ofertypozycje.id i ofertypozycje.oferta_id

## Kontekst DB

- **oferty** – PK: `id`
- **ofertypozycje** – kolumna `id` (BIGINT, UNIQUE, NOT NULL); `oferta_id` (BIGINT, NOT NULL) jako FK do oferty.id
- Stary PK `ID_pozycja_oferty` nadal w tabeli, ale **nieużywany w kodzie**

## Cel

- Pozycje ofert identyfikowane po **ofertypozycje.id** (C#: `Id`)
- Relacja do oferty po **ofertypozycje.oferta_id** (C#: `OfferId` / OfertaId)
- Ładowanie pozycji: `WHERE p.oferta_id = @ofertaId`
- SELECT zwraca `p.id AS Id`, `p.oferta_id AS OfertaId`
- UPDATE/DELETE po `p.id`

---

## Lista zmienionych plików (repo / model / VM / XAML)

| Plik | Zmiana |
|------|--------|
| **ERP.Application/DTOs/OfferPositionDto.cs** | `long Id` (mapa na ofertypozycje.id), `long OfferId` (mapa na oferta_id); brak legacy ID_pozycja_oferty w DTO |
| **ERP.Infrastructure/Repositories/OfferPositionRepository.cs** | SELECT z aliasami `p.id AS Id`, `p.oferta_id AS OfertaId`; WHERE `p.oferta_id = @OfferId`; UPDATE/DELETE/Exists po `p.id`; MapToOfferPosition odczyt Id/OfertaId (z fallback na wielkość liter) |
| **ERP.Infrastructure/Repositories/OrderPositionMainRepository.cs** | JOIN: `a.id = p.id_pozycji_pozycji_oferty` |
| **ERP.UI.WPF/ViewModels/OffersViewModel.cs** | Ładowanie pozycji po SelectedOffer.Id; DeletePositionAsync((int)SelectedOfferPosition.Id); FirstOrDefault(p => p.Id == (long)id) |
| **ERP.UI.WPF/ViewModels/OfferPositionEditViewModel.cs** | `long Id`, `long OfferId`; rzutowania (int) przy GetPositionByIdAsync i new OfferPosition(..., (int)_position.OfferId, ...); _position.Id = (long)newId po Add |
| **ERP.UI.WPF/Views/OffersView.xaml** | Bez zmian – bindingi do Name, Ilosc, CenaNetto, BruttoPoz itd.; brak odwołań do ID_pozycja_oferty; kwoty nie liczone w XAML |
| **docs/OFERTYPOZYCJE_ID_OfertaId_WYNIK.md** | Ten raport |

---

## KROK 1 – Modele/DTO

- **OfferPositionDto**: `long Id` (mapa na ofertypozycje.id), `long OfferId` (mapa na ofertypozycje.oferta_id).
- **OfferPosition** (Entity): bez zmian – nadal `int Id` (BaseEntity), `int OfferId`; repozytorium mapuje `id`/`oferta_id` z BIGINT na int przy odczycie.
- Stare pole `ID_pozycja_oferty` nie jest używane w kodzie (tylko w DB).

---

## KROK 2 – Repo / SQL

- Wszystkie zapytania do ofertypozycje używają:
  - **SELECT**: `id`, `oferta_id`, …
  - **WHERE**: `id = @Id`, `oferta_id = @OfferId`
  - **ORDER BY**: `nr_zespolu, id` lub `oferta_id, nr_zespolu, id`
  - **UPDATE**: `WHERE id = @Id`
  - **DELETE**: `WHERE id = @Id`
- Ładowanie pozycji do oferty: `WHERE p.oferta_id = @ofertaId`.
- OrderPositionMainRepository: JOIN po `ofertypozycje.id` (`a.id = p.id_pozycji_pozycji_oferty`).

---

## KROK 3 – Logika przeliczeń

- **OfferTotalsService**: bez zmian – już używa `WHERE p.oferta_id = @OfferId` przy przeliczaniu netto/vat/brutto i sumy brutto oferty.
- Przeliczenia działają na nowych kolumnach i relacji oferta_id.

---

## KROK 4 – Test (scenariusz)

1. Uruchomić WPF, otworzyć okno Oferty.
2. Wejść w ofertę z wieloma pozycjami (np. oferta_id = 2968).
3. Sprawdzić: pozycje się ładują (GetByOfferIdAsync po oferta_id).
4. Edycja pozycji: otwarcie okna, zapis – UPDATE po `id`.
5. Usuwanie pozycji: DELETE po `id`.
6. Brak błędów: unknown column / mapping (wszystkie odwołania do ID_pozycja_oferty usunięte z kodu).

---

## Potwierdzenie: CRUD na ofertypozycje.id i oferta_id

- **SELECT pozycji:** zwraca `p.id AS Id`, `p.oferta_id AS OfertaId`; ładowanie do oferty: `WHERE p.oferta_id = @OfferId`.
- **INSERT:** wstawia `oferta_id = @OfferId`; nowe `Id` z `LAST_INSERT_ID()` przypisywane do DTO.
- **UPDATE:** `WHERE p.id = @Id`.
- **DELETE:** `WHERE p.id = @Id`.
- **ViewModel:** SelectedOffer.Id → LoadPositions(offerId); DTO.Id używane do edycji/usunięcia.
- **XAML:** brak bindingów do starych nazw; kwoty tylko wyświetlane (bez liczenia w XAML).
- Stary PK `ID_pozycja_oferty` **nie jest używany** w kodzie (tylko w DB).
- **Web nie był zmieniany.**

### ETAP 5 – Test (scenariusz)

1. Uruchom WPF, otwórz „Oferty”.
2. Otwórz ofertę z wieloma pozycjami (np. oferta_id = 2968).
3. Sprawdź: lista pozycji się ładuje; edycja zapisuje (UPDATE po id); usuwanie działa (DELETE po id); dodanie pozycji ustawia Id i OfertaId.
4. Brak błędów: Unknown column, XamlParseException.
