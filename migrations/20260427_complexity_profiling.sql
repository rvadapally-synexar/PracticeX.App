-- Migration: complexity_profiling
-- Adds per-file complexity profiling to document_assets:
--   * complexity_tier:     S | M | L | X (Simple / Moderate / Large / Extra)
--   * complexity_factors_json: explainable signals (multi_sheet, has_formulas, ...)
--   * complexity_blockers_json: blockers that force tier X (macros_detected, ...)
--   * metadata_json:       format-specific details (sheet count, page count, etc.)
--   * estimated_complexity_hours: pricing-policy-derived hours
-- Idempotent — safe to re-run.

DO $EF$
BEGIN
    ALTER TABLE doc.document_assets
        ADD COLUMN IF NOT EXISTS complexity_tier character varying(2);
    ALTER TABLE doc.document_assets
        ADD COLUMN IF NOT EXISTS complexity_factors_json jsonb;
    ALTER TABLE doc.document_assets
        ADD COLUMN IF NOT EXISTS complexity_blockers_json jsonb;
    ALTER TABLE doc.document_assets
        ADD COLUMN IF NOT EXISTS metadata_json jsonb;
    ALTER TABLE doc.document_assets
        ADD COLUMN IF NOT EXISTS estimated_complexity_hours numeric(8,2);

    CREATE INDEX IF NOT EXISTS ix_document_assets_complexity_tier
        ON doc.document_assets (tenant_id, complexity_tier);
END $EF$;
