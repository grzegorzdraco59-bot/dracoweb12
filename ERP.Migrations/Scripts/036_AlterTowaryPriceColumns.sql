-- 036: Ujednolicenie kolumn cenowych w tabeli towary do decimal(15,2)
ALTER TABLE towary
  MODIFY COLUMN Cena_PLN decimal(15,2) NULL,
  MODIFY COLUMN Cena_EUR decimal(15,2) NULL,
  MODIFY COLUMN Cena_USD decimal(15,2) NULL,
  MODIFY COLUMN cena_zakupu decimal(15,2) NULL,
  MODIFY COLUMN cena_zakupu_PLN decimal(15,2) NULL,
  MODIFY COLUMN cena_zakupu_PLN_nowe_jednostki decimal(15,2) NULL,
  MODIFY COLUMN koszty_materialow decimal(15,2) NULL;
