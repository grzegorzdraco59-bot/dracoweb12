# FAZA 4 / KROK 7: Testy i weryfikacja CreateFromOfferAsync

## 1. Testy jednostkowe (ERP.Tests)

- **CreateFromOfferAsync_WhenOfferNotAccepted_ThrowsBusinessRuleException**  
  Tylko oferta w statusie Accepted może być konwertowana; dla Draft/Sent itd. rzucany jest `BusinessRuleException` z czytelnym komunikatem („Accepted”, aktualny status).

- **CreateFromOfferAsync_WhenOfferAlreadyConverted_ReturnsExistingOrderId**  
  Idempotencja: jeśli oferta była już skonwertowana (istnieje zamówienie powiązane przez pozycje z `id_pozycji_pozycji_oferty`), zwracane jest ID istniejącego zamówienia; transakcja tworzenia **nie** jest wywoływana.

- **CreateFromOfferAsync_WhenTransactionThrows_PropagatesException**  
  Atomowość: wyjątek wewnątrz transakcji jest propagowany na zewnątrz; `UnitOfWork` cofa transakcję (rollback), więc przy błędzie zapisu pozycji nagłówek nie zostaje zapisany.

Uruchomienie: `dotnet test ERP.Tests\ERP.Tests.csproj --filter "OrderFromOfferConversionServiceTests"`.

---

## 2. Ręczna weryfikacja (test harness)

Wymagana baza z tabelami: `aoferty`, `apozycjeoferty`, `zamowienia`, `pozycjezamowienia`.

### a) Powstaje nagłówek zamówienia

1. Ustaw ofertę w status **Accepted** (np. w UI „Zmień status” Draft→Sent→Accepted).
2. Kliknij „Utwórz zamówienie z oferty”.
3. Sprawdź w bazie: `SELECT * FROM zamowienia ORDER BY id DESC LIMIT 1` — powinien być nowy wiersz z `id_firmy`, `nr_zamowienia`, `data_zamowienia`, `dostawca`, `uwagi`, `status = 'Draft'`.

### b) Kopiują się wszystkie pozycje (liczba i wartości kluczowe)

1. Dla oferty z N pozycjami wykonaj konwersję.
2. Sprawdź: `SELECT COUNT(*) FROM pozycjezamowienia WHERE id_zamowienia = @nowe_id` — powinno być N.
3. Porównaj wartości kluczowe (np. `towar_nazwa_draco`, `ilosc_zamawiana`, `cena_zamawiana`, `id_pozycji_pozycji_oferty`) z `apozycjeoferty` dla tej oferty.

### c) Zapis jest atomowy (błąd przy pozycjach → brak nagłówka)

- W testach jednostkowych: gdy transakcja rzuca wyjątek, `ExecuteInTransactionAsync` nie commituje — wyjątek jest propagowany (test **WhenTransactionThrows_PropagatesException**).
- Ręcznie (opcjonalnie): np. tymczasowo zmodyfikować nazwę kolumny w `INSERT` pozycji tak, aby drugi INSERT się wywalił; po wywołaniu konwersji w tabeli `zamowienia` nie powinien pojawić się nowy nagłówek (rollback).

---

## 3. Kontrola warunku wejściowego

- Tylko oferta w statusie **Accepted** może być konwertowana.
- Dla innego statusu: `BusinessRuleException` z komunikatem w stylu:  
  *„Konwersja oferty do zamówienia jest dozwolona tylko dla oferty w statusie Zaakceptowana (Accepted). Aktualny status oferty: {status}.”*

---

## 4. Zabezpieczenie idempotencji

- Jeśli oferta była już skonwertowana (istnieje zamówienie, którego pozycje mają `id_pozycji_pozycji_oferty` z tej oferty), serwis **nie** tworzy drugiego zamówienia i zwraca ID istniejącego.
- Wykrywanie: zapytanie do `pozycjezamowienia` + `apozycjeoferty` (`GetOrderIdLinkedToOfferAsync`).
