# Okno „Oferty” – edycja pozycji na ofertypozycje.id i oferta_id

## Kontekst DB

- **oferty** – PK = id  
- **ofertypozycje** – id (BIGINT UNIQUE NOT NULL), oferta_id (BIGINT NOT NULL)  
- Stary PK `ID_pozycja_oferty` w DB, **nieużywany w kodzie**

## Cel (bez INSERT na tym etapie)

- **SELECT pozycji** – po `oferta_id` (ładowanie listy do oferty)  
- **UPDATE pozycji** – po `id` (edycja istniejącej pozycji)  
- Edycja istniejących pozycji działa na nowych kolumnach

---

## Stan po wcześniejszych zmianach (potwierdzenie)

### KROK 1 – DTO/Model

- **OfferPositionDto** ma:
  - `long Id` – mapuje do ofertypozycje.id  
  - `long OfferId` – mapuje do ofertypozycje.oferta_id  
- Brak property `ID_pozycja_oferty` w DTO (tylko w DB).

### KROK 2 – SELECT pozycji (ładowanie)

- **OfferPositionRepository.GetByOfferIdAsync**:
  - Zwraca: `p.id AS Id`, `p.oferta_id AS OfertaId` oraz resztę pól.
  - Filtruje: `WHERE p.oferta_id = @OfferId AND p.id_firmy = @CompanyId`.
  - Order: `ORDER BY p.nr_zespolu, p.id`.

### KROK 3 – UPDATE pozycji (edycja)

- **OfferPositionRepository.UpdateAsync**:
  - Warunek: `WHERE id = @Id AND id_firmy = @CompanyId` (nie `ID_pozycja_oferty`).
  - Do UPDATE przekazywane jest `offerPosition.Id` (Entity.Id z mapowania z `p.id`).
- **OfferPositionEditViewModel**: przy zapisie wywołuje `GetPositionByIdAsync((int)_position.Id)`, potem `UpdatePositionAsync(position)` – używane jest DTO.Id z listy.

### KROK 4 – UI/XAML

- **OffersView.xaml** (DataGrid pozycji): bindingi tylko do `Name`, `Ilosc`, `CenaNetto`, `Discount`, `VatRate`, `BruttoPoz`.
- Brak odwołań do `ID_pozycja_oferty` w XAML.
- Layout bez zmian.

---

## Lista plików (już zmienionych w poprzednim etapie)

| Plik | Odpowiedzialność |
|------|-------------------|
| ERP.Application/DTOs/OfferPositionDto.cs | long Id, long OfferId (mapa na id, oferta_id) |
| ERP.Infrastructure/Repositories/OfferPositionRepository.cs | SELECT p.id AS Id, p.oferta_id AS OfertaId; WHERE p.oferta_id = @OfferId; UPDATE/DELETE WHERE id = @Id |
| ERP.UI.WPF/ViewModels/OffersViewModel.cs | Ładowanie pozycji po SelectedOffer.Id; DeletePositionAsync((int)SelectedOfferPosition.Id) |
| ERP.UI.WPF/ViewModels/OfferPositionEditViewModel.cs | GetPositionByIdAsync((int)_position.Id), UpdatePositionAsync(position) – DTO.Id z listy |
| ERP.UI.WPF/Views/OffersView.xaml | Bindingi do Name, Ilosc, CenaNetto, BruttoPoz – brak ID_pozycja_oferty |

---

## Test (scenariusz)

1. Otwórz okno „Oferty”.  
2. Wejdź w ofertę z wieloma pozycjami (np. oferta_id = 2968).  
3. Zmień ilość/cenę/rabat/VAT w pozycji i zapisz.  
4. W DB sprawdź, że rekord zaktualizował się po **ofertypozycje.id** (np. `SELECT * FROM ofertypozycje WHERE id = ...`).  
5. Brak błędów: Unknown column, okno się otwiera.

---

## Potwierdzenie

- **SELECT pozycji:** po **oferta_id** (`WHERE p.oferta_id = @OfferId`).  
- **UPDATE pozycji:** po **id** (`WHERE id = @Id`), z przekazanym DTO.Id z listy.  
- INSERT (dodawanie nowych pozycji) nie było na tym etapie wymagane – w kodzie już istnieje, ale na tym etapie skupiamy się na edycji istniejących pozycji.
