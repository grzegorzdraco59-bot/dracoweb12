-- =============================================================================
-- ETAP 2 – Migracja FOREIGN KEY na kolumny <tabela>_id
-- Uruchomić PO migrate_pk_to_id_etap1.sql. Starych kolumn FK NIE usuwać.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1) pozycjefaktury: id_faktury → faktura_id (faktury.id = Id_faktury po ETAP 1)
-- -----------------------------------------------------------------------------
ALTER TABLE pozycjefaktury ADD COLUMN faktura_id BIGINT NULL;
UPDATE pozycjefaktury p
INNER JOIN faktury f ON f.Id_faktury = p.id_faktury
SET p.faktura_id = f.id;
-- Sprawdź: SELECT COUNT(*) FROM pozycjefaktury WHERE faktura_id IS NULL;
ALTER TABLE pozycjefaktury MODIFY faktura_id BIGINT NOT NULL;

-- -----------------------------------------------------------------------------
-- 2) pozycjefaktury: id_oferty → oferta_id (jeśli oferty ma id)
-- -----------------------------------------------------------------------------
ALTER TABLE pozycjefaktury ADD COLUMN oferta_id BIGINT NULL;
UPDATE pozycjefaktury p
INNER JOIN oferty o ON o.id = p.id_oferty
SET p.oferta_id = o.id
WHERE p.id_oferty IS NOT NULL;
-- Opcjonalnie: MODIFY oferta_id NOT NULL tylko jeśli wszystkie wiersze mają ofertę
-- ALTER TABLE pozycjefaktury MODIFY oferta_id BIGINT NOT NULL;

-- -----------------------------------------------------------------------------
-- 3) faktury: id_oferty → oferta_id (oferty.id już jest)
-- -----------------------------------------------------------------------------
ALTER TABLE faktury ADD COLUMN oferta_id BIGINT NULL;
UPDATE faktury f
INNER JOIN oferty o ON o.id = f.id_oferty
SET f.oferta_id = o.id
WHERE f.id_oferty IS NOT NULL;

-- -----------------------------------------------------------------------------
-- 4) ofertypozycje: oferta_id już może istnieć; jeśli jest id_oferty, skopiuj
-- -----------------------------------------------------------------------------
-- Jeśli ofertypozycje ma oferta_id – sprawdź czy wypełnione. Jeśli ma id_oferty:
-- UPDATE ofertypozycje p INNER JOIN oferty o ON o.id = p.id_oferty SET p.oferta_id = o.id;
-- (pomiń jeśli oferta_id już jest i wypełniony)

-- -----------------------------------------------------------------------------
-- 5) pozycjezamowienia: id_zamowienia → zamowienie_id (zamowienia.id po ETAP 1)
-- Wymaga aby zamowienia miała kolumnę id (uruchom blok zamowienia w ETAP 1 lub dostosuj)
-- -----------------------------------------------------------------------------
-- ALTER TABLE pozycjezamowienia ADD COLUMN zamowienie_id BIGINT NULL;
-- UPDATE pozycjezamowienia p
-- INNER JOIN zamowienia z ON z.id = p.id_zamowienia OR z.id_zamowienia = p.id_zamowienia
-- SET p.zamowienie_id = z.id;
-- ALTER TABLE pozycjezamowienia MODIFY zamowienie_id BIGINT NOT NULL;
