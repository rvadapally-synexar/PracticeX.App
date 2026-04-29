-- Slice 8: persist regex-extractor output on document_assets so the
-- premium analysis surface can render structured fields (parties, dates,
-- premises, equity grants, etc) without re-running extraction every request.
--
-- Idempotent. Safe to re-run.

ALTER TABLE doc.document_assets
  ADD COLUMN IF NOT EXISTS extracted_fields_json     jsonb,
  ADD COLUMN IF NOT EXISTS extracted_subtype         varchar(60),
  ADD COLUMN IF NOT EXISTS extracted_schema_version  varchar(40),
  ADD COLUMN IF NOT EXISTS extractor_name            varchar(80),
  ADD COLUMN IF NOT EXISTS extraction_status         varchar(40) DEFAULT 'pending',
  ADD COLUMN IF NOT EXISTS extraction_extracted_at   timestamptz,
  ADD COLUMN IF NOT EXISTS extracted_is_template     boolean,
  ADD COLUMN IF NOT EXISTS extracted_is_executed     boolean;

CREATE INDEX IF NOT EXISTS ix_document_assets_extraction_status
  ON doc.document_assets (tenant_id, extraction_status)
  WHERE extraction_status IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_document_assets_extracted_subtype
  ON doc.document_assets (tenant_id, extracted_subtype)
  WHERE extracted_subtype IS NOT NULL;
