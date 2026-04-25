-- Migration: source_discovery_extensions
-- Adds columns to existing source/ingestion/candidate tables to support the
-- Source Discovery slice (folder upload + Outlook Graph). Idempotent — safe to
-- run repeatedly through deployment pipelines per docs/database-migrations.md.

DO $EF$
BEGIN
    -- doc.source_connections: configuration, encrypted credentials placeholder, last error.
    ALTER TABLE doc.source_connections ADD COLUMN IF NOT EXISTS config_json jsonb;
    ALTER TABLE doc.source_connections ADD COLUMN IF NOT EXISTS credentials_json jsonb;
    ALTER TABLE doc.source_connections ADD COLUMN IF NOT EXISTS last_error text;
    ALTER TABLE doc.source_connections ADD COLUMN IF NOT EXISTS created_by_user_id uuid;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'fk_source_connections_users_created_by_user_id'
          AND table_schema = 'doc' AND table_name = 'source_connections'
    ) THEN
        ALTER TABLE doc.source_connections
            ADD CONSTRAINT fk_source_connections_users_created_by_user_id
            FOREIGN KEY (created_by_user_id) REFERENCES org.users (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    -- doc.source_objects: folder/relative-path metadata for discovery.
    ALTER TABLE doc.source_objects ADD COLUMN IF NOT EXISTS object_kind character varying(40) NOT NULL DEFAULT 'file';
    ALTER TABLE doc.source_objects ADD COLUMN IF NOT EXISTS relative_path character varying(1024);
    ALTER TABLE doc.source_objects ADD COLUMN IF NOT EXISTS parent_external_id character varying(512);
    ALTER TABLE doc.source_objects ADD COLUMN IF NOT EXISTS size_bytes bigint;
    ALTER TABLE doc.source_objects ADD COLUMN IF NOT EXISTS metadata_json jsonb;
    CREATE INDEX IF NOT EXISTS ix_source_objects_tenant_id_object_kind
        ON doc.source_objects (tenant_id, object_kind);
END $EF$;

DO $EF$
BEGIN
    -- doc.ingestion_batches: counts + connection link + notes.
    ALTER TABLE doc.ingestion_batches ADD COLUMN IF NOT EXISTS source_connection_id uuid;
    ALTER TABLE doc.ingestion_batches ADD COLUMN IF NOT EXISTS candidate_count integer NOT NULL DEFAULT 0;
    ALTER TABLE doc.ingestion_batches ADD COLUMN IF NOT EXISTS skipped_count integer NOT NULL DEFAULT 0;
    ALTER TABLE doc.ingestion_batches ADD COLUMN IF NOT EXISTS error_count integer NOT NULL DEFAULT 0;
    ALTER TABLE doc.ingestion_batches ADD COLUMN IF NOT EXISTS notes text;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'fk_ingestion_batches_source_connections_source_connection_id'
          AND table_schema = 'doc' AND table_name = 'ingestion_batches'
    ) THEN
        ALTER TABLE doc.ingestion_batches
            ADD CONSTRAINT fk_ingestion_batches_source_connections_source_connection_id
            FOREIGN KEY (source_connection_id) REFERENCES doc.source_connections (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    -- doc.ingestion_jobs: stage progression.
    ALTER TABLE doc.ingestion_jobs ADD COLUMN IF NOT EXISTS stage character varying(40) NOT NULL DEFAULT 'discovered';
END $EF$;

DO $EF$
BEGIN
    -- doc.document_candidates: reason codes + provenance for the candidate row.
    ALTER TABLE doc.document_candidates ADD COLUMN IF NOT EXISTS reason_codes_json jsonb;
    ALTER TABLE doc.document_candidates ADD COLUMN IF NOT EXISTS classifier_version character varying(40) NOT NULL DEFAULT 'rule_v1';
    ALTER TABLE doc.document_candidates ADD COLUMN IF NOT EXISTS origin_filename character varying(512);
    ALTER TABLE doc.document_candidates ADD COLUMN IF NOT EXISTS relative_path character varying(1024);
    ALTER TABLE doc.document_candidates ADD COLUMN IF NOT EXISTS source_object_id uuid;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'fk_document_candidates_source_objects_source_object_id'
          AND table_schema = 'doc' AND table_name = 'document_candidates'
    ) THEN
        ALTER TABLE doc.document_candidates
            ADD CONSTRAINT fk_document_candidates_source_objects_source_object_id
            FOREIGN KEY (source_object_id) REFERENCES doc.source_objects (id) ON DELETE RESTRICT;
    END IF;
END $EF$;
