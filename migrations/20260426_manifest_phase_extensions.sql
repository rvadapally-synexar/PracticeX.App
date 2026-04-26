-- Migration: manifest_phase_extensions
-- Adds the manifest-first / bundle-upload concepts to the ingestion model:
--   * doc.ingestion_batches.phase: lifecycle marker for the new staged flow.
--   * doc.document_assets: extraction routing + validity columns (PdfPig-driven).
--   * doc.source_objects: proposed_status + quick_fingerprint for pre-bytes scoring.
-- Idempotent — safe to re-run.

DO $EF$
BEGIN
    ALTER TABLE doc.ingestion_batches
        ADD COLUMN IF NOT EXISTS phase character varying(40) NOT NULL DEFAULT 'complete';

    CREATE INDEX IF NOT EXISTS ix_ingestion_batches_phase
        ON doc.ingestion_batches (tenant_id, phase, started_at DESC);
END $EF$;

DO $EF$
BEGIN
    ALTER TABLE doc.document_assets
        ADD COLUMN IF NOT EXISTS extraction_route character varying(40);
    ALTER TABLE doc.document_assets
        ADD COLUMN IF NOT EXISTS validity_status character varying(40);
    ALTER TABLE doc.document_assets
        ADD COLUMN IF NOT EXISTS has_text_layer boolean;
    ALTER TABLE doc.document_assets
        ADD COLUMN IF NOT EXISTS is_encrypted boolean;
END $EF$;

DO $EF$
BEGIN
    ALTER TABLE doc.source_objects
        ADD COLUMN IF NOT EXISTS proposed_status character varying(40);
    ALTER TABLE doc.source_objects
        ADD COLUMN IF NOT EXISTS quick_fingerprint character varying(96);

    CREATE INDEX IF NOT EXISTS ix_source_objects_tenant_quick_fingerprint
        ON doc.source_objects (tenant_id, quick_fingerprint)
        WHERE quick_fingerprint IS NOT NULL;
END $EF$;
