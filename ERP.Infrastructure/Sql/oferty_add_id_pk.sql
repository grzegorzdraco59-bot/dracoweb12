-- =============================================================================
-- Tabela oferty: kolumna `id` jako PRIMARY KEY (zachowanie danych i relacji)
-- =============================================================================
-- Problem: po RENAME TABLE aoferty -> oferty tabela ma nadal kolumnę ID_oferty,
-- a projekt WPF oczekuje kolumny `id`.
-- Rozwiązanie: zmiana nazwy kolumny ID_oferty -> id (te same wartości, PK zostaje).
-- Relacje (faktury.id_oferty, faktury.source_offer_id, ofertypozycje.oferta_id) 
-- przechowują te same wartości liczbowe – po zmianie nazwy kolumny nadal są poprawne.
-- =============================================================================

-- Zmiana nazwy kolumny klucza głównego: ID_oferty -> id (AUTO_INCREMENT i PK zachowane)
-- W MySQL/MariaDB PRIMARY KEY pozostaje na tej kolumnie po zmianie nazwy.
ALTER TABLE oferty CHANGE COLUMN ID_oferty id INT(15) NOT NULL AUTO_INCREMENT;
