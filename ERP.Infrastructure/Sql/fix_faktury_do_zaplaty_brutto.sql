-- =============================================================================
-- FIX: Unknown column 'do_zaplaty_brutto' – dodanie kolumn do tabeli faktury
-- MariaDB / MySQL. Uruchomić w kliencie SQL (np. HeidiSQL, DBeaver, mysql).
-- =============================================================================

-- ========== KROK 1 – ANALIZA ==========
-- Wykonaj poniższe zapytanie i sprawdź, czy w wyniku są kolumny:
--   sum_zaliczek_brutto, do_zaplaty_brutto

SHOW COLUMNS FROM faktury;

-- Jeśli w wyniku NIE MA tych kolumn, wykonaj KROK 2.

-- ========== KROK 2 – MIGRACJA ==========
-- Dodanie brakujących kolumn (rozliczenie zaliczek FVZ → FV).

-- Wariant A: MariaDB 10.5.2+ (ADD COLUMN IF NOT EXISTS – bezpieczne przy ponownym uruchomieniu)
ALTER TABLE faktury
  ADD COLUMN IF NOT EXISTS sum_zaliczek_brutto DECIMAL(18,2) NOT NULL DEFAULT 0.00,
  ADD COLUMN IF NOT EXISTS do_zaplaty_brutto   DECIMAL(18,2) NOT NULL DEFAULT 0.00;

-- Wariant B: Starszy MySQL/MariaDB (bez IF NOT EXISTS)
-- Uruchom tylko gdy Wariant A zwraca błąd składni. W razie błędu "Duplicate column" kolumna już istnieje – pomiń tę linię.
-- ALTER TABLE faktury ADD COLUMN sum_zaliczek_brutto DECIMAL(18,2) NOT NULL DEFAULT 0.00;
-- ALTER TABLE faktury ADD COLUMN do_zaplaty_brutto   DECIMAL(18,2) NOT NULL DEFAULT 0.00;

-- Po wykonaniu: ponownie uruchom SHOW COLUMNS FROM faktury; i upewnij się, że kolumny są na liście.
