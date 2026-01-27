-- Skrypt do utworzenia klucza obcego dla pozycji zamówienia
-- Łączy tabelę pozycjezamowienia z tabelą zamowienia

-- Najpierw sprawdźmy strukturę tabel
-- DESCRIBE zamowienia;
-- DESCRIBE pozycjezamowienia;

-- Sprawdźmy czy kolumny istnieją i znajdźmy właściwe nazwy
-- Jeśli kolumna ID w zamowienia ma inną nazwę, należy ją dostosować

-- Usuń istniejący klucz obcy jeśli istnieje (opcjonalnie)
-- ALTER TABLE pozycjezamowienia DROP FOREIGN KEY IF EXISTS fk_pozycjezamowienia_zamowienia;

-- Utwórz klucz obcy
-- Uwaga: Dostosuj nazwy kolumn do rzeczywistej struktury tabel w bazie
-- Przykład dla standardowych nazw:
ALTER TABLE pozycjezamowienia 
ADD CONSTRAINT fk_pozycjezamowienia_zamowienia 
FOREIGN KEY (id_zamowienia) 
REFERENCES zamowienia(id) 
ON DELETE CASCADE 
ON UPDATE CASCADE;

-- Jeśli kolumna ID w zamowienia ma inną nazwę (np. id_zamowienia), użyj:
-- ALTER TABLE pozycjezamowienia 
-- ADD CONSTRAINT fk_pozycjezamowienia_zamowienia 
-- FOREIGN KEY (id_zamowienia) 
-- REFERENCES zamowienia(id_zamowienia) 
-- ON DELETE CASCADE 
-- ON UPDATE CASCADE;

-- Sprawdź czy klucz został utworzony
-- SHOW CREATE TABLE pozycjezamowienia;
