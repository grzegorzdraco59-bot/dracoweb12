-- Skrypt do utworzenia tabeli Odbiorcy
-- UWAGA: Tabela już istnieje w bazie danych locbd
-- Ten skrypt służy jako dokumentacja rzeczywistej struktury tabeli

CREATE TABLE IF NOT EXISTS Odbiorcy (
    ID_odbiorcy INT(15) AUTO_INCREMENT PRIMARY KEY,
    id_firmy INT(15) NOT NULL,
    Nazwa VARCHAR(100),
    Nazwisko VARCHAR(100),
    Imie VARCHAR(100),
    Uwagi VARCHAR(1000),
    Tel_1 VARCHAR(150),
    Tel_2 VARCHAR(100),
    NIP VARCHAR(30),
    Ulica_nr VARCHAR(100),
    Kod_pocztowy VARCHAR(30),
    Miasto VARCHAR(100),
    Kraj VARCHAR(100),
    Ulica_nr_wysylka VARCHAR(100),
    Kod_pocztowy_wysylka VARCHAR(30),
    Miasto_wysylka VARCHAR(100),
    Kraj_wysylka VARCHAR(100),
    Email_1 VARCHAR(250),
    Email_2 VARCHAR(100),
    kod VARCHAR(100),
    status VARCHAR(100),
    waluta VARCHAR(5) NOT NULL DEFAULT 'PLN',
    odbiorca_typ INT(2),
    do_oferty BIT(1),
    status_vat CHAR(20),
    regon CHAR(50),
    adres_caly VARCHAR(200)
);

-- Indeksy (jeśli potrzebne)
-- CREATE INDEX idx_nazwa ON Odbiorcy(Nazwa);
-- CREATE INDEX idx_id_firmy ON Odbiorcy(id_firmy);
-- CREATE INDEX idx_nip ON Odbiorcy(NIP);
