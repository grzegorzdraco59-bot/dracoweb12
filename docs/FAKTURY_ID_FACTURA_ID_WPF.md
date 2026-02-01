# Faktury WPF – identyfikatory faktury.id i pozycjefaktury.faktura_id

## Cel

Dostosowanie okna WPF „Faktury” do nowego schematu:
- **Nagłówek:** `faktury.id` (BIGINT) jako Id.
- **Pozycje:** ładowane po `pozycjefaktury.faktura_id`; edycja po PK `id_pozycji_faktury`.
- **Na tym etapie:** tylko SELECT/UPDATE istniejących pozycji (bez INSERT nowych).

---

## Zmienione pliki

| Plik | Zmiana |
|------|--------|
| **ERP.Application/DTOs/InvoiceDto.cs** | Już wcześniej: `Id` (long) → `faktury.id`. |
| **ERP.Application/DTOs/InvoicePositionDto.cs** | Już wcześniej: `IdPozycjiFaktury`, `FakturaId` + aliasy `Id`, `InvoiceId`. |
| **ERP.Application/Repositories/IInvoicePositionRepository.cs** | `GetByInvoiceIdAsync(long invoiceId)`. |
| **ERP.Infrastructure/Repositories/InvoiceRepository.cs** | MapToDto: `Id = GetLong(reader, "id")`. GetDocumentsByOfferIdAsync: `InvoiceId = (int)GetLong(reader, "id")`. Dodana metoda `GetLong`. |
| **ERP.Infrastructure/Repositories/InvoicePositionRepository.cs** | `GetByInvoiceIdAsync(long invoiceId)`. SELECT: `p.id_pozycji_faktury AS IdPozycjiFaktury`, `p.faktura_id AS FakturaId`, … WHERE `p.faktura_id = @InvoiceId` ORDER BY `p.id_pozycji_faktury`. MapToDto: `IdPozycjiFaktury`, `FakturaId` (GetLong). Dodana metoda `GetLong`. |
| **ERP.UI.WPF/ViewModels/InvoicesViewModel.cs** | `LoadPositionsAsync(long invoiceId)`; wywołanie `LoadPositionsAsync(value.Id)` (Id jest long). |

---

## Przepływ

1. **Lista faktur (SELECT)**  
   `InvoiceRepository.GetByCompanyIdAsync` zwraca `InvoiceDto` z `Id = faktury.id` (long). W widoku używane jest `SelectedFaktura.Id`.

2. **Ładowanie pozycji (SELECT)**  
   Przy zmianie `SelectedInvoice` wywoływane jest `LoadPositionsAsync(value.Id)`.  
   `InvoicePositionRepository.GetByInvoiceIdAsync(invoiceId)` wykonuje:
   - `SELECT p.id_pozycji_faktury AS IdPozycjiFaktury, p.faktura_id AS FakturaId, …`
   - `FROM pozycjefaktury p WHERE p.faktura_id = @InvoiceId ORDER BY p.id_pozycji_faktury`

3. **Edycja pozycji (UPDATE)**  
   Obecnie w WPF nie ma osobnego okna edycji pozycji faktury ani wywołań UpdateAsync dla pozycji. Gdy zostanie dodane:
   - warunek UPDATE: `WHERE id_pozycji_faktury = @IdPozycjiFaktury`;
   - po zapisie: odświeżenie listy pozycji SELECT po `faktura_id`.

---

## Web (niezmieniany)

- `ERP.UI.Web` nie był modyfikowany.
- `InvoicesController` wywołuje `GetByInvoiceIdAsync(invoiceId, …)`; parametr z route może być `int` i jest niejawnie rzutowany na `long`.

---

## Testy (ręczne)

1. Uruchom WPF → menu Faktury → okno się otwiera.
2. Wybierz firmę (jeśli wymagane) → lista faktur ładuje się (SELECT po `faktury.id`).
3. Wybierz fakturę → pozycje ładują się (SELECT po `pozycjefaktury.faktura_id`).
4. Brak błędów „Unknown column” oraz crashy (wymaga wykonania migracji PK/FK na bazie: `faktury.id`, `pozycjefaktury.faktura_id`).

---

## Potwierdzenie

- W WPF używane są: **faktury.id** (jako `InvoiceDto.Id`) oraz **pozycjefaktury.faktura_id** (ładowanie pozycji) i **pozycjefaktury.id_pozycji_faktury** (PK w DTO: `IdPozycjiFaktury`).
- Lista faktur i lista pozycji wybranej faktury działają na nowym identyfikatorze bez używania `id_faktury` w nowej logice.
