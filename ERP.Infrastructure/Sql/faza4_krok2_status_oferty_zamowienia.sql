-- FAZA 4 / KROK 2: Status w bazie (Oferty i Zamówienia)
-- Uruchomić w HeidiSQL na właściwej bazie.
-- Tabele nagłówków: aoferty (oferty), zamowienia (zamówienia). Kolumna firmy: id_firmy.

-- ========== KONTROLA (opcjonalnie – sprawdź przed ALTER) ==========
-- DESCRIBE aoferty;
-- DESCRIBE zamowienia;
-- SHOW COLUMNS FROM aoferty LIKE 'status';
-- SHOW COLUMNS FROM zamowienia LIKE 'status';

-- ========== DODANIE KOLUMNY STATUS ==========
-- Oferty: tabela aoferty – kolumny statusu brak w repozytorium, dodajemy.
ALTER TABLE aoferty
ADD COLUMN status VARCHAR(20) NOT NULL DEFAULT 'Draft';

-- Zamówienia: tabela zamowienia – kolumna status jest używana w OrderMainRepository.
-- Jeśli kolumna już istnieje (sprawdź: SHOW COLUMNS FROM zamowienia LIKE 'status';), pomiń poniższy ALTER.
ALTER TABLE zamowienia
ADD COLUMN status VARCHAR(20) NOT NULL DEFAULT 'Draft';

-- ========== INDEKSY (CompanyId, Status) – szybsze listowanie po firmie i statusie ==========
ALTER TABLE aoferty
ADD INDEX IDX_aoferty_id_firmy_status (id_firmy, status);

ALTER TABLE zamowienia
ADD INDEX IDX_zamowienia_id_firmy_status (id_firmy, status);
