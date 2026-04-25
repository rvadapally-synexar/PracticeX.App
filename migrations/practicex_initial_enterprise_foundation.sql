DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'audit') THEN
        CREATE SCHEMA audit;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS audit.__ef_migrations_history (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'audit') THEN
            CREATE SCHEMA audit;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'contract') THEN
            CREATE SCHEMA contract;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'doc') THEN
            CREATE SCHEMA doc;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'evidence') THEN
            CREATE SCHEMA evidence;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'org') THEN
            CREATE SCHEMA org;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'workflow') THEN
            CREATE SCHEMA workflow;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE TABLE org.tenants (
        id uuid NOT NULL,
        name character varying(240) NOT NULL,
        status character varying(40) NOT NULL,
        data_region character varying(40) NOT NULL,
        baa_status character varying(40) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_tenants PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE TABLE audit.audit_events (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        actor_type character varying(80) NOT NULL,
        actor_id uuid,
        event_type character varying(160) NOT NULL,
        resource_type character varying(120) NOT NULL,
        resource_id uuid NOT NULL,
        prior_value_hash character varying(128),
        new_value_hash character varying(128),
        metadata_json jsonb,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_audit_events PRIMARY KEY (id),
        CONSTRAINT fk_audit_events_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE TABLE contract.counterparties (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        name character varying(300) NOT NULL,
        type character varying(80) NOT NULL,
        aliases jsonb NOT NULL,
        payer_identifier character varying(160),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_counterparties PRIMARY KEY (id),
        CONSTRAINT fk_counterparties_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE TABLE org.facilities (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        name character varying(240) NOT NULL,
        code character varying(16) NOT NULL,
        npi text,
        tax_id text,
        address text,
        specialty text,
        status character varying(40) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_facilities PRIMARY KEY (id),
        CONSTRAINT fk_facilities_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE TABLE org.roles (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        name character varying(120) NOT NULL,
        permissions jsonb NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_roles PRIMARY KEY (id),
        CONSTRAINT fk_roles_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE TABLE doc.source_connections (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        source_type character varying(80) NOT NULL,
        status character varying(40) NOT NULL,
        display_name character varying(240),
        oauth_subject text,
        scope_set text,
        last_sync_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_source_connections PRIMARY KEY (id),
        CONSTRAINT fk_source_connections_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE TABLE org.users (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        email character varying(320) NOT NULL,
        name character varying(240) NOT NULL,
        status character varying(40) NOT NULL,
        last_login_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_users PRIMARY KEY (id),
        CONSTRAINT fk_users_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE TABLE doc.source_objects (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        connection_id uuid NOT NULL,
        external_id character varying(512) NOT NULL,
        uri character varying(2048) NOT NULL,
        name character varying(512) NOT NULL,
        mime_type character varying(160) NOT NULL,
        sha256 character varying(64),
        source_created_at timestamp with time zone,
        source_modified_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_source_objects PRIMARY KEY (id),
        CONSTRAINT fk_source_objects_source_connections_connection_id FOREIGN KEY (connection_id) REFERENCES doc.source_connections (id) ON DELETE RESTRICT,
        CONSTRAINT fk_source_objects_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE TABLE contract.contracts (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        facility_id uuid NOT NULL,
        counterparty_id uuid NOT NULL,
        contract_type character varying(80) NOT NULL,
        status character varying(40) NOT NULL,
        owner_user_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_contracts PRIMARY KEY (id),
        CONSTRAINT fk_contracts_counterparties_counterparty_id FOREIGN KEY (counterparty_id) REFERENCES contract.counterparties (id) ON DELETE RESTRICT,
        CONSTRAINT fk_contracts_facilities_facility_id FOREIGN KEY (facility_id) REFERENCES org.facilities (id) ON DELETE RESTRICT,
        CONSTRAINT fk_contracts_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT,
        CONSTRAINT fk_contracts_users_owner_user_id FOREIGN KEY (owner_user_id) REFERENCES org.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE TABLE doc.ingestion_batches (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        source_type character varying(80) NOT NULL,
        created_by_user_id uuid NOT NULL,
        status character varying(40) NOT NULL,
        file_count integer NOT NULL,
        started_at timestamp with time zone,
        completed_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_ingestion_batches PRIMARY KEY (id),
        CONSTRAINT fk_ingestion_batches_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT,
        CONSTRAINT fk_ingestion_batches_users_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES org.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE TABLE workflow.review_tasks (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        resource_type character varying(120) NOT NULL,
        resource_id uuid NOT NULL,
        reason character varying(240) NOT NULL,
        priority integer NOT NULL,
        assigned_to_user_id uuid,
        decision character varying(40) NOT NULL,
        resolved_at timestamp with time zone,
        effort_seconds integer,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_review_tasks PRIMARY KEY (id),
        CONSTRAINT fk_review_tasks_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT,
        CONSTRAINT fk_review_tasks_users_assigned_to_user_id FOREIGN KEY (assigned_to_user_id) REFERENCES org.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE TABLE org.role_assignments (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        user_id uuid NOT NULL,
        facility_id uuid,
        role_id uuid NOT NULL,
        status character varying(40) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_role_assignments PRIMARY KEY (id),
        CONSTRAINT fk_role_assignments_facilities_facility_id FOREIGN KEY (facility_id) REFERENCES org.facilities (id) ON DELETE RESTRICT,
        CONSTRAINT fk_role_assignments_roles_role_id FOREIGN KEY (role_id) REFERENCES org.roles (id) ON DELETE RESTRICT,
        CONSTRAINT fk_role_assignments_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT,
        CONSTRAINT fk_role_assignments_users_user_id FOREIGN KEY (user_id) REFERENCES org.users (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE TABLE doc.document_assets (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        source_object_id uuid,
        storage_uri character varying(2048) NOT NULL,
        sha256 character varying(64) NOT NULL,
        mime_type character varying(160) NOT NULL,
        size_bytes bigint NOT NULL,
        page_count integer,
        text_status character varying(40) NOT NULL,
        ocr_status character varying(40) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_document_assets PRIMARY KEY (id),
        CONSTRAINT fk_document_assets_source_objects_source_object_id FOREIGN KEY (source_object_id) REFERENCES doc.source_objects (id) ON DELETE RESTRICT,
        CONSTRAINT fk_document_assets_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE TABLE contract.contract_fields (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        contract_id uuid NOT NULL,
        schema_version character varying(80) NOT NULL,
        field_key character varying(160) NOT NULL,
        value_json jsonb NOT NULL,
        normalized_value text,
        confidence numeric(5,4) NOT NULL,
        review_status character varying(40) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_contract_fields PRIMARY KEY (id),
        CONSTRAINT fk_contract_fields_contracts_contract_id FOREIGN KEY (contract_id) REFERENCES contract.contracts (id) ON DELETE RESTRICT,
        CONSTRAINT fk_contract_fields_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE TABLE doc.document_candidates (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        document_asset_id uuid NOT NULL,
        candidate_type character varying(80) NOT NULL,
        facility_hint_id uuid,
        counterparty_hint text,
        confidence numeric(5,4) NOT NULL,
        status character varying(40) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_document_candidates PRIMARY KEY (id),
        CONSTRAINT fk_document_candidates_document_assets_document_asset_id FOREIGN KEY (document_asset_id) REFERENCES doc.document_assets (id) ON DELETE RESTRICT,
        CONSTRAINT fk_document_candidates_facilities_facility_hint_id FOREIGN KEY (facility_hint_id) REFERENCES org.facilities (id) ON DELETE RESTRICT,
        CONSTRAINT fk_document_candidates_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE TABLE evidence.evidence_links (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        resource_type character varying(120) NOT NULL,
        resource_id uuid NOT NULL,
        document_asset_id uuid NOT NULL,
        page_refs jsonb NOT NULL,
        quote text NOT NULL,
        source_object_id uuid,
        agent_run_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_evidence_links PRIMARY KEY (id),
        CONSTRAINT fk_evidence_links_document_assets_document_asset_id FOREIGN KEY (document_asset_id) REFERENCES doc.document_assets (id) ON DELETE RESTRICT,
        CONSTRAINT fk_evidence_links_source_objects_source_object_id FOREIGN KEY (source_object_id) REFERENCES doc.source_objects (id) ON DELETE RESTRICT,
        CONSTRAINT fk_evidence_links_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE TABLE doc.ingestion_jobs (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        batch_id uuid NOT NULL,
        source_object_id uuid,
        document_asset_id uuid,
        status character varying(40) NOT NULL,
        error_code character varying(120),
        error_message text,
        attempt_count integer NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_ingestion_jobs PRIMARY KEY (id),
        CONSTRAINT fk_ingestion_jobs_document_assets_document_asset_id FOREIGN KEY (document_asset_id) REFERENCES doc.document_assets (id) ON DELETE RESTRICT,
        CONSTRAINT fk_ingestion_jobs_ingestion_batches_batch_id FOREIGN KEY (batch_id) REFERENCES doc.ingestion_batches (id) ON DELETE RESTRICT,
        CONSTRAINT fk_ingestion_jobs_source_objects_source_object_id FOREIGN KEY (source_object_id) REFERENCES doc.source_objects (id) ON DELETE RESTRICT,
        CONSTRAINT fk_ingestion_jobs_tenants_tenant_id FOREIGN KEY (tenant_id) REFERENCES org.tenants (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_audit_events_resource_type_resource_id ON audit.audit_events (resource_type, resource_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_audit_events_tenant_id_created_at ON audit.audit_events (tenant_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE UNIQUE INDEX ix_contract_fields_contract_id_field_key_schema_version ON contract.contract_fields (contract_id, field_key, schema_version);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_contract_fields_tenant_id ON contract.contract_fields (tenant_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_contracts_counterparty_id ON contract.contracts (counterparty_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_contracts_facility_id ON contract.contracts (facility_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_contracts_owner_user_id ON contract.contracts (owner_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_contracts_tenant_id_facility_id_status ON contract.contracts (tenant_id, facility_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE UNIQUE INDEX ix_counterparties_tenant_id_name ON contract.counterparties (tenant_id, name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_document_assets_source_object_id ON doc.document_assets (source_object_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE UNIQUE INDEX ix_document_assets_tenant_id_sha256 ON doc.document_assets (tenant_id, sha256);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_document_candidates_document_asset_id ON doc.document_candidates (document_asset_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_document_candidates_facility_hint_id ON doc.document_candidates (facility_hint_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_document_candidates_tenant_id_status ON doc.document_candidates (tenant_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_evidence_links_document_asset_id ON evidence.evidence_links (document_asset_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_evidence_links_resource_type_resource_id ON evidence.evidence_links (resource_type, resource_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_evidence_links_source_object_id ON evidence.evidence_links (source_object_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_evidence_links_tenant_id ON evidence.evidence_links (tenant_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE UNIQUE INDEX ix_facilities_tenant_id_code ON org.facilities (tenant_id, code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_ingestion_batches_created_by_user_id ON doc.ingestion_batches (created_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_ingestion_batches_tenant_id_created_at ON doc.ingestion_batches (tenant_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_ingestion_jobs_batch_id ON doc.ingestion_jobs (batch_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_ingestion_jobs_document_asset_id ON doc.ingestion_jobs (document_asset_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_ingestion_jobs_source_object_id ON doc.ingestion_jobs (source_object_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_ingestion_jobs_tenant_id_status ON doc.ingestion_jobs (tenant_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_review_tasks_assigned_to_user_id ON workflow.review_tasks (assigned_to_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_review_tasks_tenant_id_decision_priority ON workflow.review_tasks (tenant_id, decision, priority);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_role_assignments_facility_id ON org.role_assignments (facility_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_role_assignments_role_id ON org.role_assignments (role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE UNIQUE INDEX ix_role_assignments_tenant_id_user_id_facility_id_role_id ON org.role_assignments (tenant_id, user_id, facility_id, role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_role_assignments_user_id ON org.role_assignments (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_roles_tenant_id ON org.roles (tenant_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_source_connections_tenant_id_source_type_display_name ON doc.source_connections (tenant_id, source_type, display_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE UNIQUE INDEX ix_source_objects_connection_id_external_id ON doc.source_objects (connection_id, external_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE INDEX ix_source_objects_tenant_id_sha256 ON doc.source_objects (tenant_id, sha256);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    CREATE UNIQUE INDEX ix_users_tenant_id_email ON org.users (tenant_id, email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM audit.__ef_migrations_history WHERE "migration_id" = '20260425181152_initial_enterprise_foundation') THEN
    INSERT INTO audit.__ef_migrations_history (migration_id, product_version)
    VALUES ('20260425181152_initial_enterprise_foundation', '9.0.1');
    END IF;
END $EF$;
COMMIT;

