-- =============================================================================
-- Tabela ofertypozycje: kolumna oferta_id (FK do oferty.id)
-- =============================================================================
-- Problem: projekt WPF oczekuje kolumny oferta_id, w bazie jest ID_oferta.
-- Rozwiązanie: zmiana nazwy kolumny ID_oferta -> oferta_id (te same wartości).
-- =============================================================================

-- Zachowuje typ (nullability jak w oryginale)
ALTER TABLE ofertypozycje CHANGE COLUMN ID_oferta oferta_id INT(15) NULL;
