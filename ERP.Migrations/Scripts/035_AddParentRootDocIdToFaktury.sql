-- =============================================================================
-- 035: Dodanie parent_doc_id i root_doc_id do faktury
-- =============================================================================

ALTER TABLE faktury
  ADD COLUMN parent_doc_id BIGINT NULL,
  ADD COLUMN root_doc_id   BIGINT NULL;

CREATE INDEX idx_faktury_parent_doc_id ON faktury(parent_doc_id);
CREATE INDEX idx_faktury_root_doc_id   ON faktury(root_doc_id);
