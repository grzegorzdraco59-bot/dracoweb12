-- =============================================================================
-- Migracja: tabele oferty, ofertypozycje – brakujące kolumny
-- =============================================================================
-- Zakładamy, że tabele mają już nazwy: oferty, ofertypozycje
-- (jeśli nie: RENAME TABLE aoferty TO oferty, apozycjeoferty TO ofertypozycje).
-- Kolumny: oferty.id (PK), ofertypozycje.oferta_id (FK).
-- Jeśli w DB nadal jest ID_oferta: ALTER oferty CHANGE ID_oferta id INT(15) NOT NULL AUTO_INCREMENT;
-- ALTER ofertypozycje CHANGE ID_oferta oferta_id INT(15);
-- =============================================================================

-- oferty: suma brutto z pozycji (nagłówek)
ALTER TABLE oferty
  ADD COLUMN IF NOT EXISTS sum_brutto DECIMAL(18,2) NULL DEFAULT 0.00;

-- ofertypozycje: pola wyliczone (algorytm jak pozycjefaktury – rabat %, zaokrąglenia na pozycji)
ALTER TABLE ofertypozycje
  ADD COLUMN IF NOT EXISTS netto_poz  DECIMAL(18,2) NULL,
  ADD COLUMN IF NOT EXISTS vat_poz    DECIMAL(18,2) NULL,
  ADD COLUMN IF NOT EXISTS brutto_poz DECIMAL(18,2) NULL;
