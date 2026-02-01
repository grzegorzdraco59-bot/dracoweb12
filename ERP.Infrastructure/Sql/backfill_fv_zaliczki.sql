-- =============================================================================
-- ETAP 4 – BACKFILL: rozliczenie zaliczek dla historycznych FV
-- =============================================================================
-- Dla każdej faktury końcowej FV (doc_type='FV') z root_doc_id IS NOT NULL:
--   sum_zaliczek_brutto = SUM(sum_brutto) FVZ w tej samej sprawie (id_firmy, root_doc_id)
--   do_zaplaty_brutto   = ROUND(sum_brutto - sum_zaliczek_brutto, 2), min 0
-- Wymaga: kolumny sum_zaliczek_brutto, do_zaplaty_brutto (faktury_add_zaliczki_fields.sql).
-- =============================================================================

-- Liczba FV do przeliczenia (do raportu)
SELECT COUNT(*) AS fv_do_przeliczenia
FROM faktury
WHERE doc_type = 'FV' AND root_doc_id IS NOT NULL;

START TRANSACTION;

DROP TEMPORARY TABLE IF EXISTS tmp_fv_zaliczki;

-- 1) Tabela tymczasowa: dla każdej FV (z root_doc_id) suma brutto FVZ w tej samej sprawie
CREATE TEMPORARY TABLE tmp_fv_zaliczki (
  Id_faktury INT PRIMARY KEY,
  zaliczki   DECIMAL(18,2) NOT NULL DEFAULT 0
);

INSERT INTO tmp_fv_zaliczki (Id_faktury, zaliczki)
SELECT f.Id_faktury,
       (SELECT COALESCE(SUM(f2.sum_brutto), 0)
        FROM faktury f2
        WHERE f2.id_firmy    = f.id_firmy
          AND f2.root_doc_id = f.root_doc_id
          AND f2.doc_type   = 'FVZ'
          AND f2.Id_faktury <> f.Id_faktury)
FROM faktury f
WHERE f.doc_type = 'FV' AND f.root_doc_id IS NOT NULL;

-- 2) Aktualizacja FV: sum_zaliczek_brutto, do_zaplaty_brutto
UPDATE faktury f
INNER JOIN tmp_fv_zaliczki t ON f.Id_faktury = t.Id_faktury
SET f.sum_zaliczek_brutto = t.zaliczki,
    f.do_zaplaty_brutto   = GREATEST(0, ROUND(COALESCE(f.sum_brutto, 0) - t.zaliczki, 2));

-- Raport: ile rekordów przeliczono
SELECT ROW_COUNT() AS przeliczono_fv;

COMMIT;

DROP TEMPORARY TABLE IF EXISTS tmp_fv_zaliczki;

-- =============================================================================
-- Partie: przy bardzo dużych tabelach można podzielić ręcznie po zakresie Id_faktury
-- (np. WHERE f.Id_faktury BETWEEN @min_id AND @max_id w INSERT do tmp_fv_zaliczki).
-- =============================================================================

-- =============================================================================
-- Opcjonalnie: FV z root_doc_id = NULL (ustaw 0 zaliczek, do_zaplaty = sum_brutto)
-- =============================================================================
-- UPDATE faktury
-- SET sum_zaliczek_brutto = 0,
--     do_zaplaty_brutto   = GREATEST(0, ROUND(COALESCE(sum_brutto, 0), 2))
-- WHERE doc_type = 'FV' AND root_doc_id IS NULL;
