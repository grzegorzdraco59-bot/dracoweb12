-- =============================================================================
-- ETAP 1 – Migracja PRIMARY KEY na kolumnę 'id' (BIGINT)
-- MariaDB/MySQL. Wykonywać po ETAP 0 (audyt). NIE usuwać starych kolumn PK.
-- =============================================================================
-- Kolejność dla każdej tabeli: ADD id → UPDATE id = stary_PK → NOT NULL → DROP/ADD PK
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1) faktury (PK: Id_faktury)
-- -----------------------------------------------------------------------------
ALTER TABLE faktury ADD COLUMN id BIGINT NULL;
UPDATE faktury SET id = Id_faktury WHERE id IS NULL;
ALTER TABLE faktury MODIFY id BIGINT NOT NULL;
ALTER TABLE faktury DROP PRIMARY KEY, ADD PRIMARY KEY (id);
ALTER TABLE faktury MODIFY id BIGINT NOT NULL AUTO_INCREMENT;

-- -----------------------------------------------------------------------------
-- 2) pozycjefaktury (PK: id_pozycji_faktury)
-- -----------------------------------------------------------------------------
ALTER TABLE pozycjefaktury ADD COLUMN id BIGINT NULL;
UPDATE pozycjefaktury SET id = id_pozycji_faktury WHERE id IS NULL;
ALTER TABLE pozycjefaktury MODIFY id BIGINT NOT NULL;
ALTER TABLE pozycjefaktury DROP PRIMARY KEY, ADD PRIMARY KEY (id);
ALTER TABLE pozycjefaktury MODIFY id BIGINT NOT NULL AUTO_INCREMENT;

-- -----------------------------------------------------------------------------
-- 3) ofertypozycje (PK: ID_pozycja_oferty)
-- -----------------------------------------------------------------------------
ALTER TABLE ofertypozycje ADD COLUMN id BIGINT NULL;
UPDATE ofertypozycje SET id = ID_pozycja_oferty WHERE id IS NULL;
ALTER TABLE ofertypozycje MODIFY id BIGINT NOT NULL;
ALTER TABLE ofertypozycje DROP PRIMARY KEY, ADD PRIMARY KEY (id);

-- -----------------------------------------------------------------------------
-- 4) Odbiorcy (PK: ID_odbiorcy)
-- -----------------------------------------------------------------------------
ALTER TABLE Odbiorcy ADD COLUMN id BIGINT NULL;
UPDATE Odbiorcy SET id = ID_odbiorcy WHERE id IS NULL;
ALTER TABLE Odbiorcy MODIFY id BIGINT NOT NULL;
ALTER TABLE Odbiorcy DROP PRIMARY KEY, ADD PRIMARY KEY (id);

-- -----------------------------------------------------------------------------
-- 5) dostawcy (PK: id_dostawcy)
-- -----------------------------------------------------------------------------
ALTER TABLE dostawcy ADD COLUMN id BIGINT NULL;
UPDATE dostawcy SET id = id_dostawcy WHERE id IS NULL;
ALTER TABLE dostawcy MODIFY id BIGINT NOT NULL;
ALTER TABLE dostawcy DROP PRIMARY KEY, ADD PRIMARY KEY (id);

-- -----------------------------------------------------------------------------
-- 6) rola (PK: id_roli)
-- -----------------------------------------------------------------------------
ALTER TABLE rola ADD COLUMN id BIGINT NULL;
UPDATE rola SET id = id_roli WHERE id IS NULL;
ALTER TABLE rola MODIFY id BIGINT NOT NULL;
ALTER TABLE rola DROP PRIMARY KEY, ADD PRIMARY KEY (id);

-- -----------------------------------------------------------------------------
-- 7) towary (PK: ID_towar)
-- -----------------------------------------------------------------------------
ALTER TABLE towary ADD COLUMN id BIGINT NULL;
UPDATE towary SET id = ID_towar WHERE id IS NULL;
ALTER TABLE towary MODIFY id BIGINT NOT NULL;
ALTER TABLE towary DROP PRIMARY KEY, ADD PRIMARY KEY (id);

-- -----------------------------------------------------------------------------
-- 8) zamowienia – tylko jeśli PK to id_zamowienia (sprawdź: SHOW COLUMNS FROM zamowienia)
-- Jeśli tabela ma już kolumnę id jako PK – pominąć ten blok.
-- -----------------------------------------------------------------------------
-- ALTER TABLE zamowienia ADD COLUMN id BIGINT NULL;
-- UPDATE zamowienia SET id = id_zamowienia WHERE id IS NULL;
-- ALTER TABLE zamowienia MODIFY id BIGINT NOT NULL;
-- ALTER TABLE zamowienia DROP PRIMARY KEY, ADD PRIMARY KEY (id);

-- -----------------------------------------------------------------------------
-- 9) pozycjezamowienia (PK: id_pozycji_zamowienia)
-- -----------------------------------------------------------------------------
ALTER TABLE pozycjezamowienia ADD COLUMN id BIGINT NULL;
UPDATE pozycjezamowienia SET id = id_pozycji_zamowienia WHERE id IS NULL;
ALTER TABLE pozycjezamowienia MODIFY id BIGINT NOT NULL;
ALTER TABLE pozycjezamowienia DROP PRIMARY KEY, ADD PRIMARY KEY (id);

-- -----------------------------------------------------------------------------
-- 10) magazyn (PK: id_magazyn) – opcjonalnie
-- -----------------------------------------------------------------------------
-- ALTER TABLE magazyn ADD COLUMN id BIGINT NULL;
-- UPDATE magazyn SET id = id_magazyn WHERE id IS NULL;
-- ALTER TABLE magazyn MODIFY id BIGINT NOT NULL;
-- ALTER TABLE magazyn DROP PRIMARY KEY, ADD PRIMARY KEY (id);

-- oferty – już ma id jako PK, pomijamy.
-- operatorfirma, operator_login – już mają id, pomijamy.
-- doc_counters – PK złożony, bez zmian.
