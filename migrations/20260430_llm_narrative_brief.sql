-- Slice 16: Document Intelligence Brief — two-stage LLM extraction.
-- Stage 1 emits a markdown narrative brief authored as a healthcare attorney;
-- stage 2 extracts strict JSON from that brief. The brief itself is the
-- premium-feature surface; downstream consumers (Renewal Engine, scheduling
-- bridge, counterparty intel) read the structured fields stage 2 emits.
--
-- The existing llm_extracted_fields_json column is reused for stage-2 output.
-- This migration only adds the narrative-brief-specific columns + bookkeeping.
-- Idempotent.

ALTER TABLE doc.document_assets
  ADD COLUMN IF NOT EXISTS llm_narrative_md            text,
  ADD COLUMN IF NOT EXISTS llm_narrative_model         varchar(120),
  ADD COLUMN IF NOT EXISTS llm_narrative_tokens_in     int,
  ADD COLUMN IF NOT EXISTS llm_narrative_tokens_out    int,
  ADD COLUMN IF NOT EXISTS llm_narrative_extracted_at  timestamptz,
  ADD COLUMN IF NOT EXISTS llm_narrative_temperature   numeric(3,2),
  ADD COLUMN IF NOT EXISTS llm_narrative_status        varchar(40),
  ADD COLUMN IF NOT EXISTS llm_narrative_latency_ms    int;

CREATE INDEX IF NOT EXISTS ix_document_assets_llm_narrative_status
  ON doc.document_assets (tenant_id, llm_narrative_status)
  WHERE llm_narrative_status IS NOT NULL;
