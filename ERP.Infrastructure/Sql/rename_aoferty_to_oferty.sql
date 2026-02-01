-- =============================================================================
-- OPCJONALNIE: zmiana nazw tabel i kolumn (aoferty → oferty, apozycjeoferty → ofertypozycje)
-- =============================================================================
-- Uruchomić tylko gdy baza nadal ma stare nazwy. Po wykonaniu projekt WPF używa: oferty, ofertypozycje, id, oferta_id.
-- UWAGA: sprawdź zależności (FK, indeksy) przed uruchomieniem.
-- =============================================================================

-- 1) Zmiana nazw tabel
RENAME TABLE aoferty TO oferty;
RENAME TABLE apozycjeoferty TO ofertypozycje;

-- 2) Zmiana nazw kolumn w oferty (PK)
ALTER TABLE oferty CHANGE COLUMN ID_oferta id INT(15) NOT NULL AUTO_INCREMENT;

-- 3) Zmiana nazw kolumn w ofertypozycje (FK do oferty)
ALTER TABLE ofertypozycje CHANGE COLUMN ID_oferta oferta_id INT(15);

-- 4) Ewentualna aktualizacja FK (jeśli nazwa constraintu odnosi się do aoferty)
-- SHOW CREATE TABLE ofertypozycje;
-- DROP FOREIGN KEY fk_apozycjeoferty_aoferty;
-- ADD CONSTRAINT fk_ofertypozycje_oferty FOREIGN KEY (oferta_id) REFERENCES oferty(id);
