-- Tabela liczników numerów dokumentów (np. FPF, FV) per firma / typ / rok.
-- FK: company_id -> firmy(id_firmy) (w tym projekcie tabela firm to firmy, nie companies).
-- Jeśli Twoja baza ma tabelę companies(id), zamień REFERENCES na: REFERENCES companies(id)

CREATE TABLE IF NOT EXISTS doc_counters (
  company_id BIGINT NOT NULL,
  doc_type   VARCHAR(16) NOT NULL,
  year       INT NOT NULL,
  month      INT NOT NULL,
  last_no    INT NOT NULL,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (company_id, doc_type, year, month),
  CONSTRAINT fk_doc_counters_company
    FOREIGN KEY (company_id) REFERENCES firmy(id_firmy)
) ENGINE=InnoDB;
