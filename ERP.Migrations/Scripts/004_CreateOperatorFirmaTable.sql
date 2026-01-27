-- Skrypt do utworzenia tabeli operatorfirma z kluczami obcymi
-- Jeśli tabela już istnieje, sprawdź czy ma odpowiednie klucze obce

-- Sprawdzenie czy tabela istnieje
-- Jeśli nie istnieje, utwórz ją:
CREATE TABLE IF NOT EXISTS operatorfirma (
    id INT(15) AUTO_INCREMENT PRIMARY KEY,
    id_operatora INT(15) NOT NULL,
    id_firmy INT(15) NOT NULL,
    rola INT(15) NULL,
    FOREIGN KEY (id_operatora) REFERENCES operator(id_operatora) ON DELETE CASCADE,
    FOREIGN KEY (id_firmy) REFERENCES firmy(ID_FIRMY) ON DELETE CASCADE,
    FOREIGN KEY (rola) REFERENCES rola(id) ON DELETE SET NULL,
    UNIQUE KEY unique_operator_firma (id_operatora, id_firmy)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Jeśli tabela już istnieje, sprawdź i dodaj brakujące klucze obce:
-- (Uruchom te komendy tylko jeśli klucze nie istnieją)

-- Sprawdź czy klucz obcy dla id_operatora istnieje
-- Jeśli nie, dodaj go:
-- ALTER TABLE operatorfirma 
-- ADD CONSTRAINT fk_operatorfirma_operator 
-- FOREIGN KEY (id_operatora) REFERENCES operator(id_operatora) ON DELETE CASCADE;

-- Sprawdź czy klucz obcy dla id_firmy istnieje
-- Jeśli nie, dodaj go:
-- ALTER TABLE operatorfirma 
-- ADD CONSTRAINT fk_operatorfirma_firmy 
-- FOREIGN KEY (id_firmy) REFERENCES firmy(ID_FIRMY) ON DELETE CASCADE;

-- Sprawdź czy klucz obcy dla rola istnieje (opcjonalny)
-- Jeśli nie, dodaj go:
-- ALTER TABLE operatorfirma 
-- ADD CONSTRAINT fk_operatorfirma_rola 
-- FOREIGN KEY (rola) REFERENCES rola(id) ON DELETE SET NULL;

-- Przykładowe dane testowe (odkomentuj jeśli potrzebujesz):
-- INSERT INTO operatorfirma (id_operatora, id_firmy, rola) VALUES
-- (1, 1, 1),
-- (1, 2, 2);
