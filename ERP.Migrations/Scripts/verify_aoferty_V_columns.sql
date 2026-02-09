-- Weryfikacja kolumn widoku aoferty_V
-- Uruchom w HeidiSQL. Wynik: same nazwy kolumn (np. ID_oferta lub id).
-- Jeśli widok ma kolumnę id zamiast ID_oferta, zmień w OfferRepository/SearchByCompanyIdAsync:
--   ID_oferta -> id (w SELECT, WHERE, ORDER BY).

SHOW COLUMNS FROM aoferty_V;
