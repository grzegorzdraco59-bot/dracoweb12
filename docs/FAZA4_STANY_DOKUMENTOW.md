# FAZA 4 – Stany dokumentów i dozwolone przejścia (Oferty i Zamówienia)

## 1. Encje / tabele w systemie

| Dokument   | Nagłówek (tabela) | Pozycje (tabela)     | Repozytorium (nagłówek / pozycje)   |
|-----------|--------------------|----------------------|-------------------------------------|
| **Oferta** | `aoferty`          | `apozycjeoferty`     | OfferRepository / OfferPositionRepository |
| **Zamówienie** | `zamowienia`   | `pozycjezamowienia` | OrderMainRepository / OrderPositionMainRepository |

*(Uwaga: w projekcie występuje też `zamowieniahala` – zamówienia hala; niniejszy dokument dotyczy ofert `aoferty` oraz zamówień głównych `zamowienia`.)*

---

## 2. Minimalny model stanów

### 2.1 OFERTA (aoferty)

| Stan (kod) | Opis |
|------------|------|
| **Draft** | Robocza – tworzenie i edycja |
| **Sent** | Wysłana do klienta |
| **Accepted** | Zaakceptowana przez klienta |
| **Rejected** | Odrzucona |
| **Cancelled** | Anulowana |

### 2.2 ZAMÓWIENIE (zamowienia)

| Stan (kod) | Opis |
|------------|------|
| **Draft** | Robocza – tworzenie i edycja |
| **Confirmed** | Potwierdzone |
| **InProgress** | W realizacji |
| **Shipped** | Wysłane |
| **Completed** | Zakończone |
| **Cancelled** | Anulowane |

---

## 3. Tabela przejść (source → target)

### 3.1 OFERTA

| Ze stanu (source) | Do stanu (target) | Uwagi |
|-------------------|--------------------|--------|
| Draft | Sent | Wysłanie oferty |
| Draft | Cancelled | Anulowanie roboczej |
| Sent | Accepted | Akceptacja przez klienta |
| Sent | Rejected | Odrzucenie |
| Sent | Cancelled | Anulowanie po wysłaniu |
| Accepted | — | Końcowy (bez dalszych przejść stanu oferty; można generować zamówienie) |
| Rejected | — | Końcowy |
| Cancelled | — | Końcowy |

### 3.2 ZAMÓWIENIE

| Ze stanu (source) | Do stanu (target) | Uwagi |
|-------------------|--------------------|--------|
| Draft | Confirmed | Potwierdzenie zamówienia |
| Draft | Cancelled | Anulowanie roboczej |
| Confirmed | InProgress | Rozpoczęcie realizacji |
| InProgress | Shipped | Wysłanie |
| Shipped | Completed | Zakończenie |
| Confirmed | Cancelled | Anulowanie po potwierdzeniu |
| InProgress | Cancelled | Anulowanie w trakcie (opcjonalnie) |
| Completed | — | Końcowy |
| Cancelled | — | Końcowy |

---

## 4. Reguły edycji, usuwania i generowania

### 4.1 Kiedy można edytować dokument (nagłówek)

| Dokument | Dozwolone stany edycji nagłówka |
|----------|----------------------------------|
| **Oferta** | Tylko **Draft** |
| **Zamówienie** | Tylko **Draft** |

W stanach innych niż Draft edycja nagłówka jest zablokowana (lub wymaga osobnej reguły biznesowej, np. tylko pola „uwagi”).

### 4.2 Kiedy można edytować pozycje

| Dokument | Dozwolone stany edycji pozycji |
|----------|---------------------------------|
| **Oferta** | Tylko **Draft** |
| **Zamówienie** | Tylko **Draft** (ew. **Confirmed** – w zależności od polityki; domyślnie tylko Draft) |

### 4.3 Kiedy można usuwać pozycje

| Dokument | Dozwolone stany usuwania pozycji |
|----------|-----------------------------------|
| **Oferta** | Tylko **Draft** |
| **Zamówienie** | Tylko **Draft** |

W pozostałych stanach usuwanie pozycji jest niedozwolone.

### 4.4 Kiedy można usuwać dokument (nagłówek)

| Dokument | Dozwolone stany usuwania dokumentu |
|----------|-------------------------------------|
| **Oferta** | Tylko **Draft** (ew. **Cancelled** – soft delete / archiwum) |
| **Zamówienie** | Tylko **Draft** (ew. **Cancelled** – soft delete / archiwum) |

Usuwanie fizyczne (DELETE) zalecane wyłącznie dla **Draft**. Dla anulowanych dokumentów preferowane: zmiana stanu na Cancelled (bez fizycznego usuwania).

### 4.5 Kiedy można generować zamówienie z oferty

| Warunek | Reguła |
|---------|--------|
| Stan oferty | Generowanie zamówienia z oferty dozwolone **tylko dla oferty w stanie Accepted**. |
| Inne stany | Draft, Sent, Rejected, Cancelled – **nie** generować zamówienia z oferty. |

---

## 5. Podsumowanie (szybka ściąga)

| Akcja | Oferta | Zamówienie |
|-------|--------|------------|
| Edycja nagłówka | Tylko Draft | Tylko Draft |
| Edycja pozycji | Tylko Draft | Tylko Draft |
| Usuwanie pozycji | Tylko Draft | Tylko Draft |
| Usuwanie dokumentu | Tylko Draft (lub soft: Cancelled) | Tylko Draft (lub soft: Cancelled) |
| Generowanie zamówienia z oferty | — | Źródło: oferta **tylko w stanie Accepted** |

---

*Dokument: FAZA 4 / KROK 1 – definicja stanów i przejść. Implementacja w kodzie (kolumna status/stan, walidacja przejść) – kolejne kroki.*
